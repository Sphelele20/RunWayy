using Microsoft.AspNetCore.Mvc;
using RunWay.Web.Services;

namespace RunWay.Web.Controllers;

/// <summary>
/// Serves images (event banners, profile pictures). The bytes come from the API, which reads them
/// from Azure Blob Storage, so the browser and this app never talk to Azure directly.
/// </summary>
public class FilesController : Controller
{
    private readonly RaceDayApiClient _api;

    public FilesController(RaceDayApiClient api) => _api = api;

    [HttpGet("/img/{**name}")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Image(string name)
    {
        var result = await _api.GetFileAsync(name);
        if (!result.Success || result.Data == null) return NotFound();
        return File(result.Data.Bytes, result.Data.ContentType);
    }
}
