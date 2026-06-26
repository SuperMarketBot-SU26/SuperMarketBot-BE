using System.Collections.Generic;

namespace SmartMarketBot.Application.Models.Common;

public sealed record PaginatedResponse<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);
