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

        protected override string Prefix => "!";

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

            var requestChannel = Channels.FirstOrDefault(ch => ch.Id == (long)message.Channel.Id);

            if (requestChannel == null)
            {
                requestChannel = new ServiceChannel { Id = (long)message.Channel.Id, Name = message.Channel.Name };
            }

            if (messageText.Length == 1)
            {
                await ProcessCommand(requestChannel, (long)message.Author.Id, messageText[0].Substring(1));
            }
            else
            {
                await ProcessCommand(requestChannel, (long)message.Author.Id, messageText[0].Substring(1), messageText[1]);

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

        protected async override Task Send(Raid raid)
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

        protected async override Task Send(ServiceChannel serviceChannel, string message)
        {
            var channel = client.GetChannel((ulong)serviceChannel.Id);
            if (channel is ITextChannel ichannel)
            {
                await ichannel.SendMessageAsync(message);
                //var splitName = serviceChannel.Name.Split('#');
                //if (splitName.Length != 2)
                //{
                //    return;
                //}
                //var user = client.GetUser(splitName[0].Substring(1), splitName[1]);
                //if (user != null)
                //{
                //    var dmchannel = user.GetOrCreateDMChannelAsync().Result;
                //    await dmchannel.SendMessageAsync(message);
                //}
            }
            //else if (channel is SocketTextChannel textChannel)
            //{
            //    await textChannel.SendMessageAsync(message);
            //}
            //throw new NotImplementedException();
        }
    }
}
