using Microsoft.EntityFrameworkCore;
using Assignment1.Models;
namespace Assignment1.Data

{
    public class EventDBContext : DbContext
    {
        public EventDBContext(DbContextOptions<EventDBContext> options) : base(options)
        {  
        }

        public DbSet<Event> Events { get; set; }
    }
}
