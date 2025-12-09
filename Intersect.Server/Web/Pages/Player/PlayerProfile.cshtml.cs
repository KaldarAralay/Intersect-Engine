using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Intersect.Server.Web.Pages.Player;

public partial class PlayerProfileModel : PageModel
{
    [FromRoute]
    public string Id { get; set; }

    public Intersect.Server.Entities.Player? ViewedPlayer { get; set; }

    public void OnGet()
    {
        // Use lightweight query with AsNoTracking to prevent memory leaks
        // Entities won't be tracked by EF Core, allowing proper garbage collection
        try
        {
            using var context = Intersect.Server.Database.DbInterface.CreatePlayerContext();
            var query = context.Players.AsQueryable();

            // Only include Guild (for guild name) and Items (for equipment display)
            query = query.Include(p => p.Guild).Include(p => p.Items);

            if (Guid.TryParse(Id, out var playerId))
            {
                ViewedPlayer = query
                    .Where(p => p.Id == playerId)
                    .AsNoTracking() // Critical: prevents tracking, allows GC
                    .FirstOrDefault();
            }
            else
            {
                ViewedPlayer = query
                    .Where(p => p.Name == Id)
                    .AsNoTracking() // Critical: prevents tracking, allows GC
                    .FirstOrDefault();
            }
        }
        catch
        {
            // If query fails, try again with a fresh context and minimal data
            // NEVER use Player.Find() as it returns tracked entities that cause memory leaks
            try
            {
                using var fallbackContext = Intersect.Server.Database.DbInterface.CreatePlayerContext();
                if (Guid.TryParse(Id, out var playerId))
                {
                    ViewedPlayer = fallbackContext.Players
                        .Where(p => p.Id == playerId)
                        .Include(p => p.Guild)
                        .AsNoTracking() // Critical: must use AsNoTracking to prevent memory leaks
                        .FirstOrDefault();
                }
                else
                {
                    ViewedPlayer = fallbackContext.Players
                        .Where(p => p.Name == Id)
                        .Include(p => p.Guild)
                        .AsNoTracking() // Critical: must use AsNoTracking to prevent memory leaks
                        .FirstOrDefault();
                }
            }
            catch
            {
                // If all queries fail, return null rather than using Player.Find()
                // Player.Find() returns tracked entities that stay in memory
                ViewedPlayer = null;
            }
        }
    }
}

