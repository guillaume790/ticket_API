using Microsoft.EntityFrameworkCore;
using ticket_API.Models;

namespace ticket_API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
           : base(options)
        {
        }

        public DbSet<Ticket> Tickets { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<Event> Events { get; set; }

    }
}
