using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Resources;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Timers;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types.Enums;


namespace TGRaidBot
{
    public class TelegramService : Service
    {
        private string _token;

        private bool _stopUpdates;

        public override string Token
        {
            get
            {
                return _token;
            }
            set
            {
                _token = value;
                Bot = new TelegramBotClient(_token);
                PollUpdates();
            }

        }

        protected override string Prefix => "/";

        [XmlIgnore]
        private TelegramBotClient Bot { get; set; }
       

        public override string GetStatus()
        {
            var result = Bot.GetMeAsync();
            result.Wait();

            var resultText = result.Result.ToString();
            foreach (var raid in Raids)
            {
                resultText = $"{resultText}\n  {raid.Name} ends at {raid.EndTime} raiders {raid.Attendance.Count}";
            }

            return resultText;
        }

        public override void Save()
        {
            var serialized = JsonConvert.SerializeObject(Raids);
            File.WriteAllText("RaidState.json", serialized);
        }

        protected override async Task Send(Raid raid)
        {
            if ( raid == null || !Raids.Contains(raid)) return;

            var link = $"<a href =\"https://raidikalu.herokuapp.com/#raidi-{raid.Id}\">{raid.Name}</a>";

            var message = raid.ComposeMessage(link);
            
            bool useDelay = false;
            if (raid.Messages.Any())
            {
                try
                {
                    useDelay = raid.Messages.Count > 25;
                    foreach (var raidMessage in raid.Messages)
                    {
                        var editResult = await Bot.EditMessageTextAsync(raidMessage.ChatId, raidMessage.MessageId, message,
                            ParseMode.Html, true);
                        if (useDelay)
                        {
                            Task.Delay(200).Wait();
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
                        raid.Messages.Add(new TGMessage(await Bot.SendTextMessageAsync(serviceChannel.Id, message, ParseMode.Html, true,
                            DateTime.Now.Hour < 9 || DateTime.Now.Hour > 22)));
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
        }

        protected async override Task Send(ServiceChannel serviceChannel, string message)
        {
            await Bot.SendTextMessageAsync(serviceChannel.Id, message);
        }

        private async void PollUpdates()
        {
            int updateId = 0;
            while (!_stopUpdates)
            {
                var updates = await Bot.GetUpdatesAsync(updateId);
                foreach (var update in updates)
                {
                    updateId = update.Id + 1;
                    if (update.Type == UpdateType.Message && update?.Message?.Text != null && update.Message.Text.StartsWith(Prefix))
                    {
                        var messageText = update.Message.Text.Split(' ', 2);
                        //var parameters = new string[];
                        //if (messageText.Length > 1)
                        //{
                        //    parameters.Append(messageText[1]);
                        //}

                        var requestChannel = Channels.FirstOrDefault(ch => ch.Id == update.Message.Chat.Id);

                        if (requestChannel == null)
                        {
                            requestChannel = new ServiceChannel { Id = update.Message.Chat.Id };
                            if (update.Message.Chat.Type == ChatType.Private)
                            {
                                requestChannel.Name = update.Message.Chat.Username;
                            }
                            else
                            {
                                requestChannel.Name = update.Message.Chat.Title;
                            }
                        }
                        if (messageText.Length == 1)
                        {
                            await ProcessCommand(requestChannel, update.Message.From.Id, messageText[0].Substring(1));
                        }
                        else
                        {
                            await ProcessCommand(requestChannel, update.Message.From.Id, messageText[0].Substring(1), messageText[1]);

                        }

                        //switch (messageText.First().ToLower())
                        //{
                        //    case "/start":
                        //    case "/help":
                        //        await Bot.SendTextMessageAsync(update.Message.Chat.Id,
                        //            "Voit hallita haluamiasi hälytyksiä lähettällä yksityisviestillä minulle seuraavia komentoja: \n /add Salin Nimi  - Lisää salin seurantaan. \n /remove Salin Nimi  - Poistaa salin seurannasta.\n /list  - Listaa seuratut salit.",
                        //            replyToMessageId: update.Message.MessageId);
                        //        break;
                        //    case "/list":
                        //        requestChannel = Channels.FirstOrDefault(ch => ch.Id == update.Message.Chat.Id);
                        //        if (requestChannel == null || !requestChannel.Gyms.Any())
                        //        {
                        //            await Bot.SendTextMessageAsync(update.Message.Chat.Id,
                        //                "Sinulla ei ole yhtään salia seurannassa.",
                        //                replyToMessageId: update.Message.MessageId);
                        //        }
                        //        else
                        //        {
                        //            string reply = requestChannel.Gyms.First();
                        //            foreach (var requestChannelGym in requestChannel.Gyms.Skip(1))
                        //            {
                        //                reply = $"{reply}, {requestChannelGym}";
                        //            }

                        //            await Bot.SendTextMessageAsync(update.Message.Chat.Id, reply,
                        //                replyToMessageId: update.Message.MessageId);
                        //        }

                        //        break;
                        //    case "/add":
                        //        requestChannel = Channels.FirstOrDefault(ch => ch.Id == update.Message.Chat.Id);
                        //        if (messageText.Length < 2) break;

                        //        if (requestChannel == null)
                        //        {
                        //            requestChannel = new ServiceChannel
                        //            {
                        //                Id = update.Message.Chat.Id
                        //            };
                        //            if (update.Message.Chat.Type == ChatType.Private)
                        //            {
                        //                requestChannel.Name = update.Message.Chat.Username;
                        //            }
                        //            else
                        //            {
                        //                requestChannel.Name = update.Message.Chat.Title;
                        //            }
                        //            requestChannel.Gyms.Add(messageText[1]);
                        //            Channels.Add(requestChannel);
                        //            await Bot.SendTextMessageAsync(update.Message.Chat.Id,
                        //                $"{messageText[1]} lisätty.");
                        //            SaveGyms();
                        //        }
                        //        else
                        //        {
                        //            if (requestChannel.Operators.Any() && !requestChannel.Operators.Contains(update.Message.From.Id))
                        //            {
                        //                await Bot.SendTextMessageAsync(update.Message.Chat.Id,
                        //                    "Sinulla ei ole oikeuksia lisätä saleja.",
                        //                    replyToMessageId: update.Message.MessageId);
                        //                break;
                        //            }
                        //            if (!requestChannel.Gyms.Contains(messageText[1]))
                        //            {
                        //                requestChannel.Gyms.Add(messageText[1]);
                        //                await Bot.SendTextMessageAsync(update.Message.Chat.Id,
                        //                    $"{messageText[1]} lisätty.");
                        //                SaveGyms();
                        //            }
                        //        }
                        //        break;
                        //    case "/remove":
                        //        requestChannel = Channels.FirstOrDefault(ch => ch.Id == update.Message.Chat.Id);
                        //        if (requestChannel == null) break;

                        //        if (requestChannel.Operators.Any() && !requestChannel.Operators.Contains(update.Message.From.Id))
                        //        {
                        //            await Bot.SendTextMessageAsync(update.Message.Chat.Id,
                        //                "Sinulla ei ole oikeuksia poistaa saleja.",
                        //                replyToMessageId: update.Message.MessageId);
                        //            break;
                        //        }

                        //        if (messageText.Length < 2)
                        //        {
                        //            await Bot.SendTextMessageAsync(update.Message.Chat.Id,
                        //                "Anna poistettavan salin nimi komennon perään Esim. /remove Esimerkkisali Numero 1",
                        //                replyToMessageId: update.Message.MessageId);
                        //            break;
                        //        }

                        //        if (requestChannel.Gyms.Contains(messageText[1]))
                        //        {
                        //            requestChannel.Gyms.Remove(messageText[1]);
                        //            SaveGyms();
                        //            await Bot.SendTextMessageAsync(update.Message.Chat.Id,
                        //                $"Sali {messageText[1]} poistettu seurannasta.",
                        //                replyToMessageId: update.Message.MessageId);
                        //        }
                        //        else
                        //        {
                        //            await Bot.SendTextMessageAsync(update.Message.Chat.Id,
                        //                $"Sinulla ei ole salia {messageText[1]} seurannassa. Tarkista kirjoititko salin nimen oikein.",
                        //                replyToMessageId: update.Message.MessageId);
                        //            break;
                        //        }

                        //        break;
                        //}

                    }
                }
            }
        }
    }
}
