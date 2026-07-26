using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScarletPigsServices.Api.Extensions;
using ScarletPigsServices.Data.Auth;

namespace ScarletPigsServices.Api.Controllers
{
    [ApiController]
    [Route("users")]
    public sealed class UsersController : ControllerBase
    {
        [Authorize]
        [HttpGet("me")]
        [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public ActionResult<CurrentUserResponse> GetCurrentUser()
        {
            return Ok(User.ToCurrentUserResponse());
        }
    }
}