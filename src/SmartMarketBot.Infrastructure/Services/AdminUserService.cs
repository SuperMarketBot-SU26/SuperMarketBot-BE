using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Admin;
using SmartMarketBot.Application.Models.Common;
using SmartMarketBot.Domain.Common;
using SmartMarketBot.Domain.Entities;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

public sealed class AdminUserService(AppDbContext dbContext) : IAdminUserService
{
    private static readonly IReadOnlySet<string> AllowedRoles = new HashSet<string>
    {
        Roles.Admin,
        Roles.Staff,
        Roles.Member
    };

    private static readonly IReadOnlySet<string> AllowedStatuses = new HashSet<string>
    {
        "Active",
        "Inactive",
        "Pending",
        "Blocked"
    };

    public async Task<PaginatedResponse<UserDto>> GetUsersAsync(
        string? username,
        string? email,
        string? role,
        string? status,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Accounts.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(username))
        {
            query = query.Where(x => x.Username.Contains(username));
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            query = query.Where(x => x.Email.Contains(email));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            query = query.Where(x => x.Role == role);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var items = await query
            .OrderBy(x => x.AccountId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new UserDto(
                x.AccountId,
                x.Username,
                x.Email,
                x.FullName,
                x.Phone,
                x.Status,
                x.Role,
                x.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<UserDto>(items, totalCount, pageNumber, pageSize, totalPages);
    }

    public async Task<UserDto?> GetUserByIdAsync(int accountId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Accounts
            .AsNoTracking()
            .Where(x => x.AccountId == accountId)
            .Select(x => new UserDto(
                x.AccountId,
                x.Username,
                x.Email,
                x.FullName,
                x.Phone,
                x.Status,
                x.Role,
                x.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!AllowedRoles.Contains(request.Role))
        {
            throw new ArgumentException($"Role '{request.Role}' không hợp lệ.", nameof(request.Role));
        }

        if (!AllowedStatuses.Contains(request.Status))
        {
            throw new ArgumentException($"Status '{request.Status}' không hợp lệ.", nameof(request.Status));
        }

        if (await dbContext.Accounts.AnyAsync(x => x.Username == request.Username, cancellationToken))
        {
            throw new InvalidOperationException($"Username '{request.Username}' đã tồn tại.");
        }

        if (await dbContext.Accounts.AnyAsync(x => x.Email == request.Email, cancellationToken))
        {
            throw new InvalidOperationException($"Email '{request.Email}' đã tồn tại.");
        }

        var account = new Account
        {
            Username = request.Username,
            Email = request.Email,
            FullName = request.FullName,
            Phone = request.Phone,
            Role = request.Role,
            Status = request.Status,
            PasswordHash = HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new UserDto(
            account.AccountId,
            account.Username,
            account.Email,
            account.FullName,
            account.Phone,
            account.Status,
            account.Role,
            account.CreatedAt);
    }

    public async Task<UserDto> UpdateUserAsync(int accountId, UpdateUserRequestDto request, CancellationToken cancellationToken = default)
    {
        var account = await dbContext.Accounts.FindAsync(new object[] { accountId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Account {accountId} không tồn tại.");

        if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != account.Email)
        {
            if (await dbContext.Accounts.AnyAsync(x => x.Email == request.Email && x.AccountId != accountId, cancellationToken))
            {
                throw new InvalidOperationException($"Email '{request.Email}' đã tồn tại.");
            }
        }

        account.Email = request.Email;
        account.FullName = request.FullName ?? account.FullName;
        account.Phone = request.Phone ?? account.Phone;

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            if (!AllowedRoles.Contains(request.Role))
            {
                throw new ArgumentException($"Role '{request.Role}' không hợp lệ.", nameof(request.Role));
            }

            account.Role = request.Role;
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!AllowedStatuses.Contains(request.Status))
            {
                throw new ArgumentException($"Status '{request.Status}' không hợp lệ.", nameof(request.Status));
            }

            account.Status = request.Status;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new UserDto(
            account.AccountId,
            account.Username,
            account.Email,
            account.FullName,
            account.Phone,
            account.Status,
            account.Role,
            account.CreatedAt);
    }

    public async Task<UserDto> UpdateUserRoleAsync(int accountId, UpdateUserRoleRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!AllowedRoles.Contains(request.Role))
        {
            throw new ArgumentException($"Role '{request.Role}' không hợp lệ.", nameof(request.Role));
        }

        var account = await dbContext.Accounts.FindAsync(new object[] { accountId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Account {accountId} không tồn tại.");

        account.Role = request.Role;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new UserDto(
            account.AccountId,
            account.Username,
            account.Email,
            account.FullName,
            account.Phone,
            account.Status,
            account.Role,
            account.CreatedAt);
    }

    public async Task<UserDto> UpdateUserStatusAsync(int accountId, UpdateUserStatusRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!AllowedStatuses.Contains(request.Status))
        {
            throw new ArgumentException($"Status '{request.Status}' không hợp lệ.", nameof(request.Status));
        }

        var account = await dbContext.Accounts.FindAsync(new object[] { accountId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Account {accountId} không tồn tại.");

        account.Status = request.Status;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new UserDto(
            account.AccountId,
            account.Username,
            account.Email,
            account.FullName,
            account.Phone,
            account.Status,
            account.Role,
            account.CreatedAt);
    }

    public async Task<bool> DeleteUserAsync(int accountId, CancellationToken cancellationToken = default)
    {
        var account = await dbContext.Accounts.FindAsync(new object[] { accountId }, cancellationToken);
        if (account is null) return false;

        account.Status = "Inactive";
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<RoleDto> roles = new[]
        {
            new RoleDto(Roles.Admin, "Toàn quyền điều hành hệ thống."),
            new RoleDto(Roles.Staff, "Nhân viên vận hành và hỗ trợ tại siêu thị."),
            new RoleDto(Roles.Member, "Người dùng hội viên/khách hàng.")
        };

        return Task.FromResult(roles);
    }

    private static string HashPassword(string password)
    {
        const int iterations = 100_000;
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, 32);
        return $"pbkdf2${iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }
}
