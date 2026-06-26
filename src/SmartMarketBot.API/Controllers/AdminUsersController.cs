using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Admin;
using SmartMarketBot.Domain.Common;

namespace SmartMarketBot.API.Controllers;

[ApiController]
[Route("api/v1/admin/users")]
[Authorize(Roles = Roles.Admin)]
public sealed class AdminUsersController(IAdminUserService adminUserService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> GetUsers(
        [FromQuery] string? username,
        [FromQuery] string? email,
        [FromQuery] string? role,
        [FromQuery] string? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await adminUserService.GetUsersAsync(username, email, role, status, pageNumber, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{accountId:int}")]
    public async Task<ActionResult<UserDto>> GetById(int accountId, CancellationToken cancellationToken = default)
    {
        var user = await adminUserService.GetUserByIdAsync(accountId, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserRequestDto request, CancellationToken cancellationToken = default)
    {
        var created = await adminUserService.CreateUserAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { accountId = created.AccountId }, created);
    }

    [HttpPut("{accountId:int}")]
    public async Task<ActionResult<UserDto>> Update(int accountId, [FromBody] UpdateUserRequestDto request, CancellationToken cancellationToken = default)
    {
        var updated = await adminUserService.UpdateUserAsync(accountId, request, cancellationToken);
        return Ok(updated);
    }

    [HttpPatch("{accountId:int}/role")]
    public async Task<ActionResult<UserDto>> UpdateRole(int accountId, [FromBody] UpdateUserRoleRequestDto request, CancellationToken cancellationToken = default)
    {
        var updated = await adminUserService.UpdateUserRoleAsync(accountId, request, cancellationToken);
        return Ok(updated);
    }

    [HttpPatch("{accountId:int}/status")]
    public async Task<ActionResult<UserDto>> UpdateStatus(int accountId, [FromBody] UpdateUserStatusRequestDto request, CancellationToken cancellationToken = default)
    {
        var updated = await adminUserService.UpdateUserStatusAsync(accountId, request, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{accountId:int}")]
    public async Task<IActionResult> Delete(int accountId, CancellationToken cancellationToken = default)
    {
        var success = await adminUserService.DeleteUserAsync(accountId, cancellationToken);
        return success ? NoContent() : NotFound();
    }
}
