using Intersect.Core;
using Intersect.Server.Collections.Sorting;
using Intersect.Server.Database;
using Intersect.Server.Database.PlayerData;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Intersect.Server.Web.Pages.Shared.Players;

public partial class PlayerListModel : PageModel
{
    public string? Caption { get; set; }

    public int Count { get; set; } = 10;

    public int Page { get; set; }

    public bool ShowRank { get; set; } = true;

    public SortDirection SortDirection { get; set; } = SortDirection.Descending;

    /// <summary>
    /// Gets ranked players with minimal data loading to prevent memory leaks.
    /// Only loads Guild relationship (needed for guild name display).
    /// Does NOT load Bank, Hotbar, Items, Quests, Spells, Variables to reduce memory usage.
    /// </summary>
    public static IEnumerable<Intersect.Server.Entities.Player> GetRankedPlayersLightweight(
        int page,
        int count,
        SortDirection sortDirection)
    {
        try
        {
            using var context = DbInterface.CreatePlayerContext();
            var query = context.Players.AsQueryable();

            // Only include Guild (needed for guild name), skip all other relationships
            query = query.Include(p => p.Guild);

            // Order and paginate
            var orderedQuery = sortDirection == SortDirection.Ascending
                ? query.OrderBy(p => p.Level).ThenBy(p => p.Exp)
                : query.OrderByDescending(p => p.Level).ThenByDescending(p => p.Exp);

            var results = orderedQuery
                .Skip(page * count)
                .Take(count)
                .AsNoTracking() // Critical: prevents entities from being tracked, allowing GC
                .ToList();

            // Explicitly dispose context before returning to ensure all resources are released
            // The 'using' statement handles this, but being explicit about our intent
            
            return results;
        }
        catch (Exception exception)
        {
            ApplicationContext.Context.Value?.Logger.LogError(exception, "Error ranking players");
            return Array.Empty<Intersect.Server.Entities.Player>();
        }
    }
}
