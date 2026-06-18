using SmartMarketBot.Application.Models.Realtime;

namespace SmartMarketBot.Application.Interfaces;

/// <summary>Abstraction cho SignalR hub client (Member side).</summary>
public interface IMemberRealtimeNotifier
{
    Task PushToMemberAsync(int memberId, MemberRealtimeUpdateDto update, CancellationToken ct = default);
}