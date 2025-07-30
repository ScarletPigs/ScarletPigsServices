using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components;
using Piglet_Domain_Models.Models;
using ScarletPigsServices.Data.Events;
using ScarletPigsServices.Website.Data.Models.Auth;
using ScarletPigsServices.Website.Data.Services.HTTP;

namespace ScarletPigsServices.Website.Components.Pages.Events
{
    public partial class CreateEvent
    {

        [Parameter, SupplyParameterFromQuery]
        public DateTime? fromTime { get; set; } = default!;

        [Parameter, SupplyParameterFromQuery]
        public DateTime? toTime { get; set; } = default!;

        [Inject]
        private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

        [Inject]
        public IScarletPigsApi Api { get; set; } = default!;

        [Inject]
        public NavigationManager NavigationManager { get; set; } = default!;

        public CreateEventDTO CreateEventDTO { get; set; } = default!;

        KeycloakUser? User { get; set; }
        int success { get; set; } = 0; // 0 = not yet, 1 = success, 2 = error


        protected override Task OnInitializedAsync()
        {
            Task.Run(async () =>
            {
                var authState = await AuthStateProvider.GetAuthenticationStateAsync();
                User = (KeycloakUser?)authState.User;

                if (User == null)
                    NavigationManager.NavigateTo("/");

                // Initialize the EditEventDTO with the current event's data
                CreateEventDTO = new CreateEventDTO()
                {
                    Name = "",
                    Description = "",
                    Author = User.UserName,
                    CreatorDiscordUsername = User.Id,
                    StartTime = fromTime ?? DateTime.UtcNow,
                    EndTime = toTime ?? DateTime.UtcNow
                };


                await InvokeAsync(StateHasChanged);
            });

            return base.OnInitializedAsync();
        }

        public async Task OnCreateEvent()
        {

            if (await Api.CreateEventAsync(CreateEventDTO) != null)
                success = 1;
            else
                success = 2;



            if (success == 1)
                NavigationManager.NavigateTo($"/");
        }


        public async Task Cancel()
        {
            NavigationManager.NavigateTo($"/");
        }
    }
}