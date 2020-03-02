using Discord;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using NLog;
using System.Linq;
using Discord.Commands;

namespace TGRaidBot
{
    public class DiscordService : Service
    { 
        private readonly DiscordSocketClient client;
        private readonly CommandService commands;

        private string _token;

        private Logger logger => NLog.LogManager.GetCurrentClassLogger();

        public override string Token
        {
            get
            {
                return _token;
            }
            set
            {
                _token = value;
                Start();
                //PollUpdates();
            }

        }

        public DiscordService()
        {
            client = new DiscordSocketClient();
            commands = new CommandService();
        }

        public async void Start()
        {
            logger.Info("Initializing Discord bot.");
            await commands.AddModuleAsync<DiscordCommands>(null);
            client.Log += Log;
            client.MessageReceived += HandleCommandAsync;
            client.LoginAsync(TokenType.Bot, Token).Wait();

            await client.StartAsync();
        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {
            // Don't process the command if it was a system message
            var message = arg as SocketUserMessage;
            if (message == null) return;

            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;

            // Determine if the message is a command based on the prefix and make sure no bots trigger commands
            if (!(message.HasCharPrefix('!', ref argPos) ||
                message.HasMentionPrefix(client.CurrentUser, ref argPos)) ||
                message.Author.IsBot)
                return;

            //if (message.Channel != null)
            //{
            //    await message.Channel.SendMessageAsync("Moi");
            //}

            logger.Info($"Received message from {message.Author}, channel: {message.Channel}, with content: {message.Content}");

            var messageText = message.Content.Split(' ', 2);

            ServiceChannel requestChannel;

            switch (messageText[0].ToLower())
            {
                case "!start":
                case "!help":
                    await message.Channel.SendMessageAsync(
                        "Voit hallita haluamiasi hälytyksiä lähettällä yksityisviestillä minulle seuraavia komentoja: \n !add Salin Nimi  - Lisää salin seurantaan. \n !remove Salin Nimi  - Poistaa salin seurannasta.\n !list  - Listaa seuratut salit. \n !setprofile - Aseta profiili");
                    break;
                case "!list":
                    requestChannel = Channels.FirstOrDefault(ch => ch.Id == (long)message.Channel.Id);
                    if (requestChannel == null || !requestChannel.Gyms.Any())
                    {
                        await message.Channel.SendMessageAsync("Sinulla ei ole yhtään salia seurannassa.");
                    }
                    else
                    {
                        string reply = requestChannel.Gyms.First();
                        foreach (var requestChannelGym in requestChannel.Gyms.Skip(1))
                        {
                            reply = $"{reply}, {requestChannelGym}";
                        }

                        await message.Channel.SendMessageAsync(reply);
                    }

                    break;
                case "!add":
                    requestChannel = Channels.FirstOrDefault(ch => ch.Id == (long)message.Channel.Id);
                    if (message.Content.Length < 2) break;

                    if (requestChannel == null)
                    {
                        requestChannel = new ServiceChannel
                        {
                            Id = (long)message.Channel.Id
                        };
                        //if (update.Message.Chat.Type == ChatType.Private)
                        //{
                        //    requestChannel.Name = update.Message.Chat.Username;
                        //}
                        //else
                        //{
                            requestChannel.Name = message.Channel.Name;
                        //}
                        requestChannel.Gyms.Add(messageText[1]);
                        Channels.Add(requestChannel);
                        await message.Channel.SendMessageAsync($"{messageText[1]} lisätty.");
                        SaveGyms();
                    }
                    else
                    {
                        if (requestChannel.Operators.Any() && !requestChannel.Operators.Contains((long)message.Author.Id))
                        {
                            await message.Channel.SendMessageAsync("Sinulla ei ole oikeuksia lisätä saleja.");
                            break;
                        }
                        if (!requestChannel.Gyms.Contains(messageText[1]))
                        {
                            requestChannel.Gyms.Add(messageText[1]);
                            await message.Channel.SendMessageAsync($"{messageText[1]} lisätty.");
                            SaveGyms();
                        }
                    }
                    break;
                case "!remove":
                    requestChannel = Channels.FirstOrDefault(ch => ch.Id == (long)message.Channel.Id);
                    if (requestChannel == null) break;

                    if (requestChannel.Operators.Any() && !requestChannel.Operators.Contains((long)message.Author.Id))
                    {
                        await message.Channel.SendMessageAsync("Sinulla ei ole oikeuksia poistaa saleja.");
                        break;
                    }

                    if (messageText.Length < 2)
                    {
                        await message.Channel.SendMessageAsync("Anna poistettavan salin nimi komennon perään Esim. /remove Esimerkkisali Numero 1");
                        break;
                    }

                    if (requestChannel.Gyms.Contains(messageText[1]))
                    {
                        requestChannel.Gyms.Remove(messageText[1]);
                        SaveGyms();
                        await message.Channel.SendMessageAsync($"Sali {messageText[1]} poistettu seurannasta.");
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync($"Sinulla ei ole salia {messageText[1]} seurannassa. Tarkista kirjoititko salin nimen oikein.");
                        break;
                    }
                    break;
                case "!setprofile":
                    if (messageText.Length == 1)
                    {
                        await message.Channel.SendMessageAsync("Anna profiilin nimi. Esim: !SetProfile Työ.");
                    }
                    requestChannel = Channels.FirstOrDefault(ch => ch.Id == (long)message.Channel.Id);
                    var splitMessageText = messageText[1].Split(" ", 2);
                    int duration = 0;
                    if (splitMessageText.Length > 1 && int.TryParse(splitMessageText[1], out duration))
                    { }
                    if (!requestChannel.SetProfile(splitMessageText[0], duration))
                    {
                        await message.Channel.SendMessageAsync("Kyseistä profiilia ei löydy.");
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync($"Profiili {splitMessageText[0]} asetettu aktiiviseksi.");
                        if (duration > 0)
                        {
                            await message.Channel.SendMessageAsync($"Vakioprofiili asetetaan takaisin {duration} {ServiceChannel.GetTimerUnit()} päästä.");
                        }

                    }

                    break;
                case "!listprofiles":
                    requestChannel = Channels.FirstOrDefault(ch => ch.Id == (long)message.Channel.Id);
                    string profiilit = "";
                    foreach(var profiili in requestChannel.Profiles)
                    {
                        profiilit += $"{profiili.Name} ";
                    }
                    break;
            }
        }

        private async void ProfileTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var serviceChannel = sender as ServiceChannel;
            if (serviceChannel != null)
            {
                var channel = client.GetChannel((ulong)serviceChannel.Id);
                if (channel == null)
                {
                    var splitName = serviceChannel.Name.Split('#');
                    if (splitName.Length != 2)
                    {
                        return;
                    }
                    var user = client.GetUser(splitName[0].Substring(1), splitName[1]);
                    if (user != null)
                    {
                        var dmchannel = user.GetOrCreateDMChannelAsync().Result;
                        await dmchannel.SendMessageAsync("Vakioprofiili on palautettu.");
                    }
                }
                else if (channel is SocketTextChannel textChannel)
                {
                    await textChannel.SendMessageAsync("Vakioprofiili on palautettu.");
                }
            }
        }

