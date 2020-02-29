using Discord;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using NLog;
using System.Linq;

namespace TGRaidBot
{
    public class DiscordService : Service
    { 
        private DiscordSocketClient client;

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


        public async void Start()
        {
            logger.Info("Initializing Discord bot.");
            client = new DiscordSocketClient();
            client.Log += Log;
            client.LoginAsync(TokenType.Bot, Token).Wait();

            await client.StartAsync();
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

            var link = $"[{raid.Name}](https://raidikalu.herokuapp.com/#raidi-{raid.Id})";

            var message = raid.ComposeMessage(link);

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
                            await discordMessage.ModifyAsync(m => m.Content = message);
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
                        if (channel is SocketTextChannel textChannel)
                        {
                            raid.Messages.Add(new DiscordMessage(await textChannel.SendMessageAsync(message)));
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
