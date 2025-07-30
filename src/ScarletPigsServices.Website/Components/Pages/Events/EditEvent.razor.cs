using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components;
using ScarletPigsServices.Website.Data.Models.Auth;
using ScarletPigsServices.Website.Data.Services.HTTP;
using ScarletPigsServices.Data.Events;

namespace ScarletPigsServices.Website.Components.Pages.Events
{
    public partial class EditEvent
    {
        [Parameter]
        public string id { get; set; } = string.Empty;

        [Inject]
        private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

        [Inject]
        public IScarletPigsApi Api { get; set; } = default!;

        [Inject]
        public NavigationManager NavigationManager { get; set; } = default!;

        public Event? CurrentEvent { get; set; }

        public EditEventDTO EditEventDTO { get; set; } = default!;

        KeycloakUser? User { get; set; }
        int success { get; set; } = 0; // 0 = not yet, 1 = success, 2 = error


        protected override Task OnInitializedAsync()
        {
            Task.Run(async () =>
            {
                var authState = await AuthStateProvider.GetAuthenticationStateAsync();
                User = (KeycloakUser?)authState.User;

                CurrentEvent = await Api.GetEventAsync(id);

                if (CurrentEvent == null || User == null)
                    NavigationManager.NavigateTo("/");

                if (User?.Id != CurrentEvent?.CreatorDiscordUsername)
                    NavigationManager.NavigateTo($"/events/{id}");

                // Initialize the EditEventDTO with the current event's data
                EditEventDTO = new EditEventDTO() { Id = 1 };

                EditEventDTO.Name = CurrentEvent.Name;
                EditEventDTO.Description = CurrentEvent.Description;
                EditEventDTO.CreatorDiscordUsername = CurrentEvent.CreatorDiscordUsername;
                EditEventDTO.StartTime = CurrentEvent.StartTime;
                EditEventDTO.EndTime = CurrentEvent.EndTime;


                await InvokeAsync(StateHasChanged);
            });

            return base.OnInitializedAsync();
        }

        public async Task UpdateEvent()
        {
            
            if (CurrentEvent == null)
                return;

            if (await Api.UpdateEventAsync(CurrentEvent.Id.ToString(), EditEventDTO))
                success = 1;
            else
                success = 2;
        }

        public async Task DeleteEvent()
        {
            if (CurrentEvent == null)
                return;

            await Api.DeleteEventAsync(CurrentEvent.Id.ToString());
            NavigationManager.NavigateTo("/events");
        }

        public async Task Cancel()
        {
            NavigationManager.NavigateTo($"/events/{id}");
        }
    }
}