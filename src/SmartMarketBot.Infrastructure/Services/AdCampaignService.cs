using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Ads;
using SmartMarketBot.Domain.Entities;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

public sealed class AdCampaignService(
    AppDbContext db,
    ILocalizationService localizer,
    ILogger<AdCampaignService> logger) : IAdCampaignService
{
    private static readonly Lock WalletLock = new();
    private const int PeakHourStart1 = 11;
    private const int PeakHourEnd1 = 13;
    private const int PeakHourStart2 = 17;
    private const int PeakHourEnd2 = 20;
    private const decimal PeakHourMultiplier = 1.5m;
    private const int FraudDetectionWindowSeconds = 30;
    private const int MaxClicksPerWindow = 3;

    public async Task<PaginatedResponse<CampaignResponseDto>> GetListAsync(CampaignListRequestDto request, CancellationToken cancellationToken = default)
    {
        var query = db.AdCampaigns
            .AsNoTracking()
            .Include(c => c.Package)
            .Include(c => c.Brand)
            .Include(c => c.SponsoredProducts)
            .Include(c => c.AdCampaignLogs)
            .AsQueryable();

        if (!string.IsNullOrEmpty(request.Status))
            query = query.Where(c => c.Status == request.Status);

        if (request.BrandId.HasValue)
            query = query.Where(c => c.BrandId == request.BrandId.Value);

        if (request.FromDate.HasValue)
            query = query.Where(c => c.StartDate >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(c => c.EndDate <= request.ToDate.Value);

        if (!string.IsNullOrEmpty(request.SearchTerm))
            query = query.Where(c => c.CampaignName.Contains(request.SearchTerm));

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        var campaigns = await query
            .OrderByDescending(c => c.AdCampaignId)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = campaigns.Select(MapCampaignToDto).ToList();

        return new PaginatedResponse<CampaignResponseDto>(
            items,
            totalCount,
            request.PageNumber,
            request.PageSize,
            totalPages
        );
    }

    public async Task<CampaignResponseDto?> GetByIdAsync(int campaignId, CancellationToken cancellationToken = default)
    {
        var campaign = await db.AdCampaigns
            .AsNoTracking()
            .Include(c => c.Package)
            .Include(c => c.Brand)
            .Include(c => c.SponsoredProducts)
            .Include(c => c.AdCampaignLogs)
            .FirstOrDefaultAsync(c => c.AdCampaignId == campaignId, cancellationToken);

        return campaign is null ? null : MapCampaignToDto(campaign);
    }

    public async Task<CampaignResponseDto> CreateAsync(CreateCampaignRequestDto request, CancellationToken cancellationToken = default)
    {
        var package = await db.AdPackages.FindAsync([request.PackageId], cancellationToken)
            ?? throw new KeyNotFoundException(localizer.Get("AdPackageNotFound", request.PackageId));

        var brand = await db.Brands.FindAsync([request.BrandId], cancellationToken)
            ?? throw new KeyNotFoundException(localizer.Get("BrandNotFound", request.BrandId));

        if (request.RobotZoneId.HasValue)
        {
            var zoneExists = await db.RobotZones.AnyAsync(z => z.RobotZoneId == request.RobotZoneId.Value, cancellationToken);
            if (!zoneExists)
                throw new KeyNotFoundException(localizer.Get("RobotZoneNotFound", request.RobotZoneId.Value));
        }

        if (request.EndDate <= request.StartDate)
            throw new ArgumentException(localizer.Get("EndDateMustBeAfterStartDate"));

        var campaign = new AdCampaign
        {
            PackageId = request.PackageId,
            BrandId = request.BrandId,
            RobotZoneId = request.RobotZoneId,
            CampaignName = request.CampaignName,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = CampaignStatus.Inactive
        };

        db.AdCampaigns.Add(campaign);
        await db.SaveChangesAsync(cancellationToken);

        var result = await db.AdCampaigns
            .AsNoTracking()
            .Include(c => c.Package)
            .Include(c => c.Brand)
            .Include(c => c.SponsoredProducts)
            .FirstAsync(c => c.AdCampaignId == campaign.AdCampaignId, cancellationToken);

        return MapCampaignToDto(result);
    }

    public async Task<CampaignResponseDto> UpdateAsync(int campaignId, UpdateCampaignRequestDto request, CancellationToken cancellationToken = default)
    {
        var campaign = await db.AdCampaigns
            .Include(c => c.Package)
            .Include(c => c.Brand)
            .Include(c => c.SponsoredProducts)
            .Include(c => c.AdCampaignLogs)
            .FirstOrDefaultAsync(c => c.AdCampaignId == campaignId, cancellationToken)
            ?? throw new KeyNotFoundException(localizer.Get("CampaignNotFound", campaignId));

        if (campaign.Status != CampaignStatus.Inactive)
            throw new InvalidOperationException(localizer.Get("CampaignNotEditable"));

        if (request.EndDate <= request.StartDate)
            throw new ArgumentException(localizer.Get("EndDateMustBeAfterStartDate"));

        campaign.CampaignName = request.CampaignName;
        campaign.StartDate = request.StartDate;
        campaign.EndDate = request.EndDate;
        campaign.RobotZoneId = request.RobotZoneId;

        await db.SaveChangesAsync(cancellationToken);

        return MapCampaignToDto(campaign);
    }

    public async Task<bool> DeleteAsync(int campaignId, CancellationToken cancellationToken = default)
    {
        var campaign = await db.AdCampaigns.FindAsync([campaignId], cancellationToken)
            ?? throw new KeyNotFoundException(localizer.Get("CampaignNotFound", campaignId));

        if (campaign.Status == CampaignStatus.Active)
            throw new InvalidOperationException(localizer.Get("CannotDeleteActiveCampaign"));

        db.AdCampaigns.Remove(campaign);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ActivateCampaignResponseDto> ActivateAsync(int campaignId, CancellationToken cancellationToken = default)
    {
        var campaign = await db.AdCampaigns
            .Include(c => c.Package)
            .Include(c => c.Brand)
            .FirstOrDefaultAsync(c => c.AdCampaignId == campaignId, cancellationToken)
            ?? throw new KeyNotFoundException(localizer.Get("CampaignNotFound", campaignId));

        if (campaign.Status != CampaignStatus.Inactive)
            throw new InvalidOperationException(localizer.Get("CampaignNotInactive"));

        if (campaign.Package is null)
            throw new InvalidOperationException(localizer.Get("CampaignNoPackage"));

        var totalCost = campaign.Package.PricePackage + campaign.Package.PriceRoute;

        var isInMemory = db.Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) == true;

        if (isInMemory)
        {
            // InMemory provider does not support transactions.
            // The WalletLock (in-process) provides sufficient atomicity.
            lock (WalletLock)
            {
                if (campaign.Brand!.Wallet < totalCost)
                    throw new InvalidOperationException(localizer.Get("InsufficientWalletBalance", totalCost, campaign.Brand.Wallet));

                campaign.Brand.Wallet -= totalCost;
            }

            campaign.Status = CampaignStatus.Active;
            campaign.StartDate = DateTime.UtcNow;

            db.AdCampaignLogs.Add(new AdCampaignLog
            {
                AdCampaignId = campaign.AdCampaignId,
                ActionType = "Activation",
                ChargedAmount = totalCost,
                Timestamp = DateTime.UtcNow
            });

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Campaign {CampaignId} activated (InMemory). Charged {Amount}. Remaining: {Balance}",
                campaignId, totalCost, campaign.Brand.Wallet);

            return new ActivateCampaignResponseDto(
                campaign.AdCampaignId,
                campaign.CampaignName,
                CampaignStatus.Inactive,
                CampaignStatus.Active,
                totalCost,
                campaign.Brand.Wallet
            );
        }

        // Real DB with transaction support
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = null;
        try
        {
            transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            bool walletDeducted;
            lock (WalletLock)
            {
                if (campaign.Brand!.Wallet < totalCost)
                    throw new InvalidOperationException(localizer.Get("InsufficientWalletBalance", totalCost, campaign.Brand.Wallet));

                campaign.Brand.Wallet -= totalCost;
                walletDeducted = true;
            }

            if (!walletDeducted)
            {
                throw new InvalidOperationException(localizer.Get("WalletDeductionFailed"));
            }

            campaign.Status = CampaignStatus.Active;
            campaign.StartDate = DateTime.UtcNow;

            db.AdCampaignLogs.Add(new AdCampaignLog
            {
                AdCampaignId = campaign.AdCampaignId,
                ActionType = "Activation",
                ChargedAmount = totalCost,
                Timestamp = DateTime.UtcNow
            });

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation("Campaign {CampaignId} activated. Charged {Amount} from Brand {BrandId} wallet. Remaining: {Balance}",
                campaignId, totalCost, campaign.BrandId, campaign.Brand.Wallet);

            return new ActivateCampaignResponseDto(
                campaign.AdCampaignId,
                campaign.CampaignName,
                CampaignStatus.Inactive,
                CampaignStatus.Active,
                totalCost,
                campaign.Brand.Wallet
            );
        }
        catch
        {
            if (transaction != null)
                await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<PauseCampaignResponseDto> PauseAsync(int campaignId, string reason, CancellationToken cancellationToken = default)
    {
        var campaign = await db.AdCampaigns
            .Include(c => c.Brand)
            .FirstOrDefaultAsync(c => c.AdCampaignId == campaignId, cancellationToken)
            ?? throw new KeyNotFoundException(localizer.Get("CampaignNotFound", campaignId));

        if (campaign.Status != CampaignStatus.Active)
            throw new InvalidOperationException(localizer.Get("CampaignNotActive"));

        campaign.Status = CampaignStatus.Paused;

        var log = new AdCampaignLog
        {
            AdCampaignId = campaign.AdCampaignId,
            ActionType = "Paused",
            ChargedAmount = 0,
            Timestamp = DateTime.UtcNow
        };
        db.AdCampaignLogs.Add(log);

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Campaign {CampaignId} paused. Reason: {Reason}", campaignId, reason);

        return new PauseCampaignResponseDto(
            campaign.AdCampaignId,
            campaign.CampaignName,
            reason,
            CampaignStatus.Paused
        );
    }

    public async Task<CancelCampaignResponseDto> CancelAsync(int campaignId, CancellationToken cancellationToken = default)
    {
        var campaign = await db.AdCampaigns
            .Include(c => c.Brand)
            .FirstOrDefaultAsync(c => c.AdCampaignId == campaignId, cancellationToken)
            ?? throw new KeyNotFoundException(localizer.Get("CampaignNotFound", campaignId));

        if (campaign.Status == CampaignStatus.Completed || campaign.Status == CampaignStatus.Canceled)
            throw new InvalidOperationException(localizer.Get("CampaignAlreadyTerminated"));

        decimal refundedAmount = 0;

        if (campaign.Status == CampaignStatus.Active)
        {
            var logs = await db.AdCampaignLogs
                .Where(l => l.AdCampaignId == campaignId && l.ActionType == "Activation" && l.ChargedAmount > 0)
                .ToListAsync(cancellationToken);

            refundedAmount = logs.Sum(l => l.ChargedAmount);

            if (refundedAmount > 0)
            {
                lock (WalletLock)
                {
                    campaign.Brand!.Wallet += refundedAmount;
                }
            }
        }

        campaign.Status = CampaignStatus.Canceled;

        var log = new AdCampaignLog
        {
            AdCampaignId = campaign.AdCampaignId,
            ActionType = "Canceled",
            ChargedAmount = -refundedAmount,
            Timestamp = DateTime.UtcNow
        };
        db.AdCampaignLogs.Add(log);

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Campaign {CampaignId} canceled. Refunded: {RefundedAmount}", campaignId, refundedAmount);

        return new CancelCampaignResponseDto(
            campaign.AdCampaignId,
            campaign.CampaignName,
            CampaignStatus.Canceled,
            refundedAmount
        );
    }

    public async Task ProcessExpiredCampaignsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var expiredCampaigns = await db.AdCampaigns
            .Where(c => c.Status == CampaignStatus.Active || c.Status == CampaignStatus.Paused)
            .Where(c => c.EndDate < now)
            .ToListAsync(cancellationToken);

        foreach (var campaign in expiredCampaigns)
        {
            campaign.Status = CampaignStatus.Completed;

            var log = new AdCampaignLog
            {
                AdCampaignId = campaign.AdCampaignId,
                ActionType = "Completed",
                ChargedAmount = 0,
                Timestamp = DateTime.UtcNow
            };
            db.AdCampaignLogs.Add(log);

            logger.LogInformation("Campaign {CampaignId} auto-completed due to EndDate expiry", campaign.AdCampaignId);
        }

        if (expiredCampaigns.Any())
            await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ProcessWalletLowBalanceAsync(int brandId, CancellationToken cancellationToken = default)
    {
        var brand = await db.Brands.FindAsync([brandId], cancellationToken);
        if (brand is null || brand.Wallet > 0)
            return;

        var activeCampaigns = await db.AdCampaigns
            .Where(c => c.BrandId == brandId && c.Status == CampaignStatus.Active)
            .ToListAsync(cancellationToken);

        foreach (var campaign in activeCampaigns)
        {
            await PauseAsync(campaign.AdCampaignId, "Insufficient wallet balance", cancellationToken);
        }
    }

    public async Task ProcessOutOfStockAsync(int campaignId, CancellationToken cancellationToken = default)
    {
        var campaign = await db.AdCampaigns
            .Include(c => c.SponsoredProducts)
            .ThenInclude(sp => sp.Product!)
            .ThenInclude(p => p.ProductSlots)
            .ThenInclude(ps => ps.Slot)
            .FirstOrDefaultAsync(c => c.AdCampaignId == campaignId, cancellationToken);

        if (campaign is null || campaign.Status != CampaignStatus.Active)
            return;

        var allOutOfStock = campaign.SponsoredProducts.All(sp =>
        {
            if (sp.Product?.ProductSlots == null || !sp.Product.ProductSlots.Any())
                return true;
            return sp.Product.ProductSlots.All(ps => ps.Slot.Quantity <= 0);
        });

        if (allOutOfStock)
        {
            await PauseAsync(campaignId, "All sponsored products are out of stock", cancellationToken);
        }
    }

    public async Task<RobotPlaylistResponseDto> GetRobotPlaylistAsync(int robotId, CancellationToken cancellationToken = default)
    {
        var robot = await db.Robots
            .Include(r => r.RobotZones)
            .FirstOrDefaultAsync(r => r.RobotId == robotId, cancellationToken)
            ?? throw new KeyNotFoundException(localizer.Get("RobotNotFound", robotId));

        var currentZone = robot.RobotZones.FirstOrDefault();
        var currentZoneId = currentZone?.ZoneId;

        var productSlotQuery = db.ProductSlots
            .AsNoTracking()
            .Include(ps => ps.Product!)
            .ThenInclude(p => p.SponsoredProducts)
            .ThenInclude(sp => sp.AdCampaign)
            .ThenInclude(c => c!.Package)
            .Include(ps => ps.Slot!)
            .ThenInclude(s => s.Shelf!)
            .ThenInclude(sh => sh.Aisle!)
            .Where(ps => ps.Slot.Quantity > 0);

        if (currentZoneId.HasValue)
        {
            productSlotQuery = productSlotQuery
                .Where(ps => ps.Slot.Shelf.Aisle.ZoneId == currentZoneId.Value);
        }

        var productSlots = await productSlotQuery.ToListAsync(cancellationToken);

        var activeCampaignProducts = productSlots
            .SelectMany(ps => ps.Product!.SponsoredProducts)
            .Where(sp => sp.AdCampaign?.Status == CampaignStatus.Active)
            .Where(sp => sp.Status == SponsoredProductStatus.Active)
            .ToList();

        var playlist = activeCampaignProducts
            .Select(sp => new RobotPlaylistItemDto(
                sp.SponsoredId,
                sp.AdCampaignId,
                sp.AdCampaign!.CampaignName,
                sp.ProductId,
                sp.Product!.ProductName,
                sp.Product.UnitPrice,
                sp.Priority,
                sp.AdCampaign.Package?.AdScore ?? 0,
                sp.AdCampaign.EndDate,
                sp.Product.ImageUrl ?? string.Empty,
                "image",
                30
            ))
            .OrderByDescending(item => item.AdScore)
            .ThenByDescending(item => item.Priority)
            .ThenBy(item => item.EndDate)
            .ToList();

        return new RobotPlaylistResponseDto(
            robotId,
            currentZoneId,
            playlist,
            DateTime.UtcNow
        );
    }

    public async Task<LogInteractionResponseDto> LogInteractionAsync(LogInteractionRequestDto request, CancellationToken cancellationToken = default)
    {
        var campaign = await db.AdCampaigns
            .Include(c => c.Package)
            .Include(c => c.Brand)
            .FirstOrDefaultAsync(c => c.AdCampaignId == request.AdCampaignId, cancellationToken)
            ?? throw new KeyNotFoundException(localizer.Get("CampaignNotFound", request.AdCampaignId));

        if (campaign.Status != CampaignStatus.Active)
        {
            return new LogInteractionResponseDto(
                true,
                0,
                0,
                false,
                null,
                localizer.Get("CampaignNotActive")
            );
        }

        bool isFraud = false;
        string? fraudReason = null;
        decimal chargedAmount = 0;

        if (request.ActionType == AdActionType.Click || request.ActionType == AdActionType.Navigation)
        {
            var interactionResult = await CheckAndDetectFraudAsync(request, cancellationToken);
            isFraud = interactionResult.IsFraud;
            fraudReason = interactionResult.FraudReason;

            if (!isFraud)
            {
                var now = DateTime.UtcNow;
                var currentHour = now.Hour;
                var isPeakHour = (currentHour >= PeakHourStart1 && currentHour < PeakHourEnd1) ||
                                 (currentHour >= PeakHourStart2 && currentHour < PeakHourEnd2);

                var basePrice = campaign.Package?.BasePriceClick ?? 0;
                chargedAmount = isPeakHour ? basePrice * PeakHourMultiplier : basePrice;

                lock (WalletLock)
                {
                    if (campaign.Brand!.Wallet >= chargedAmount)
                    {
                        campaign.Brand.Wallet -= chargedAmount;
                    }
                    else
                    {
                        chargedAmount = campaign.Brand.Wallet;
                        campaign.Brand.Wallet = 0;
                    }
                }

                if (campaign.Brand!.Wallet <= 0)
                {
                    _ = Task.Run(() => ProcessWalletLowBalanceAsync(campaign.BrandId, CancellationToken.None));
                }
            }
        }

        var logEntry = new AdCampaignLog
        {
            AdCampaignId = request.AdCampaignId,
            ActionType = isFraud ? AdActionType.FraudDetected : request.ActionType,
            ChargedAmount = chargedAmount,
            Timestamp = DateTime.UtcNow,
            SponsoredId = request.SponsoredId,
            ProductId = request.ProductId,
            RobotId = request.RobotId,
            RobotZoneId = request.RobotZoneId,
            ZoneId = request.ZoneId,
            SlotId = request.SlotId,
            MemberId = request.MemberId,
            XCoord = request.XCoord,
            YCoord = request.YCoord
        };

        logEntry.SessionId = request.SessionId;
        db.AdCampaignLogs.Add(logEntry);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Interaction logged: Campaign={CampaignId}, Action={Action}, Charged={Charged}, Fraud={IsFraud}",
            request.AdCampaignId, request.ActionType, chargedAmount, isFraud);

        return new LogInteractionResponseDto(
            true,
            logEntry.LogId,
            chargedAmount,
            isFraud,
            fraudReason,
            isFraud ? localizer.Get("FraudDetected") : localizer.Get("InteractionLogged")
        );
    }

    public async Task<PaginatedResponse<AdCampaignLogDto>> GetCampaignLogsAsync(int campaignId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var campaign = await db.AdCampaigns.AnyAsync(c => c.AdCampaignId == campaignId, cancellationToken);
        if (!campaign)
            throw new KeyNotFoundException(localizer.Get("CampaignNotFound", campaignId));

        var query = db.AdCampaignLogs
            .AsNoTracking()
            .Include(l => l.AdCampaign)
            .Include(l => l.Product)
            .Where(l => l.AdCampaignId == campaignId);

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var logs = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = logs.Select(l => new AdCampaignLogDto(
            l.LogId,
            l.AdCampaignId,
            l.AdCampaign?.CampaignName,
            l.ActionType,
            l.ChargedAmount,
            l.Timestamp,
            l.SponsoredId,
            l.ProductId,
            l.Product?.ProductName,
            l.RobotId,
            l.ZoneId,
            l.MemberId,
            l.ActionType == AdActionType.FraudDetected
        )).ToList();

        return new PaginatedResponse<AdCampaignLogDto>(
            items,
            totalCount,
            pageNumber,
            pageSize,
            totalPages
        );
    }

    private async Task<(bool IsFraud, string? FraudReason)> CheckAndDetectFraudAsync(
        LogInteractionRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.SessionId) && !request.MemberId.HasValue)
            return (false, null);

        var windowStart = DateTime.UtcNow.AddSeconds(-FraudDetectionWindowSeconds);

        // InMemory-compatible: count all qualifying logs, then compare.
        // GroupBy + Select(g => g.Count()) is not supported by InMemory provider.
        IQueryable<AdCampaignLog> query = db.AdCampaignLogs
            .Where(l => l.AdCampaignId == request.AdCampaignId)
            .Where(l => l.ActionType == AdActionType.Click)
            .Where(l => l.Timestamp >= windowStart);

        if (!string.IsNullOrEmpty(request.SessionId))
            query = query.Where(l => l.SessionId == request.SessionId);
        else if (request.MemberId.HasValue)
            query = query.Where(l => l.MemberId == request.MemberId);

        var recentClicks = await query.CountAsync(cancellationToken);

        if (recentClicks >= MaxClicksPerWindow)
        {
            logger.LogWarning("Fraud detected: Session/Member exceeded max clicks. CampaignId={CampaignId}, SessionId={SessionId}, MemberId={MemberId}",
                request.AdCampaignId, request.SessionId, request.MemberId);

            return (true, localizer.Get("FraudExcessiveClicks", MaxClicksPerWindow, FraudDetectionWindowSeconds));
        }

        return (false, null);
    }

    private static CampaignResponseDto MapCampaignToDto(AdCampaign c)
    {
        return new CampaignResponseDto(
            c.AdCampaignId,
            c.CampaignName,
            c.PackageId,
            c.Package?.PackageName ?? string.Empty,
            c.BrandId,
            c.Brand?.BrandName ?? string.Empty,
            c.RobotZoneId,
            c.StartDate,
            c.EndDate,
            c.Status,
            c.SponsoredProducts?.Count ?? 0,
            c.AdCampaignLogs?.Sum(l => l.ChargedAmount) ?? 0
        );
    }
}
