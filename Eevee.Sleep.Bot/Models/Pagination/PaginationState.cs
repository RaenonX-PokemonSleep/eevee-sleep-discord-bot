using Eevee.Sleep.Bot.Enums;

namespace Eevee.Sleep.Bot.Models.Pagination;

public class PaginationState<T> where T : class {
    public required int CurrentPage { get; set; }

    public required int TotalPages { get; init; }

    public required IEnumerable<T> Collection { get; init; }

    public required ButtonId ActionButtonId { get; init; }

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}