using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;

namespace SmartMarketBot.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class VisionController(IAiVisionProxy aiVisionProxy) : ControllerBase
{
    [HttpPost("upload")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<object>> Upload([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest("File is empty.");
        }

        await using var ms = new MemoryStream();
        await file.CopyToAsync(ms, cancellationToken);
        var result = await aiVisionProxy.AnalyzeImageAsync(ms.ToArray(), file.FileName, cancellationToken);

        return Ok(new
        {
            fileName = file.FileName,
            result
        });
    }
}
