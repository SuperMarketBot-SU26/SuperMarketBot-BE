using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Models.Admin;
using SmartMarketBot.Domain.Common;

namespace SmartMarketBot.API.Controllers;

[ApiController]
[Route("api/v1/admin/roles")]
[Authorize(Roles = Roles.Admin)]
public sealed class AdminRolesController : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyList<RoleDto>> GetRoles()
    {
        var roles = new RoleDto[]
        {
            new RoleDto(Roles.Admin, "Toàn quyền điều hành hệ thống."),
            new RoleDto(Roles.Staff, "Nhân viên vận hành và hỗ trợ tại siêu thị."),
            new RoleDto(Roles.Member, "Người dùng hội viên/khách hàng.")
        };

        return Ok(roles);
    }
}
