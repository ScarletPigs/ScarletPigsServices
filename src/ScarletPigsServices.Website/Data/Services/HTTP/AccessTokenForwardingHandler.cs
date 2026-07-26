using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;

namespace ScarletPigsServices.Website.Data.Services.HTTP
{
    public sealed class AccessTokenForwardingHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AccessTokenForwardingHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Headers.Authorization is null)
            {
                var accessToken = await _httpContextAccessor.HttpContext?.GetTokenAsync("access_token")!;
                if (!string.IsNullOrWhiteSpace(accessToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}