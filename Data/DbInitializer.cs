using Assignment1.Models;
namespace Assignment1.Data
{
    public class DbInitializer
    {
        public static void Initialize(EventDBContext context)
        {
            context.Database.EnsureCreated();

            if (context.Events.Any())
            {
                return;
            }

            var Events = new Event[]{
                new Event { title = "Career Talk", bannerurl= "", description = "An event for talking about careers",date = new DateTime(2026, 12,01), location = "Gym", 
                    attendees = new List<Attendee>{
                        new Attendee { name = "Alice Johnson", email = "alice.johnson@example.com", EventID = 1 },
                        new Attendee { name = "Bob Smith", email = "bob.smith@example.com", EventID = 1 } } },
                new Event { title = "Tech Talk", bannerurl= "", description="An event where we talk about tech",date = new DateTime(2026, 12,05), location = "Library",
                    attendees = new List<Attendee>{
                        new Attendee { name = "Charlie Brown", email = "charlie.brown@example.com", EventID = 2 },
                        new Attendee { name = "Diana Prince", email = "diana.prince@example.com", EventID = 2 } } },
                new Event {title = "Hack Night", bannerurl="", description="We are going to hack all night",date = new DateTime(2026, 11,21), location = "Computer Lab",
                    attendees = new List<Attendee>{
                        new Attendee { name = "Evan Wright", email = "evan.wright@example.com", EventID = 3 },
                        new Attendee { name = "Fiona Gallagher", email = "fiona.gallagher@example.com", EventID = 3 } } }
            };

            foreach (Event e in Events)
            {
                context.Add(e);
            }
            context.SaveChanges();
        }
    }
}
