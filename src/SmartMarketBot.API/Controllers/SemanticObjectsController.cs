using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.SemanticObjects;

namespace SmartMarketBot.API.Controllers;

[ApiController]
[Route("api/v1/semantic-objects")]
public sealed class SemanticObjectsController(
    ISemanticObjectService semanticObjectService) : ControllerBase
{
    /// <summary>Lấy danh sách tất cả Semantic Objects (hình chữ nhật / kệ hàng) trên map. Có phân trang.</summary>
    [HttpGet]
    public async Task<ActionResult<SemanticObjectListResponseDto>> GetAll(
        [FromQuery] int? mapId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var result = await semanticObjectService.GetAllAsync(mapId, pageNumber, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>Lấy chi tiết một Semantic Object.</summary>
    [HttpGet("{objectId:int}")]
    public async Task<ActionResult<SemanticObjectDto>> GetById(int objectId, CancellationToken cancellationToken)
    {
        var obj = await semanticObjectService.GetByIdAsync(objectId, cancellationToken);
        return obj is null ? NotFound() : Ok(obj);
    }

    /// <summary>Ghép đôi (Mapping): Admin bấm vào hình chữ nhật "Kệ Sữa" trên Web Manager, gọi API này gán ProductType vào SemanticObject.</summary>
    [HttpPost("{objectId:int}/assign-product-type")]
    public async Task<ActionResult<SemanticObjectDto>> AssignProductType(
        int objectId,
        [FromBody] AssignProductTypeRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await semanticObjectService.AssignProductTypeAsync(objectId, request.ProductTypeId, cancellationToken);
        return Ok(result);
    }

    /// <summary>Xoá mapping: bỏ gán ProductType khỏi Semantic Object.</summary>
    [HttpDelete("{objectId:int}/assign-product-type")]
    public async Task<ActionResult<SemanticObjectDto>> UnassignProductType(int objectId, CancellationToken cancellationToken)
    {
        var result = await semanticObjectService.UnassignProductTypeAsync(objectId, cancellationToken);
        return Ok(result);
    }
}
