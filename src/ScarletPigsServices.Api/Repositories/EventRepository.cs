using Microsoft.EntityFrameworkCore;
using ScarletPigsServices.Data;
using ScarletPigsServices.Data.Events;

namespace ScarletPigsServices.Api.Repositories
{
    public interface IEventRepository
    {
        public Task<IEnumerable<Event>> GetEvents();
        public Task<IEnumerable<Event>> GetEvents(DateTime fromDate, DateTime toDate);
        public Task<Event> GetEvent(int id);
        public Task<Event> CreateEvent(CreateEventDTO eventdto);
        public Task<Event> UpdateEvent(EditEventDTO eventobj);
        public Task<Event> DeleteEvent(int id);
    }

    public class EventRepository : IEventRepository
    {
        private ScarletPigsDbContext DBContext { get; set; }

        public EventRepository(ScarletPigsDbContext context)
        {
            DBContext = context;
        }

        public async Task<IEnumerable<Event>> GetEvents()
        {
            return await DBContext.Events.AsNoTracking().ToListAsync();
        }

        public async Task<IEnumerable<Event>> GetEvents(DateTime fromDate, DateTime toDate)
        {
            return await DBContext.Events.AsNoTracking().Where(e => e.StartTime >= fromDate && e.EndTime <= toDate).ToListAsync();
        }

        public async Task<Event?> GetEvent(int id)
        {
            return await DBContext.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<Event> CreateEvent(CreateEventDTO eventdto)
        {
            var eventobj = new Event
            {
                Name = eventdto.Name,
                CreatorDiscordUsername = eventdto.CreatorDiscordUsername,
                Description = eventdto.Description,
                CreatedAt = DateTime.Now.ToUniversalTime(),
                LastModified = DateTime.Now.ToUniversalTime(),
                StartTime = eventdto.StartTime.ToUniversalTime(),
                EndTime = eventdto.EndTime.ToUniversalTime()
            };
            await DBContext.Events.AddAsync(eventobj);
            await DBContext.SaveChangesAsync();
            return eventobj;
        }

        public async Task<Event> UpdateEvent(EditEventDTO eventobj)
        {
            Event? evnt = await DBContext.Events.FirstOrDefaultAsync(e => e.Id == eventobj.Id);

            if (evnt == null)
                throw new Exception("Event not found");

            evnt.LastModified = DateTime.Now.ToUniversalTime();

            evnt.Name = eventobj.Name ?? evnt.Name;
            evnt.Description = eventobj.Description ?? evnt.Description;
            evnt.StartTime = (eventobj.StartTime ?? evnt.StartTime).ToUniversalTime();
            evnt.EndTime = (eventobj.EndTime ?? evnt.EndTime).ToUniversalTime();

            DBContext.Events.Update(evnt);
            await DBContext.SaveChangesAsync();
            return evnt;
        }

        public async Task<Event> DeleteEvent(int id)
        {
            var eventobj = await DBContext.Events.FirstOrDefaultAsync(e => e.Id == id);
            DBContext.Events.Remove(eventobj);
            await DBContext.SaveChangesAsync();
            return eventobj;
        }
    }
}
