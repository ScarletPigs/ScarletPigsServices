using Microsoft.EntityFrameworkCore;
using Piglet_Domain_Models.Models;
using ScarletPigsServices.Data.Events;

namespace Piglet_API.Data
{
    public class PigletDBContext : DbContext
    {
        public PigletDBContext(DbContextOptions<PigletDBContext> options) : base(options)
        {
        }

        public DbSet<Event> Events => Set<Event>();
        public DbSet<EventType> EventTypes => Set<EventType>();
    }
}
