using Microsoft.AspNetCore.Http;
using SmartMarketBot.Application.Interfaces;

namespace SmartMarketBot.Infrastructure.Services;

public sealed class LocalizationService : ILocalizationService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly Dictionary<string, Dictionary<string, string>> _translations;

    public LocalizationService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;

        _translations = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["vi"] = new Dictionary<string, string>
            {
                // Auth
                ["EmailInUse"] = "Email đã được sử dụng.",
                ["OtpResendCooldown"] = "Vui lòng chờ {0} giây trước khi yêu cầu OTP mới.",
                ["RegistrationOtpSent"] = "Mã OTP đã được gửi về email của bạn.",
                ["OtpInvalid"] = "Mã OTP không chính xác.",
                ["OtpUsed"] = "Mã OTP đã được sử dụng.",
                ["OtpExpired"] = "Mã OTP đã hết hạn.",
                ["OtpRequestNotFound"] = "Không tìm thấy yêu cầu OTP. Vui lòng bắt đầu lại.",
                ["OtpResent"] = "OTP mới đã được gửi.",
                ["LoginInvalid"] = "Email hoặc mật khẩu không đúng.",
                ["AccountLocked"] = "Tài khoản đã bị khóa.",
                ["EmailNotConfirmed"] = "Email chưa được xác minh. Vui lòng kiểm tra hộp thư.",
                ["RefreshTokenInvalid"] = "Refresh token không hợp lệ hoặc đã hết hạn.",
                ["LogoutSuccess"] = "Đăng xuất thành công.",
                ["ForgotPasswordOtpSent"] = "Nếu email tồn tại, mã OTP đã được gửi.",
                ["AccountNotFound"] = "Không tìm thấy tài khoản.",
                ["PasswordResetSuccess"] = "Mật khẩu đã được đặt lại thành công.",

                // Member
                ["MemberNotFound"] = "Không tìm thấy hội viên {0}.",
                ["BudgetSet"] = "Đã cài ngân sách {0:N0}₫ cho phiên mua sắm.",
                ["ProductNotFound"] = "Không tìm thấy sản phẩm với barcode {0}.",
                ["AllergyAlert"] = "⚠️ CẢNH BÁO DỊ ỨNG: {0} chứa thành phần dị ứng của bạn!",
                ["BudgetExceededAlert"] = "💰 Thêm {0} ({1:N0}₫) sẽ vượt ngân sách {2:N0}₫ của bạn!",
                ["DuplicatePurchaseAlert"] = "🔄 Bạn đã mua {0} trong 7 ngày gần đây.",
                ["AltReasonAllergy"] = "Không chứa thành phần dị ứng",
                ["AltReasonBudget"] = "Giá phù hợp hơn với ngân sách",
                ["BdayDeal"] = "🎂 Quà sinh nhật",
                ["AnniversaryDeal"] = "🎉 Kỷ niệm hội viên",
                ["EventDealReason"] = "{0:N0}% giảm giá cho toàn bộ đơn hàng dịp {1}",

                // Staff & OOS
                ["SlotNotFound"] = "Không tìm thấy vị trí ô kệ {0}.",
                ["OosEventWithStock"] = "Ghi nhận sự cố hết hàng (ScanID={0}). Đã gửi thông báo cho nhân viên.",
                ["OosEventNoStock"] = "Ghi nhận sự cố hết hàng (ScanID={0}). Kho tổng hết hàng - đề xuất sản phẩm thay thế.",

                // Admin
                ["BrandNotFound"] = "Không tìm thấy Brand {0}.",
                ["BrandHasActiveCampaign"] = "Brand đang có chiến dịch quảng cáo, không thể xóa.",
                ["AdPackageNotFound"] = "Không tìm thấy AdPackage {0}.",
                ["AdPackageInUse"] = "AdPackage đang được sử dụng bởi chiến dịch quảng cáo, không thể xóa.",
                ["AdCampaignNotFound"] = "Không tìm thấy AdCampaign {0}.",
                ["CampaignDateInvalid"] = "Ngày kết thúc phải sau ngày bắt đầu.",
                ["SponsoredProductNotFound"] = "Không tìm thấy SponsoredProduct {0}.",
                ["SponsoredMappingExists"] = "Mapping AdCampaign ↔ Product đã tồn tại.",
                ["ProductNotFoundById"] = "Không tìm thấy sản phẩm {0}.",

                // Phase B - Ads Sponsored Recommendations
                ["AdImpressionRecorded"] = "Đã ghi nhận {0} impression cho AdCampaign {1} (Route-based).",
                ["AdNoActiveCampaign"] = "Hiện không có chiến dịch quảng cáo nào đang chạy tại Zone {0}.",
                ["AdSlotNotFound"] = "Không tìm thấy Slot {0} khi ghi nhận impression.",
                ["AdZoneNotFound"] = "Không tìm thấy Zone {0} cho Slot {1}.",
                ["AdNoRoutePricing"] = "AdPackage {0} chưa cấu hình giá Route (PriceRoute=0).",
                ["AdRobotNotFoundByCode"] = "Không tìm thấy robot với mã '{0}'.",
                ["AdRobotNoActiveRoute"] = "Robot '{0}' chưa được gán route Active. Không phát quảng cáo.",
                ["AdNoContext"] = "Robot '{0}' không có context nào (route/zone/shelf). Bỏ qua.",

                // AdCampaign (existing)
                ["CampaignNotFound"] = "Không tìm thấy AdCampaign {0}.",
                ["CampaignNotInactive"] = "Chiến dịch không ở trạng thái Inactive/Paused.",
                ["CampaignNoPackage"] = "Chiến dịch chưa gán AdPackage.",
                ["CampaignNoRouteAssigned"] = "Chiến dịch chưa gán RobotRoute nào. Phải mua ít nhất 1 route để kích hoạt.",
                ["CampaignNoTargeting"] = "Chiến dịch chưa chọn targeting nào (Route / Zone / Shelf). Phải mua ít nhất 1 loại để kích hoạt.",
                ["CampaignNotEditable"] = "Chỉ chỉnh sửa được chiến dịch ở trạng thái Inactive.",
                ["CampaignNotActive"] = "Chiến dịch không ở trạng thái Active.",
                ["CampaignAlreadyActive"] = "Chiến dịch đã Active. Hãy Pause trước khi gán thêm route.",
                ["CampaignAlreadyTerminated"] = "Chiến dịch đã kết thúc (Completed/Canceled).",
                ["CannotDeleteActiveCampaign"] = "Không thể xóa chiến dịch đang Active.",
                ["EndDateMustBeAfterStartDate"] = "Ngày kết thúc phải sau ngày bắt đầu.",
                ["ProductIdsRequired"] = "Phải có ít nhất 1 ProductId.",
                ["ProductsNotFound"] = "Không tìm thấy sản phẩm: {0}.",
                ["InsufficientWalletBalance"] = "Số dư ví không đủ. Cần {0:N0}₫, hiện có {1:N0}₫.",
                ["RobotRouteNotFound"] = "Không tìm thấy RobotRoute: {0}.",
                ["FraudDetected"] = "Phát hiện hành vi gian lận, ghi nhận bị từ chối.",
                ["InteractionLogged"] = "Đã ghi nhận tương tác.",
                ["FraudExcessiveClicks"] = "Phát hiện {0} lượt click trong {1}s — vượt ngưỡng cho phép.",
                ["SessionBindNoMatch"] = "Không tìm thấy session {0} trong cửa sổ 30s gần nhất.",
                ["SessionBindSuccess"] = "Đã gán {0} log sang Member.",

                // Realtime skeleton
                ["StaffAlertMissingContext"] = "Staff alert thiếu context (SlotId hoặc ZoneId).",
                ["MemberUpdateMissingId"] = "Member update thiếu MemberId.",
                ["HealthDegraded"] = "Hệ thống đang suy giảm (DB mất kết nối).",

                // Navigation & Robot
                ["StartEndNodeNotExist"] = "Node bắt đầu hoặc kết thúc không tồn tại trên sơ đồ.",
                ["StartEndNodeBlocked"] = "Node bắt đầu hoặc kết thúc đã bị chặn.",
                ["RouteNotFound"] = "Không tìm thấy đường đi từ node {0} đến node {1}.",
                ["NodeNotFound"] = "Không tìm thấy node {0}.",
                ["DestNodeInvalid"] = "DestinationNodeId phải là số nguyên hợp lệ.",
                ["RobotNotFound"] = "Không tìm thấy robot '{0}'.",
                ["AllergenRegistered"] = "Sản phẩm chứa thành phần dị ứng đã đăng ký.",

                // Map Management
                ["MapSyncSuccess"] = "Đã đồng bộ bản đồ thành công.",
                ["MapNotFound"] = "Không tìm thấy bản đồ {0}.",
                ["MapNotFoundForFloor"] = "Không tìm thấy bản đồ cho tầng {0}.",
                ["FloorplanImageUploaded"] = "Đã tải ảnh mặt bằng thành công.",
                ["FloorIdRequired"] = "FloorId là bắt buộc.",
                ["MapSyncEmptyPayload"] = "Dữ liệu sync trống. Cần có Nodes, Edges hoặc SemanticObjects.",
                ["FileRequired"] = "File là bắt buộc.",
                ["ImageOnlyAllowed"] = "Chỉ chấp nhận file ảnh JPG/PNG.",
                ["SemanticObjectNotFound"] = "Không tìm thấy SemanticObject {0}.",

                ["UnexpectedError"] = "Đã xảy ra lỗi không mong muốn."
            },
            ["en"] = new Dictionary<string, string>
            {
                // Auth
                ["EmailInUse"] = "Email is already in use.",
                ["OtpResendCooldown"] = "Please wait {0} seconds before requesting a new OTP.",
                ["RegistrationOtpSent"] = "OTP code has been sent to your email.",
                ["OtpInvalid"] = "Invalid OTP code.",
                ["OtpUsed"] = "OTP code has already been used.",
                ["OtpExpired"] = "OTP code has expired.",
                ["OtpRequestNotFound"] = "No OTP request found. Please start over.",
                ["OtpResent"] = "New OTP has been sent.",
                ["LoginInvalid"] = "Incorrect email or password.",
                ["AccountLocked"] = "Account has been locked.",
                ["EmailNotConfirmed"] = "Email is not verified. Please check your inbox.",
                ["RefreshTokenInvalid"] = "Invalid or expired refresh token.",
                ["LogoutSuccess"] = "Logged out successfully.",
                ["ForgotPasswordOtpSent"] = "If the email exists, an OTP has been sent.",
                ["AccountNotFound"] = "Account not found.",
                ["PasswordResetSuccess"] = "Password has been reset successfully.",

                // Member
                ["MemberNotFound"] = "Member {0} not found.",
                ["BudgetSet"] = "Shopping budget of {0:N0}₫ has been set.",
                ["ProductNotFound"] = "Product with barcode {0} not found.",
                ["AllergyAlert"] = "⚠️ ALLERGY WARNING: {0} contains your allergen ingredients!",
                ["BudgetExceededAlert"] = "💰 Adding {0} ({1:N0}₫) will exceed your budget of {2:N0}₫!",
                ["DuplicatePurchaseAlert"] = "🔄 You purchased {0} in the last 7 days.",
                ["AltReasonAllergy"] = "Does not contain allergen ingredients",
                ["AltReasonBudget"] = "Price fits your budget better",
                ["BdayDeal"] = "🎂 Birthday Gift",
                ["AnniversaryDeal"] = "🎉 Member Anniversary",
                ["EventDealReason"] = "{0:N0}% discount storewide for {1}",

                // Staff & OOS
                ["SlotNotFound"] = "Shelf slot {0} not found.",
                ["OosEventWithStock"] = "Out-of-stock event logged (ScanID={0}). Staff notification sent.",
                ["OosEventNoStock"] = "Out-of-stock event logged (ScanID={0}). No warehouse stock - product substitution recommended.",

                // Admin (English)
                ["BrandNotFound"] = "Brand {0} not found.",
                ["BrandHasActiveCampaign"] = "Brand has active ad campaigns and cannot be deleted.",
                ["AdPackageNotFound"] = "AdPackage {0} not found.",
                ["AdPackageInUse"] = "AdPackage is in use by an ad campaign and cannot be deleted.",
                ["AdCampaignNotFound"] = "AdCampaign {0} not found.",
                ["CampaignDateInvalid"] = "End date must be after start date.",
                ["SponsoredProductNotFound"] = "SponsoredProduct {0} not found.",
                ["SponsoredMappingExists"] = "AdCampaign ↔ Product mapping already exists.",
                ["ProductNotFoundById"] = "Product {0} not found.",

                // Phase B - Ads Sponsored Recommendations
                ["AdImpressionRecorded"] = "Logged {0} impression(s) for AdCampaign {1} (Route-based).",
                ["AdNoActiveCampaign"] = "No active ad campaigns in Zone {0} right now.",
                ["AdSlotNotFound"] = "Slot {0} not found while recording impression.",
                ["AdZoneNotFound"] = "Zone {0} not found for Slot {1}.",
                ["AdNoRoutePricing"] = "AdPackage {0} has no route pricing configured (PriceRoute=0).",
                ["AdRobotNotFoundByCode"] = "Robot with code '{0}' not found.",
                ["AdRobotNoActiveRoute"] = "Robot '{0}' has no Active route assigned. No ads will play.",
                ["AdNoContext"] = "Robot '{0}' has no context (route/zone/shelf). Skipped.",

                // AdCampaign (existing)
                ["CampaignNotFound"] = "AdCampaign {0} not found.",
                ["CampaignNotInactive"] = "Campaign is not in Inactive/Paused status.",
                ["CampaignNoPackage"] = "Campaign has no AdPackage assigned.",
                ["CampaignNoRouteAssigned"] = "Campaign has no RobotRoute assigned. Purchase at least 1 route to activate.",
                ["CampaignNoTargeting"] = "Campaign has no targeting assigned (Route / Zone / Shelf). Purchase at least 1 type to activate.",
                ["CampaignNotEditable"] = "Only Inactive campaigns can be edited.",
                ["CampaignNotActive"] = "Campaign is not in Active status.",
                ["CampaignAlreadyActive"] = "Campaign is already Active. Pause it before assigning more routes.",
                ["CampaignAlreadyTerminated"] = "Campaign has already ended (Completed/Canceled).",
                ["CannotDeleteActiveCampaign"] = "Cannot delete an Active campaign.",
                ["EndDateMustBeAfterStartDate"] = "End date must be after start date.",
                ["ProductIdsRequired"] = "At least 1 ProductId is required.",
                ["ProductsNotFound"] = "Products not found: {0}.",
                ["InsufficientWalletBalance"] = "Insufficient wallet balance. Need {0:N0}₫, currently {1:N0}₫.",
                ["RobotRouteNotFound"] = "RobotRoute not found: {0}.",
                ["FraudDetected"] = "Fraud detected — interaction was rejected.",
                ["InteractionLogged"] = "Interaction logged.",
                ["FraudExcessiveClicks"] = "Detected {0} clicks in {1}s — exceeds the allowed threshold.",
                ["SessionBindNoMatch"] = "Session {0} not found within the last 30s.",
                ["SessionBindSuccess"] = "Bound {0} log(s) to Member.",

                // Realtime skeleton
                ["StaffAlertMissingContext"] = "Staff alert missing context (SlotId or ZoneId).",
                ["MemberUpdateMissingId"] = "Member update missing MemberId.",
                ["HealthDegraded"] = "System degraded (DB unreachable).",

                // Navigation & Robot
                ["StartEndNodeNotExist"] = "Start or end node does not exist on map.",
                ["StartEndNodeBlocked"] = "Start or end node is blocked.",
                ["RouteNotFound"] = "No route found from node {0} to node {1}.",
                ["NodeNotFound"] = "Node {0} not found.",
                ["DestNodeInvalid"] = "DestinationNodeId must be a valid integer.",
                ["RobotNotFound"] = "Robot '{0}' not found.",
                ["AllergenRegistered"] = "Product contains a registered allergen ingredient.",

                // Map Management
                ["MapSyncSuccess"] = "Map synchronized successfully.",
                ["MapNotFound"] = "Map {0} not found.",
                ["MapNotFoundForFloor"] = "No map found for floor {0}.",
                ["FloorplanImageUploaded"] = "Floorplan image uploaded successfully.",
                ["FloorIdRequired"] = "FloorId is required.",
                ["MapSyncEmptyPayload"] = "Sync payload is empty. Nodes, Edges, or SemanticObjects required.",
                ["FileRequired"] = "File is required.",
                ["ImageOnlyAllowed"] = "Only JPG/PNG image files are allowed.",
                ["SemanticObjectNotFound"] = "SemanticObject {0} not found.",

                ["UnexpectedError"] = "An unexpected error occurred."
            }
        };
    }

    public string Get(string key)
    {
        var lang = GetCurrentLanguage();
        if (_translations.TryGetValue(lang, out var dict) && dict.TryGetValue(key, out var val))
        {
            return val;
        }
        if (_translations["vi"].TryGetValue(key, out var viVal))
        {
            return viVal;
        }
        return key;
    }

    public string Get(string key, params object[] args)
    {
        var template = Get(key);
        try
        {
            return string.Format(template, args);
        }
        catch
        {
            return template;
        }
    }

    private string GetCurrentLanguage()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null) return "vi";

        // 1. Check Query Parameter: ?lang=en
        if (context.Request.Query.TryGetValue("lang", out var langQuery))
        {
            var l = langQuery.ToString().ToLowerInvariant();
            if (l == "en" || l.StartsWith("en-")) return "en";
            if (l == "vi" || l.StartsWith("vi-")) return "vi";
        }

        // 2. Check Header: Accept-Language
        if (context.Request.Headers.TryGetValue("Accept-Language", out var langHeader))
        {
            var l = langHeader.ToString().ToLowerInvariant();
            if (l.Contains("en")) return "en";
            if (l.Contains("vi")) return "vi";
        }

        return "vi"; // default
    }
}
