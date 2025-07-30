using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using MudBlazor.Extensions;
using ScarletPigsServices.Website.Data.Models.Events;
using ScarletPigsServices.Website.Data.Services.HTTP;

namespace ScarletPigsServices.Website.Components.Helper
{
    public partial class ScarletPigsCalendar
    {
        // Service Injection
        [Inject]
        public IScarletPigsApi Api { get; set; } = default!;
        [Inject]
        public NavigationManager NavigationManager { get; set; } = default!;
        [Inject]
        public AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

        // Component references
        public MudMessageBox EventList { get; set; } = default!;

        // Properties
        public List<CalendarEvent> Events { get; set; } = new List<CalendarEvent>();
        public List<CalendarEvent> SelectedEvents { get; set; } = new List<CalendarEvent>();
        public DateTime SelectedDate { get; set; } = DateTime.UtcNow;

        protected override Task OnInitializedAsync()
        {
            Task.Run(async () =>
            {
                Events = (await Api.GetEventsAsync()).Select(x => x.ToCalendarEvent()).ToList();
                await InvokeAsync(StateHasChanged);
            });

            return base.OnInitializedAsync();
        }

        public Task CellClicked(DateTime dateTime)
        {
            // Don't navigate if the user is not authenticated
            if (AuthenticationStateProvider.GetAuthenticationStateAsync().GetAwaiter().GetResult().User.Identity?.IsAuthenticated != true)
                return Task.CompletedTask;

            // Set selected events to be equal to all events that are intersecting with the day clicked
            SelectedEvents = Events.Where(x => x.StartTime.Date == dateTime.Date || x.EndTime.Date == dateTime.Date).ToList();
            SelectedDate = dateTime.ToUniversalTime();

            // Open the event list
            EventList.ShowAsync();

            return Task.CompletedTask;

            // Get all events that are happening at the clicked time
            var events = Events.Where(x => x.StartTime <= dateTime && x.EndTime >= dateTime).ToList();

            // If there are no events, navigate to the create event page
            if (events.Count == 0)
            {
                NavigationManager.NavigateTo("/events/create");
                return Task.CompletedTask;
            }


            return Task.CompletedTask;
        }

        public Task AddNewEvent()
        {
            // Set from time to the selected date at 15 o'clock
            DateTime fromTime = SelectedDate.Date.AddHours(15);
            DateTime toTime = SelectedDate.Date.AddHours(18);

            string uriTarget = NavigationManager.GetUriWithQueryParameters($"/events/create", new Dictionary<string, object?>
            {
                { "fromTime", fromTime },
                { "toTime", toTime }
            });

            NavigationManager.NavigateTo(uriTarget);
            return Task.CompletedTask;
        }



    }
}