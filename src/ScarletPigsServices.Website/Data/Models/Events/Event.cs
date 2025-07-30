using Heron.MudCalendar;
using ScarletPigsServices.Data.Events;
using ScarletPigsServices.Website.Data.Models.ModLists;

namespace ScarletPigsServices.Website.Data.Models.Events
{
    public static class EventExtensions
    {
        public static CalendarEvent ToCalendarEvent(this Event apievent)
        {
            return new CalendarEvent(apievent);
        }
    }

    public class CalendarEvent : CalendarItem
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public EventType EventType { get; set; }
        public string Author { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public ModList Modset { get; set; }

        public string ApiId { get; set; }

        public CalendarEvent(Event apievent)
        {
            ApiId = apievent.Id.ToString();

            Name = apievent.Name;
            Description = apievent.Description;
            Author = apievent.Author;
            CreatedAt = apievent.CreatedAt;
            LastUpdatedAt = apievent.LastModified;
            StartTime = apievent.StartTime;
            EndTime = apievent.EndTime;

            if (Modset == null)
            {
                Modset = new ModList();
            }
            if (EventType == null)
            {
                EventType = new EventType();
            }

            // Set the calendar item properties
            Text = Name;
            Start = StartTime;
            End = EndTime;
            AllDay = false;
        }
    }
}
