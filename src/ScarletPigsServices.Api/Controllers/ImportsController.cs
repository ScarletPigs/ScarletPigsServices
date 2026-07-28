using Microsoft.AspNetCore.Mvc;
using ScarletPigsServices.Api.Services.Imports;

namespace ScarletPigsServices.Api.Controllers;

[ApiController]
[Route("api/admin/imports")]
public sealed class ImportsController(
    ILegacyGoogleSheetsImportService importService) : ControllerBase
{
    [HttpPost("google-sheets")]
    [ProducesResponseType<LegacyGoogleSheetsImportResult>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LegacyGoogleSheetsImportResult>> ImportGoogleSheets(
        CancellationToken cancellationToken) =>
        Ok(await importService.ImportAsync(cancellationToken));
}
