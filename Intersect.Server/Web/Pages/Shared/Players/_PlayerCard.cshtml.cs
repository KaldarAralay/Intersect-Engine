using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Intersect.Server.Web.Pages.Shared.Players;

public partial class PlayerCardModel : PageModel
{
    public Intersect.Server.Entities.Player? Player { get; set; }

    public long? Rank { get; set; }

    public long? RankScale { get; set; }
}