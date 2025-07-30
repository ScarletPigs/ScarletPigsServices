using Microsoft.EntityFrameworkCore;
using ScarletPigsServices.Data.Events;

namespace ScarletPigsServices.Data
{
    public class ScarletPigsDbContext : DbContext
    {
        public ScarletPigsDbContext(DbContextOptions<ScarletPigsDbContext> options) : base(options)
        {

        }

        public DbSet<Event> Events => Set<Event>();
        public DbSet<EventType> EventTypes => Set<EventType>();
        
    }
}
