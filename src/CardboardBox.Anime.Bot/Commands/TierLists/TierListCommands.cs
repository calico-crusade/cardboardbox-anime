using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardboardBox.Anime.Bot.Commands.TierLists;

public class TierListCommands
{
    private readonly ITierFetchService _tierApi;

    public TierListCommands(ITierFetchService tierApi)
    {
        _tierApi = tierApi;
    }

    //[Command("tier", "Fetches a tier list", LongRunning = true)]
    //public async Task GetTier(SocketSlashCommand cmd,
    //    [Option("Url", true)] string url)
    //{
    //    var item = await _tierApi.FetchTierList(url);
    //    if (item == null)
    //    {
    //        await cmd.Modify("Something went wrong");
    //        return;
    //    }

    //    await cmd.Modify($"Found it! {item.Title} (Tiers: {item.Tiers.Length} - Images: {item.Images.Length})");

    //}
}
