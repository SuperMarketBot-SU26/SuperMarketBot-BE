using SmartMarketBot.Application.Models.Staff;

namespace SmartMarketBot.Application.Interfaces;

/// <summary>Flow 4 — Out-of-Stock Handler: báo cáo kệ trống và quản lý nhiệm vụ tiếp hàng.</summary>
public interface IStaffService
{
    /// <summary>
    /// Robot hoặc khách báo cáo kệ trống/bị che khuất.
    /// Backend kiểm tra tồn kho tổng và gửi thông báo SignalR/MQTT đến nhân viên.
    /// </summary>
    Task<ReportOosResponseDto> ReportOutOfStockAsync(ReportOosRequestDto request, CancellationToken ct = default);

    /// <summary>Lấy danh sách nhiệm vụ bổ sung hàng đang chờ xử lý (Staff App).</summary>
    Task<RestockTaskListResponseDto> GetRestockTasksAsync(CancellationToken ct = default);

    /// <summary>Nhân viên xác nhận đã bổ sung hàng xong — cập nhật Slot.Quantity.</summary>
    Task<int> CompleteRestockAsync(CompleteRestockRequestDto request, CancellationToken ct = default);
}
