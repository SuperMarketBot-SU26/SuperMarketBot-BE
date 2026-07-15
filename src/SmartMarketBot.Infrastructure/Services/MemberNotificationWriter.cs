using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Domain.Common;
using SmartMarketBot.Domain.Entities;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

public sealed class MemberNotificationWriter(
    AppDbContext db,
    ILogger<MemberNotificationWriter> logger) : IMemberNotificationWriter
{
    public async Task SaveAsync(
        int memberId,
        string notifType,
        string title,
        string message,
        string? payloadJson = null,
        CancellationToken ct = default)
    {
        try
        {
            db.MemberNotifications.Add(new MemberNotification
            {
                MemberId    = memberId,
                NotifType   = notifType,
                Title       = title,
                Message     = message,
                PayloadJson = payloadJson,
                IsRead      = false,
                CreatedAt   = VnDateTime.Now
            });
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "[MemberNotificationWriter] Save thất bại (MemberId={Id}, Type={Type})",
                memberId, notifType);
            throw; // Caller (MemberService) đã có try/catch riêng, ném lại để log đầy đủ stack
        }
    }
}
