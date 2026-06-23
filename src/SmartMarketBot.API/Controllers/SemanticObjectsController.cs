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

    /// <summary>Ghép đôi (Mapping): Admin bấm vào hình chữ nhật "Kệ Sữa" trên Web Manager, gọi API này gán Product vào SemanticObject.
    /// Từ nay, hệ thống biết: Khách tìm Vinamilk → Chạy đến Node đứng gần kệ này!</summary>
    [HttpPost("{objectId:int}/assign-product")]
    public async Task<ActionResult<SemanticObjectDto>> AssignProduct(
        int objectId,
        [FromBody] AssignProductRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await semanticObjectService.AssignProductAsync(objectId, request.ProductId, cancellationToken);
        return Ok(result);
    }

    /// <summary>Xoá mapping: bỏ gán sản phẩm khỏi Semantic Object.</summary>
    [HttpDelete("{objectId:int}/assign-product")]
    public async Task<ActionResult<SemanticObjectDto>> UnassignProduct(int objectId, CancellationToken cancellationToken)
    {
        var result = await semanticObjectService.UnassignProductAsync(objectId, cancellationToken);
        return Ok(result);
    }
}
