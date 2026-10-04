using System.Diagnostics;
using Assignment1.Models;
using Microsoft.AspNetCore.Mvc;

namespace Assignment1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private static readonly List<Event> _events = new()
        {
            new Event { id = 1, title = "Career Talk", date = new DateTime(2026, 12,01), location = "Gym", attendees = new List<Attendee>() },
            new Event { id = 2, title = "Tech Talk", date = new DateTime(2026, 12,05), location = "Library",attendees = new List<Attendee>()},
            new Event { id = 3, title = "Hack Night", date = new DateTime(2026, 11,21), location = "Computer Lab",attendees = new List<Attendee>()}
        };
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult EventManager()
        {
            return View(_events);
        }


        [HttpGet]
        public IActionResult ManageAttendee(int id)
        {
            var events = _events.FirstOrDefault(e => e.id == id);
            if (events == null)
            {
                return NotFound();
            }
            return View(events);
        }

        [HttpPost]
        public IActionResult ManageAttendee(int id, Attendee attendee)
        {
            // 1. Find the specific event in your database or repository

            var events = _events.FirstOrDefault(e => e.id == id);
            if (events == null)
            {
                return NotFound();
            }

            events.attendees.Add(attendee);

            TempData["SuccessMessage"] = "Attendee added!";
            return View(events);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
