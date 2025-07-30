using Microsoft.AspNetCore.Components.Authorization;
using ScarletPigsServices.Website.Data.Models.Auth;

namespace ScarletPigsServices.Website.Data.Services.Auth
{
    public class KeycloakAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public KeycloakAuthenticationStateProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var principal = _httpContextAccessor.HttpContext.User;
            var customUser = new KeycloakUser(principal);
            return Task.FromResult(new AuthenticationState(customUser));
        }
    }
}
