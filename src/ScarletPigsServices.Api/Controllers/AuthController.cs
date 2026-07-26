using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ScarletPigsServices.Api.Authentication;
using ScarletPigsServices.Data.Auth;

namespace ScarletPigsServices.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ITokenService tokenService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TokenResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors
                .GroupBy(error => error.Code)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(error => error.Description).ToArray());

            return BadRequest(new ValidationProblemDetails(errors));
        }

        return Ok(await tokenService.IssueAsync(user, cancellationToken));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            return Unauthorized();
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            return Unauthorized();
        }

        return Ok(await tokenService.IssueAsync(user, cancellationToken));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponse>> Refresh(RefreshRequest request, CancellationToken cancellationToken)
    {
        var response = await tokenService.RefreshAsync(request.RefreshToken, cancellationToken);
        return response is null ? Unauthorized() : Ok(response);
    }

    [Authorize]
    [HttpPost("revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Revoke(RevokeRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!string.IsNullOrWhiteSpace(userId))
        {
            await tokenService.RevokeAsync(userId, request.RefreshToken, cancellationToken);
        }

        return NoContent();
    }
}
