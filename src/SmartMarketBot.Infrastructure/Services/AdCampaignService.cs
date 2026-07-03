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

    public async Task<PaginatedResponse<CampaignResponseDto>> GetListAsync(
        CampaignListRequestDto request,
        CancellationToken cancellationToken = default)
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

        return new PaginatedResponse<CampaignResponseDto>(items, totalCount, request.PageNumber, request.PageSize, totalPages);
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

    public async Task<CampaignResponseDto> CreateAsync(
        CreateCampaignRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var package = await db.AdPackages.FindAsync([request.PackageId], cancellationToken)
            ?? throw new KeyNotFoundException(localizer.Get("AdPackageNotFound", request.PackageId));

        var brand = await db.Brands.FindAsync([request.BrandId], cancellationToken)
            ?? throw new KeyNotFoundException(localizer.Get("BrandNotFound", request.BrandId));

        if (request.SemanticObjectId.HasValue)
        {
            var objExists = await db.SemanticObjects.AnyAsync(o => o.ObjectId == request.SemanticObjectId.Value, cancellationToken);
            if (!objExists)
                throw new KeyNotFoundException(localizer.Get("SemanticObjectNotFound", request.SemanticObjectId.Value));
        }

        if (request.EndDate <= request.StartDate)
            throw new ArgumentException(localizer.Get("EndDateMustBeAfterStartDate"));

        var campaign = new AdCampaign
        {
            PackageId = request.PackageId,
            BrandId = request.BrandId,
            SemanticObjectId = request.SemanticObjectId,
            CampaignName = request.CampaignName,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = CampaignStatus.Inactive
        };

        db.AdCampaigns.Add(campaign);
        await db.SaveChangesAsync(cancellationToken);

        if (request.ProductIds is { Count: > 0 })
        {
            await AddSponsoredProductsAsync(campaign.AdCampaignId, request.ProductIds, cancellationToken);
        }

        var result = await db.AdCampaigns
            .AsNoTracking()
            .Include(c => c.Package)
            .Include(c => c.Brand)
            .Include(c => c.SponsoredProducts)
            .FirstAsync(c => c.AdCampaignId == campaign.AdCampaignId, cancellationToken);

        return MapCampaignToDto(result);
    }

    public async Task<CampaignResponseDto> CreateWithProductsAsync(
        CreateCampaignWithProductsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var package = await db.AdPackages.FindAsync([request.PackageId], cancellationToken)
            ?? throw new KeyNotFoundException(localizer.Get("AdPackageNotFound", request.PackageId));

        var brand = await db.Brands.FindAsync([request.BrandId], cancellationToken)
            ?? throw new KeyNotFoundException(localizer.Get("BrandNotFound", request.BrandId));

        if (request.SemanticObjectId.HasValue)
        {
            var objExists = await db.SemanticObjects.AnyAsync(o => o.ObjectId == request.SemanticObjectId.Value, cancellationToken);
            if (!objExists)
                throw new KeyNotFoundException(localizer.Get("SemanticObjectNotFound", request.SemanticObjectId.Value));
        }

        if (request.EndDate <= request.StartDate)
            throw new ArgumentException(localizer.Get("EndDateMustBeAfterStartDate"));

        if (request.ProductIds.Count == 0)
            throw new ArgumentException(localizer.Get("ProductIdsRequired"));

        await ValidateProductsExistAsync(request.ProductIds, cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var campaign = new AdCampaign
            {
                PackageId = request.PackageId,
                BrandId = request.BrandId,
                SemanticObjectId = request.SemanticObjectId,
                CampaignName = request.CampaignName,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Status = CampaignStatus.Inactive
            };

            db.AdCampaigns.Add(campaign);
            await db.SaveChangesAsync(cancellationToken);

            var sponsoredProducts = request.ProductIds
                .Distinct()
                .Select(productId => new SponsoredProduct
                {
                    AdCampaignId = campaign.AdCampaignId,
                    ProductId = productId,
                    Priority = 0,
                    Status = SponsoredProductStatus.Active
                })
                .ToList();

            db.SponsoredProducts.AddRange(sponsoredProducts);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Campaign {CampaignId} created with {Count} sponsored products for Brand {BrandId}",
                campaign.AdCampaignId, sponsoredProducts.Count, campaign.BrandId);

            var result = await db.AdCampaigns
                .AsNoTracking()
                .Include(c => c.Package)
                .Include(c => c.Brand)
                .Include(c => c.SponsoredProducts)
                .FirstAsync(c => c.AdCampaignId == campaign.AdCampaignId, cancellationToken);

            return MapCampaignToDto(result);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<CampaignResponseDto> UpdateAsync(
        int campaignId,
        UpdateCampaignRequestDto request,
        CancellationToken cancellationToken = default)
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
        campaign.SemanticObjectId = request.SemanticObjectId;

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

        if (campaign.Status != CampaignStatus.Inactive && campaign.Status != CampaignStatus.Paused)
            throw new InvalidOperationException(localizer.Get("CampaignNotInactive"));

        var isResuming = campaign.Status == CampaignStatus.Paused;
        decimal chargedAmount = 0;

        if (!isResuming)
        {
            if (campaign.Package is null)
                throw new InvalidOperationException(localizer.Get("CampaignNoPackage"));

            var totalCost = campaign.Package.PricePackage + campaign.Package.PriceRoute;
            var isInMemory = db.Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) == true;

            if (isInMemory)
            {
                lock (WalletLock)
                {
                    if (campaign.Brand!.Wallet < totalCost)
                        throw new InvalidOperationException(localizer.Get("InsufficientWalletBalance", totalCost, campaign.Brand.Wallet));

                    campaign.Brand.Wallet -= totalCost;
                }

                campaign.StartDate = DateTime.UtcNow;

                db.AdCampaignLogs.Add(new AdCampaignLog
                {
                    AdCampaignId = campaign.AdCampaignId,
                    ActionType = "Activation",
                    ChargedAmount = totalCost,
                    Timestamp = DateTime.UtcNow
                });

                await db.SaveChangesAsync(cancellationToken);

                logger.LogInformation(
                    "Campaign {CampaignId} activated (InMemory). Charged {Amount}. Remaining: {Balance}",
                    campaignId, totalCost, campaign.Brand!.Wallet);

                campaign.Status = CampaignStatus.Active;
                return new ActivateCampaignResponseDto(
                    campaign.AdCampaignId, campaign.CampaignName,
                    CampaignStatus.Inactive, CampaignStatus.Active,
                    totalCost, campaign.Brand.Wallet);
            }

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                decimal newBalance;
                lock (WalletLock)
                {
                    if (campaign.Brand!.Wallet < totalCost)
                        throw new InvalidOperationException(localizer.Get("InsufficientWalletBalance", totalCost, campaign.Brand.Wallet));

                    campaign.Brand.Wallet -= totalCost;
                    newBalance = campaign.Brand.Wallet;
                }

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

                logger.LogInformation(
                    "Campaign {CampaignId} activated. Charged {Amount} from Brand {BrandId} wallet. Remaining: {Balance}",
                    campaignId, totalCost, campaign.BrandId, newBalance);

                campaign.Status = CampaignStatus.Active;
                return new ActivateCampaignResponseDto(
                    campaign.AdCampaignId, campaign.CampaignName,
                    CampaignStatus.Inactive, CampaignStatus.Active,
                    totalCost, newBalance);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        campaign.Status = CampaignStatus.Active;
        db.AdCampaignLogs.Add(new AdCampaignLog
        {
            AdCampaignId = campaign.AdCampaignId,
            ActionType = "Resumed",
            ChargedAmount = 0,
            Timestamp = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Campaign {CampaignId} resumed from Paused. No charge applied.", campaignId);

        return new ActivateCampaignResponseDto(
            campaign.AdCampaignId, campaign.CampaignName,
            CampaignStatus.Paused, CampaignStatus.Active,
            0, campaign.Brand!.Wallet);
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

        db.AdCampaignLogs.Add(new AdCampaignLog
        {
            AdCampaignId = campaign.AdCampaignId,
            ActionType = "Paused",
            ChargedAmount = 0,
            Timestamp = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Campaign {CampaignId} paused. Reason: {Reason}", campaignId, reason);

        return new PauseCampaignResponseDto(
            campaign.AdCampaignId, campaign.CampaignName, reason, CampaignStatus.Paused);
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

        db.AdCampaignLogs.Add(new AdCampaignLog
        {
            AdCampaignId = campaign.AdCampaignId,
            ActionType = "Canceled",
            ChargedAmount = -refundedAmount,
            Timestamp = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Campaign {CampaignId} canceled. Refunded: {RefundedAmount}", campaignId, refundedAmount);

        return new CancelCampaignResponseDto(
            campaign.AdCampaignId, campaign.CampaignName, CampaignStatus.Canceled, refundedAmount);
    }

    public async Task<SessionBindResponseDto> BindSessionAsync(
        SessionBindRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var memberExists = await db.Members.AnyAsync(m => m.MemberId == request.MemberId, cancellationToken);
        if (!memberExists)
            throw new KeyNotFoundException(localizer.Get("MemberNotFound", request.MemberId));

        var now = DateTime.UtcNow;
        var windowStart = now.AddSeconds(-FraudDetectionWindowSeconds);

        var activeSessionLogs = await db.AdCampaignLogs
            .Where(l => l.SessionId == request.SessionId
                        && l.Timestamp >= windowStart
                        && l.MemberId == null)
            .ToListAsync(cancellationToken);

        if (activeSessionLogs.Count == 0)
            return new SessionBindResponseDto(
                0, request.MemberId, request.SessionId,
                localizer.Get("SessionBindNoMatch", request.SessionId));

        foreach (var log in activeSessionLogs)
        {
            log.MemberId = request.MemberId;
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Session {SessionId} bound to Member {MemberId}. Updated {Count} logs.",
            request.SessionId, request.MemberId, activeSessionLogs.Count);

        return new SessionBindResponseDto(
            activeSessionLogs.Count, request.MemberId, request.SessionId,
            localizer.Get("SessionBindSuccess", activeSessionLogs.Count));
    }

    public async Task ProcessExpiredCampaignsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var expiredCampaigns = await db.AdCampaigns
            .Where(c => (c.Status == CampaignStatus.Active || c.Status == CampaignStatus.Paused)
                        && c.EndDate < now)
            .ToListAsync(cancellationToken);

        foreach (var campaign in expiredCampaigns)
        {
            campaign.Status = CampaignStatus.Completed;

            db.AdCampaignLogs.Add(new AdCampaignLog
            {
                AdCampaignId = campaign.AdCampaignId,
                ActionType = "Completed",
                ChargedAmount = 0,
                Timestamp = DateTime.UtcNow
            });

            logger.LogInformation("Campaign {CampaignId} auto-completed due to EndDate expiry", campaign.AdCampaignId);
        }

        if (expiredCampaigns.Count != 0)
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

    public async Task<RobotPlaylistResponseDto> GetRobotPlaylistAsync(int robotId, int? semanticObjectId, CancellationToken cancellationToken = default)
    {
        var robotExists = await db.Robots
            .AsNoTracking()
            .AnyAsync(r => r.RobotId == robotId, cancellationToken);
        if (!robotExists)
            throw new KeyNotFoundException(localizer.Get("RobotNotFound", robotId));

        if (!semanticObjectId.HasValue)
        {
            return new RobotPlaylistResponseDto(robotId, null, [], DateTime.UtcNow, null);
        }

        var semanticObject = await db.SemanticObjects
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ObjectId == semanticObjectId.Value, cancellationToken);

        if (semanticObject is null || !semanticObject.ProductTypeId.HasValue)
        {
            return new RobotPlaylistResponseDto(robotId, null, [], DateTime.UtcNow, semanticObjectId.Value);
        }

        var now = DateTime.UtcNow;
        var productTypeId = semanticObject.ProductTypeId.Value;

        var playlistItems = await (from sp in db.SponsoredProducts.AsNoTracking()
                                   where sp.Product.ProductTypeId == productTypeId
                                         && sp.Status == SponsoredProductStatus.Active
                                         && sp.AdCampaign.Status == CampaignStatus.Active
                                         && sp.AdCampaign.StartDate <= now
                                         && sp.AdCampaign.EndDate >= now
                                   orderby sp.AdCampaign.Package.AdScore descending, sp.Priority descending, sp.AdCampaign.EndDate
                                   select new RobotPlaylistItemDto
                                   {
                                       SponsoredId = sp.SponsoredId,
                                       AdCampaignId = sp.AdCampaignId,
                                       CampaignName = sp.AdCampaign.CampaignName,
                                       ProductId = sp.ProductId,
                                       ProductName = sp.Product.ProductName,
                                       ProductPrice = sp.Product.UnitPrice,
                                       Priority = sp.Priority,
                                       AdScore = sp.AdCampaign.Package.AdScore,
                                       EndDate = sp.AdCampaign.EndDate,
                                       ImageUrl = sp.Product.ImageUrl ?? string.Empty,
                                       DisplayDurationSeconds = 30,
                                       MediaContents = sp.AdCampaign.AdResources
                                           .Where(r => r.Status == AdResourceStatus.Active)
                                           .Select(r => new MediaContentDto(
                                               r.ResourceType,
                                               r.ResourceUrl,
                                               r.ContentText,
                                               r.Resolution))
                                           .ToList()
                                   })
                                   .ToListAsync(cancellationToken);

        return new RobotPlaylistResponseDto(robotId, null, playlistItems, DateTime.UtcNow, semanticObjectId.Value);
    }

    public async Task<LogInteractionResponseDto> LogInteractionAsync(
        LogInteractionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var campaign = await db.AdCampaigns
            .Include(c => c.Package)
            .Include(c => c.Brand)
            .FirstOrDefaultAsync(c => c.AdCampaignId == request.AdCampaignId, cancellationToken)
            ?? throw new KeyNotFoundException(localizer.Get("CampaignNotFound", request.AdCampaignId));

        if (campaign.Status != CampaignStatus.Active)
        {
            return new LogInteractionResponseDto(
                true, 0, 0, false, null, localizer.Get("CampaignNotActive"));
        }

        bool isFraud = false;
        string? fraudReason = null;
        decimal chargedAmount = 0;

        if (request.ActionType == AdActionType.Click || request.ActionType == AdActionType.Navigation)
        {
            var fraudResult = await CheckAndDetectFraudAsync(request, cancellationToken);
            isFraud = fraudResult.IsFraud;
            fraudReason = fraudResult.FraudReason;

            if (!isFraud)
            {
                var now = DateTime.UtcNow;
                var hour = now.Hour;
                var isPeakHour = (hour >= PeakHourStart1 && hour < PeakHourEnd1) ||
                                 (hour >= PeakHourStart2 && hour < PeakHourEnd2);

                var basePrice = campaign.Package?.BasePriceClick ?? 0;
                chargedAmount = isPeakHour
                    ? Math.Round(basePrice * PeakHourMultiplier, 2)
                    : basePrice;

                var isInMemory = db.Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) == true;

                if (isInMemory)
                {
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
                }
                else
                {
                    await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
                    try
                    {
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

                        await db.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                    }
                    catch
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        throw;
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
            SemanticObjectId = request.SemanticObjectId,
            ZoneId = request.ZoneId,
            SlotId = request.SlotId,
            MemberId = request.MemberId,
            SessionId = request.SessionId,
            XCoord = request.XCoord,
            YCoord = request.YCoord
        };

        db.AdCampaignLogs.Add(logEntry);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Interaction logged: Campaign={CampaignId}, Action={Action}, Charged={Charged}, Fraud={IsFraud}",
            request.AdCampaignId, request.ActionType, chargedAmount, isFraud);

        return new LogInteractionResponseDto(
            true, logEntry.LogId, chargedAmount, isFraud, fraudReason,
            isFraud ? localizer.Get("FraudDetected") : localizer.Get("InteractionLogged"));
    }

    public async Task<PaginatedResponse<AdCampaignLogDto>> GetCampaignLogsAsync(
        int campaignId, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var exists = await db.AdCampaigns.AnyAsync(c => c.AdCampaignId == campaignId, cancellationToken);
        if (!exists)
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

        var items = logs
            .Select(l => new AdCampaignLogDto(
                l.LogId, l.AdCampaignId, l.AdCampaign?.CampaignName,
                l.ActionType, l.ChargedAmount, l.Timestamp,
                l.SponsoredId, l.ProductId, l.Product?.ProductName,
                l.RobotId, l.ZoneId, l.MemberId, l.SessionId,
                l.ActionType == AdActionType.FraudDetected))
            .ToList();

        return new PaginatedResponse<AdCampaignLogDto>(items, totalCount, pageNumber, pageSize, totalPages);
    }

    private async Task<(bool IsFraud, string? FraudReason)> CheckAndDetectFraudAsync(
        LogInteractionRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.SessionId) && !request.MemberId.HasValue)
            return (false, null);

        var windowStart = DateTime.UtcNow.AddSeconds(-FraudDetectionWindowSeconds);

        IQueryable<AdCampaignLog> baseQuery = db.AdCampaignLogs
            .Where(l => l.AdCampaignId == request.AdCampaignId)
            .Where(l => l.ActionType == AdActionType.Click)
            .Where(l => l.Timestamp >= windowStart);

        if (!string.IsNullOrEmpty(request.SessionId))
            baseQuery = baseQuery.Where(l => l.SessionId == request.SessionId);
        else if (request.MemberId.HasValue)
            baseQuery = baseQuery.Where(l => l.MemberId == request.MemberId);

        var recentClicks = await baseQuery.CountAsync(cancellationToken);

        if (recentClicks >= MaxClicksPerWindow)
        {
            var identifier = !string.IsNullOrEmpty(request.SessionId)
                ? $"SessionID={request.SessionId}"
                : $"MemberID={request.MemberId}";

            logger.LogWarning(
                "Fraud detected: {Identifier} exceeded max clicks ({Max}) in {Window}s. CampaignId={CampaignId}",
                identifier, MaxClicksPerWindow, FraudDetectionWindowSeconds, request.AdCampaignId);

            return (true, localizer.Get("FraudExcessiveClicks", MaxClicksPerWindow, FraudDetectionWindowSeconds));
        }

        return (false, null);
    }

    private async Task AddSponsoredProductsAsync(int campaignId, List<int> productIds, CancellationToken cancellationToken)
    {
        var sponsoredProducts = productIds
            .Distinct()
            .Select(productId => new SponsoredProduct
            {
                AdCampaignId = campaignId,
                ProductId = productId,
                Priority = 0,
                Status = SponsoredProductStatus.Active
            })
            .ToList();

        db.SponsoredProducts.AddRange(sponsoredProducts);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateProductsExistAsync(List<int> productIds, CancellationToken cancellationToken)
    {
        var existingIds = await db.Products
            .Where(p => productIds.Contains(p.ProductId))
            .Select(p => p.ProductId)
            .ToListAsync(cancellationToken);

        var missingIds = productIds.Except(existingIds).ToList();
        if (missingIds.Count != 0)
            throw new KeyNotFoundException(localizer.Get("ProductsNotFound", string.Join(", ", missingIds)));
    }

    private static CampaignResponseDto MapCampaignToDto(AdCampaign c)
    {
        return new CampaignResponseDto(
            c.AdCampaignId, c.CampaignName,
            c.PackageId, c.Package?.PackageName ?? string.Empty,
            c.BrandId, c.Brand?.BrandName ?? string.Empty,
            c.SemanticObjectId, c.StartDate, c.EndDate, c.Status,
            c.SponsoredProducts?.Count ?? 0,
            c.AdCampaignLogs?.Sum(l => l.ChargedAmount) ?? 0);
    }
}
