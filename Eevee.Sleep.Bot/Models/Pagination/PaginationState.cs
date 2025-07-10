using Eevee.Sleep.Bot.Enums;

namespace Eevee.Sleep.Bot.Models.Pagination;

public class PaginationState<T> where T : class {
    public required int CurrentPage { get; set; }
    public required int TotalPages { get; set; }
    public required IEnumerable<T> Collection { get; set; }
    public required ButtonId ActionButtonId { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}