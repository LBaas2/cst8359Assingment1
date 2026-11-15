using Assignment1.Data;
using Assignment1.DTOs;
using Assignment1.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Assignment1.Controllers
{
    public class EventController : Controller
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName = "bannerurl";

        private readonly EventDBContext _context;

        private readonly ILogger<EventController> _logger;


        public EventController(ILogger<EventController> logger, BlobServiceClient blobServiceClient, EventDBContext context)
        {
            _logger = logger;
            _blobServiceClient = blobServiceClient;
            _context = context;
        }

        [Route("/[controller]")]
        public IActionResult EventManager()
        {

            var events = _context.Events.ToList();             

            return View(events);
        }

        [Route("/[controller]/{id}/attendees/create")]
        [HttpGet]
        public async Task<IActionResult> CreateAttendee(int id)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null)
            {
                return NotFound();
            }

            ViewBag.EventTitle = ev.title;
            var attendee = new Attendee { EventID = id };
            return View(attendee);
        }

        [Route("/[controller]/{id}/attendees/create")]
        [HttpPost]
        public async Task<IActionResult> CreateAttendee(int id, Attendee attendee)
        {

            var ev = await _context.Events.FindAsync(id);
            if (ev == null)
            {
                return NotFound();
            }

            // Link the attendee to the event ID from the route
            attendee.id = 0;
            attendee.EventID = id;

            if (ModelState.IsValid)
            {
                _context.Add(attendee);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"{attendee.name} was added to {ev.title}.";
                return RedirectToAction("ManageAttendee", new { id = id });
            }

            // Re-populate the event title if validation fails so the heading renders properly
            ViewBag.EventTitle = ev.title;
            return View(attendee);
        }

        [Route("/[controller]/{id}/attendees/delete")]
        [HttpGet]
        public async Task<IActionResult> DeleteAttendee(int id, int attendeeId)
        {
            var ev = await _context.Events
                .Include(e => e.attendees)
                .FirstOrDefaultAsync(e => e.id == id);

            if (ev == null)
            {
                return NotFound();
            }

            var attendee = ev.attendees.FirstOrDefault(a => a.id == attendeeId);
            if (attendee != null)
            {
                ev.attendees.Remove(attendee);
                _context.Remove(attendee);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"{attendee.name} was removed from {ev.title}.";
            }

            return RedirectToAction("ManageAttendee", new { id = id });
        }

        [Route("/[controller]/create")]
        [HttpGet]
        public IActionResult CreateEvent()
        {
            return View();
        }

        [Route("/[controller]/create")]
        [HttpPost]
        public async Task<IActionResult> CreateEvent([FromForm] EventDto dto)
        {
            Event newEvent;
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (dto.BannerImage != null)
            {
                var containerClient = await GetOrCreateContainerClientAsync();
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

                string uniqueFileName = $"{Guid.NewGuid()}_{dto.BannerImage.FileName}";
                var blobClient = containerClient.GetBlobClient(uniqueFileName);

                using var stream = dto.BannerImage.OpenReadStream();
                await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = dto.BannerImage.ContentType });

                newEvent = new Event
                {
                    title = dto.Title,
                    description = dto.Description,
                    date = dto.Date,
                    location = dto.Location,
                    bannerurl = blobClient.Uri.ToString()
                };
            }
            else
            {
                newEvent = new Event
                {
                    title = dto.Title,
                    description = dto.Description,
                    date = dto.Date,
                    location = dto.Location,
                    bannerurl = ""
                };
            }
            _context.Add(newEvent);
            await _context.SaveChangesAsync();

            return RedirectToAction("EventManager");
        }

        // GET: Displays the form pre-filled with existing attendee data
        [Route("/[controller]/{id}/attendees/{attendeeID}")]
        [HttpGet]
        public async Task<IActionResult> EditAttendee(int id, int attendeeID)
        {
            var ev = await _context.Events.Include(e => e.attendees).FirstOrDefaultAsync(e => e.id == id);

            var attendee = ev?.attendees.FirstOrDefault(a => a.id == attendeeID);

            if (attendee == null)
            {
                return NotFound();
            }

            // Pass the Event ID via ViewBag if Attendee doesn't have an EventId property
            ViewBag.EventId = id;
            return View(attendee);
        }

        [Route("/[controller]/{id}/attendees/{attendeeID}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAttendee(int id, int attendeeID, Attendee updatedAttendee)
        {
            var ev = await _context.Events.Include(e => e.attendees).FirstOrDefaultAsync(e => e.id == id);

            var existingAttendee = ev?.attendees.FirstOrDefault(a => a.id == attendeeID);

            if (existingAttendee == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Copy updated fields onto the tracked entity
                existingAttendee.name = updatedAttendee.name;
                existingAttendee.email = updatedAttendee.email;

                await _context.SaveChangesAsync();
                return RedirectToAction("ManageAttendee", new { id = id });
            }

            ViewBag.EventId = id;
            return View(updatedAttendee);
        }


        // GET or POST: Attendees/Delete?email=alice@example.com
        public async Task<IActionResult> DeleteAttendee(string email)
        {
            // Example: Deleting an attendee via the parent Event
            var eventItem = await _context.Events
                .Include(e => e.attendees)
                .FirstOrDefaultAsync(e => e.attendees.Any(a => a.email == email));

            var attendee = eventItem?.attendees.FirstOrDefault(a => a.email == email);
            if (attendee != null)
            {
                eventItem?.attendees.Remove(attendee);
                await _context.SaveChangesAsync();
                return RedirectToAction("ManageAttendees", "Events", new { id = attendee.EventID });
            }
            return NotFound();
        }

        [Route("/[controller]/{id}/Attendees")]
        public async Task<IActionResult> ManageAttendee(int id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // .Include(e => e.attendees) loads all related Attendee records for this Event
            var Attendees = await _context.Events
                .Include(e => e.attendees)
                .FirstOrDefaultAsync(e => e.id == id);

            if (Attendees == null)
            {
                return NotFound();
            }

            return View(Attendees);
        }


        [Route("/[controller]/{id}/details")]
        [HttpGet]
        public async Task<IActionResult> DetailEvent(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ev = await _context.Events
                .Include(e => e.attendees)
                .FirstOrDefaultAsync(e => e.id == id);

            if (ev == null)
            {
                return NotFound();
            }

            return View(ev);
        }

        [Route("/[Controller]/{id}/edit")]
        [HttpGet]
        public async Task<IActionResult> EditEvent(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ev = await _context.Events.FindAsync(id);
            if (ev == null)
            {
                return NotFound();
            }

            var dto = new EventDto
            {
                Title = ev.title,
                Description = ev.description,
                Date = ev.date,
                Location = ev.location
            };

            ViewBag.EventId = ev.id;
            ViewBag.ExistingBannerUrl = ev.bannerurl;
            return View(dto);
        }

        [Route("/[Controller]/{id}/edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(int id, EventDto dto)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                ev.title = dto.Title;
                ev.description = dto.Description;
                ev.date = dto.Date;
                ev.location = dto.Location;

                // Only replace the banner if a new file was uploaded
                if (dto.BannerImage != null && dto.BannerImage.Length > 0)
                {
                    var containerClient = await GetOrCreateContainerClientAsync();
                    await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

                    string uniqueFileName = $"{Guid.NewGuid()}_{dto.BannerImage.FileName}";
                    var blobClient = containerClient.GetBlobClient(uniqueFileName);

                    using var stream = dto.BannerImage.OpenReadStream();
                    await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = dto.BannerImage.ContentType });

                    ev.bannerurl = blobClient.Uri.ToString();
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Event '{ev.title}' updated successfully.";
                return RedirectToAction("EventManager");
            }

            ViewBag.EventId = id;
            ViewBag.ExistingBannerUrl = ev.bannerurl;
            return View(dto);
        }

        [Route("/[controller]/{id}/delete")]
        [HttpGet]
        public async Task<IActionResult> DeleteEvent(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ev = await _context.Events.Include(e => e.attendees).FirstOrDefaultAsync(e => e.id == id);

            if (ev == null)
            {
                return NotFound();
            }

            return View(ev);
        }

        [Route("/[controller]/{id}/delete")]
        [HttpPost, ActionName("DeleteEvent")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEventConfirmed(int id)
        {
            var ev = await _context.Events
                .Include(e => e.attendees)
                .FirstOrDefaultAsync(e => e.id == id);

            if (ev != null)
            {
                // Remove related attendees first to prevent foreign key constraint conflicts
                if (ev.attendees != null && ev.attendees.Any())
                {
                    _context.RemoveRange(ev.attendees);
                }

                _context.Events.Remove(ev);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Event '{ev.title}' was deleted.";
            }

            return RedirectToAction("EventManager");
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


        private async Task<BlobContainerClient> GetOrCreateContainerClientAsync()
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            if (await containerClient.ExistsAsync())
            {
                return containerClient;
            }
            else
            {
                containerClient = await _blobServiceClient.CreateBlobContainerAsync(_containerName, Azure.Storage.Blobs.Models.PublicAccessType.BlobContainer);
                return containerClient;
            }
        }
    }

}