        public async void Stop()
        {
            if (client != null)
            await client.StopAsync();
        }


        public override string GetStatus()
        {
            
            return "yay";
            //throw new NotImplementedException();
        }

        public override void Save()
        {
            //throw new NotImplementedException();
        }

        private Task Log(LogMessage msg)
        {
            switch (msg.Severity)

            {
                case LogSeverity.Critical:
                    logger.Fatal(msg.ToString());
                    break;
                case LogSeverity.Error:
                    logger.Error(msg.ToString());
                    break;
                case LogSeverity.Warning:
                    logger.Warn(msg.ToString());
                    break;
                case LogSeverity.Info:
                    logger.Info(msg.ToString());
                    break;
                case LogSeverity.Verbose:
                    logger.Debug(msg.ToString());
                    break;
                case LogSeverity.Debug:
                    logger.Debug(msg.ToString());
                    break;
                default:
                    break;
            };
            return Task.CompletedTask;
        }

        protected async override void Send(Raid raid)
        {
            if (raid == null || !Raids.Contains(raid)) return;

            var link = $"{raid.Name}"; // (https://raidikalu.herokuapp.com/#raidi-{raid.Id})";

            var message = raid.ComposeMessage(link);
            var url = $"https://raidikalu.herokuapp.com/#raidi-{raid.Id}";

            var embed = new EmbedBuilder
            {
                Title = "Raidikalu",
                Url = url
            };

            bool useDelay = false;
            if (raid.Messages.Any())
            {

                try
                {
                    useDelay = raid.Messages.Count > 25;
                    foreach (var raidMessage in raid.Messages)
                    {
                        var discordMessage = raidMessage.Content as Discord.Rest.RestUserMessage;
                        if (discordMessage != null)
                        {
                            await discordMessage.ModifyAsync(m => { m.Content = message; m.Embed = embed.Build(); } );
                        }
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error modifying message: {e.Message}");
                }
            }
            else
            {
                try
                {
                    var sendChannels = Channels.Where(ch => ch.Gyms.Contains(raid.Name)).ToList();
                    useDelay = sendChannels.Count() > 25;
                    foreach (var serviceChannel in sendChannels)
                    {
                        var channel = client.GetChannel((ulong)serviceChannel.Id);
                        if (channel == null)
                        {
                            var splitName = serviceChannel.Name.Split('#');
                            if (splitName.Length != 2)
                            {
                                continue;
                            }
                            var user = client.GetUser(splitName[0].Substring(1), splitName[1]);
                            if (user != null)
                            {
                                var dmchannel = user.GetOrCreateDMChannelAsync().Result;
                                raid.Messages.Add(new DiscordMessage(await dmchannel.SendMessageAsync(message, false, embed.Build())));
                            }
                        }
                        else if (channel is SocketTextChannel textChannel)
                        {
                            raid.Messages.Add(new DiscordMessage(await textChannel.SendMessageAsync(message, false, embed.Build())));
                        }
                        if (useDelay)
                        {
                            Task.Delay(200).Wait();
                        }
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error writing message: {e.Message}");
                }
            }
           
            //throw new NotImplementedException();
        }
    }
}
