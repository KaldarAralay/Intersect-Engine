using Intersect.Core;
using Intersect.Framework.Core;
using Intersect.Server.Core;
using Intersect.Server.Entities;
using Intersect.Server.General;
using Intersect.Server.Networking;
using Intersect.Utilities;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Intersect.Server.Web.Pages;

public class IndexModel : PageModel
{
    public long Uptime { get; set; }
    public long CyclesPerSecond { get; set; }
    public int? ConnectedClients { get; set; }
    public int OnlinePlayers { get; set; }
    public int TotalPlayers { get; set; }

    public void OnGet()
    {
        var cyclesPerSecond = ApplicationContext.GetContext<IServerContext>()?.LogicService.CyclesPerSecond ?? -1;
        Uptime = Timing.Global.Milliseconds;
        CyclesPerSecond = cyclesPerSecond;
        ConnectedClients = Client.Instances?.Count;
        OnlinePlayers = Intersect.Server.Entities.Player.OnlinePlayers.Count;
        TotalPlayers = Intersect.Server.Entities.Player.Count();
    }
}