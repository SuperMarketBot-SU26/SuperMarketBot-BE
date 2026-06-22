using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Ads;

namespace SmartMarketBot.API.Controllers;

[ApiController]
[Route("api/v1/ad-resources")]
public sealed class AdResourcesController(
    IAdResourceService resourceService,
    IFileStorageService fileStorage) : ControllerBase
{
    [HttpGet("campaign/{campaignId:int}")]
    public async Task<ActionResult<PaginatedResponse<AdResourceDto>>> GetByCampaign(
        int campaignId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await resourceService.GetByCampaignAsync(campaignId, pageNumber, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{resourceId:int}")]
    public async Task<ActionResult<AdResourceDto>> GetById(int resourceId, CancellationToken cancellationToken)
    {
        var resource = await resourceService.GetByIdAsync(resourceId, cancellationToken);
        if (resource is null)
            return NotFound();
        return Ok(resource);
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<AdResourceDto>> UploadResource(
        [FromForm] UploadAdResourceRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length == 0)
            return BadRequest(new { message = "File is required." });

        await using var stream = request.File.OpenReadStream();
        var relativePath = await fileStorage.SaveAsync(stream, request.File.FileName, "ad-resources", cancellationToken);

        var createRequest = new CreateAdResourceRequestDto
        {
            AdCampaignId = request.AdCampaignId,
            ResourceType = request.ResourceType,
            ResourceUrl = fileStorage.GetAbsoluteUrl(relativePath),
            ContentText = request.ContentText,
            Resolution = request.Resolution
        };

        var resource = await resourceService.CreateAsync(createRequest, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { resourceId = resource.ResourceId }, resource);
    }

    [HttpPost]
    public async Task<ActionResult<AdResourceDto>> Create(
        [FromBody] CreateAdResourceRequestDto request,
        CancellationToken cancellationToken)
    {
        var resource = await resourceService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { resourceId = resource.ResourceId }, resource);
    }

    [HttpPatch("{resourceId:int}/status")]
    public async Task<ActionResult<AdResourceDto>> UpdateStatus(
        int resourceId,
        [FromBody] UpdateResourceStatusDto request,
        CancellationToken cancellationToken)
    {
        var resource = await resourceService.UpdateStatusAsync(resourceId, request.Status, cancellationToken);
        return Ok(resource);
    }

    [HttpDelete("{resourceId:int}")]
    public async Task<ActionResult> Delete(int resourceId, CancellationToken cancellationToken)
    {
        var deleted = await resourceService.DeleteAsync(resourceId, cancellationToken);
        if (!deleted)
            return NotFound();
        return NoContent();
    }
}

public sealed record UpdateResourceStatusDto(string Status);

public sealed record UploadAdResourceRequestDto(
    [FromForm] int AdCampaignId,
    [FromForm] string ResourceType,
    [FromForm] IFormFile? File,
    [FromForm] string? ContentText,
    [FromForm] string? Resolution);
