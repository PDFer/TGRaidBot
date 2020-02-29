using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TGRaidBot
{
    public class DiscordCommands : ModuleBase<SocketCommandContext>
    {
        [Command("Add")]
        public async Task AddAsync ()
        {
            await ReplyAsync("Got Add command.");
        }
    }
}
