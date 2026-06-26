using System.Collections.Generic;
using SmartMarketBot.Application.Models.Admin;
using SmartMarketBot.Application.Models.Common;

namespace SmartMarketBot.Application.Interfaces;

public interface IAdminUserService
{
    Task<PaginatedResponse<UserDto>> GetUsersAsync(
        string? username,
        string? email,
        string? role,
        string? status,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<UserDto?> GetUserByIdAsync(int accountId, CancellationToken cancellationToken = default);
    Task<UserDto> CreateUserAsync(CreateUserRequestDto request, CancellationToken cancellationToken = default);
    Task<UserDto> UpdateUserAsync(int accountId, UpdateUserRequestDto request, CancellationToken cancellationToken = default);
    Task<UserDto> UpdateUserRoleAsync(int accountId, UpdateUserRoleRequestDto request, CancellationToken cancellationToken = default);
    Task<UserDto> UpdateUserStatusAsync(int accountId, UpdateUserStatusRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> DeleteUserAsync(int accountId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default);
}
