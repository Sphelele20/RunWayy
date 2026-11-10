using Microsoft.AspNetCore.Mvc;
using RunWay.Api.Services;

namespace RunWay.Api.Controllers;

/// <summary>Streams images stored in Azure Blob Storage. The MVC app never talks to Azure directly.</summary>
[ApiController]
[Route("api/files")]
public class FilesController : ControllerBase
{
    private readonly IFileStorageService _files;

    public FilesController(IFileStorageService files) => _files = files;

    [HttpGet("{**name}")]
    public async Task<IActionResult> Get(string name, CancellationToken ct)
    {
        // Only the two image folders are public, and no path tricks.
        var valid = (name.StartsWith("events/") || name.StartsWith("profiles/")) && !name.Contains("..");
        if (!valid) return NotFound();

        var blob = await _files.OpenReadAsync(name, ct);
        if (blob == null) return NotFound();

        Response.Headers.CacheControl = "public,max-age=3600";
        return File(blob.Value.Content, blob.Value.ContentType);
    }
}
