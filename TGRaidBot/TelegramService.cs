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
    public class TelegramService : Service, ISender
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
        


        [XmlIgnore]
        public Collection<Raid> Raids { get; private set; } = new Collection<Raid>();

        [XmlIgnore]
        private TelegramBotClient Bot { get; set; }

        private readonly int petriChatId = 416974747;
        private readonly int RaidikaluTesti = -258669304;
        private readonly long HerwoodRaid = -1001127957863;

        public TelegramService()
        {
            LoadRaids();
        }

        private void LoadRaids()
        {
            if (File.Exists("RaidState.json"))
            {
                var jsonState = File.ReadAllText("RaidState.json");
                if (!string.IsNullOrWhiteSpace(jsonState))
                {
                    Raids = JsonConvert.DeserializeObject<Collection<Raid>>(jsonState);
                    var raidToRemove = Raids.FirstOrDefault(r => r.EndTime < DateTime.Now);
                    while (raidToRemove != null)
                    {
                        Raids.Remove(raidToRemove);
                        raidToRemove = Raids.FirstOrDefault(r => r.EndTime < DateTime.Now);
                    }
                }
            }
        }

        public void OnMessageReceived(object sender, DataEventArgs args)
        {
            ParseMessage(args.Message);
        }

        public void ParseMessage(EventMessage eventMessage)
        {
            Raid raid = null;
            if (eventMessage != null && eventMessage.Event == EventMessage.EventType.Raid)
            {
                raid = ParseRaid(eventMessage.Data);
            }
            else if (eventMessage != null && eventMessage.Event == EventMessage.EventType.Attendance)
            {
                raid = ParseAttendance(eventMessage.Data);
            }
            Send(raid);
        }

        public string GetStatus()
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

        public void Save()
        {
            var serialized = JsonConvert.SerializeObject(Raids);
            File.WriteAllText("RaidState.json", serialized);
        }


        private Raid ParseRaid(EventData data)
        {
            if (data == null || !Channels.Any(ch => ch.Gyms.Any(gym => gym.Contains(data.Gym)))) return null;

            if (!data.Start.HasValue && !data.End.HasValue)
            {
                Console.WriteLine("No Start or End time.");
                return null;
            }

            DateTime start = DateTimeOffset.FromUnixTimeSeconds(data.Start.GetValueOrDefault()).LocalDateTime;
            DateTime end = DateTimeOffset.FromUnixTimeSeconds(data.End.GetValueOrDefault()).LocalDateTime;

            if (data.Start.HasValue && !data.End.HasValue)
            {
                end = start + new TimeSpan(0, 45, 0);
            }
            else if (!data.Start.HasValue)
            {
                start = end - new TimeSpan(0, 45, 0);
            }

            if (end < DateTime.Now) return null;

            var raid = Raids.FirstOrDefault(r => r.Id == data.Raid);
            if (raid != null)
            {
                raid.Pokemon = data.Pokemon;
                raid.Tier = data.Tier.GetValueOrDefault();
                raid.StartTime = start;
                raid.EndTime = end;
            }
            else
            {
                raid = new Raid(data.Raid, data.Gym)
                {
                    Pokemon = data.Pokemon,
                    Tier = data.Tier.GetValueOrDefault(),
                    StartTime = start,
                    EndTime = end
                };
                Raids.Add(raid);
            }
            
            var raidToRemove = Raids.FirstOrDefault(r => r.EndTime < DateTime.Now);
            var currentRaidRemoved = false;
            while (raidToRemove != null)
            {
                Raids.Remove(raidToRemove);
                if (raidToRemove == raid)
                {
                    currentRaidRemoved = true;
                }
                raidToRemove = Raids.FirstOrDefault(r => r.EndTime < DateTime.Now);
            }

            if (currentRaidRemoved)
            {
                return null;
            }

            return raid;
        }

        private Raid ParseAttendance(EventData data)
        {
            var raid = Raids.FirstOrDefault(r => r.Id == data.Raid);
            if (raid == null) return null;

            if (string.IsNullOrWhiteSpace(data.Submitter)) return null;
            raid.AddAttendee(data.Submitter, data.Time);
            return raid;
        }

        private async void Send(Raid raid)
        {
            if ( raid == null || !Raids.Contains(raid)) return;

            string message = "";
            //if (raid.RaidInfoIsDirty)
            //{
                message =
                    $"T{raid.Tier} raid salilla <a href=\"https://raidikalu.herokuapp.com/#raidi-{raid.Id}\">{raid.Name}</a>:";
                if (!string.IsNullOrWhiteSpace(raid.Pokemon))
                {
                    message = $"{message} {raid.Pokemon},";
                }

                if (raid.StartTime > DateTime.Now)
                {
                    message = $"{message} alkaa {raid.StartTime.Hour}:{raid.StartTime.Minute:D2}";
                }
                else
                {
                    message = $"{message} loppuu {raid.EndTime.Hour}:{raid.EndTime.Minute:D2}";
                }
                raid.RaidInfoIsDirty = false;
                //if (raid.Message != null)
                //{
                //    var splitMsg = raid.Message.Text.Split('\n');
                //    if (splitMsg.Length > 1)
                //    {
                //        message = $"{message}\n{splitMsg[1]}";
                //    }

                //    var editResult = await Bot.EditMessageTextAsync(ChatId, raid.Message.MessageId, message, ParseMode.Html, true);
                //    raid.Message = editResult;
                //}
                //else
                //{
                //    message = $"{message}\n  Ilmoittautuneita  0";
                //    var result = await Bot.SendTextMessageAsync(ChatId, message, ParseMode.Html, true,
                //        DateTime.Now.Hour < 9 || DateTime.Now.Hour > 22);
                //    raid.Message = result;
                //}
            //}

            

            message = $"{message}\n  Ilmoittautuneita";

            if (raid.Attendance.Count == 0)
            {
                message = $"{message}  0";
            }
            else
            {
                var times = new Dictionary<string, int>();
                foreach (var attendee in raid.Attendance)
                {
                    if (attendee.Key == null || attendee.Value == null) continue;

                    if (!times.ContainsKey(attendee.Value))
                    {
                        times[attendee.Value] = 1;
                    }
                    else
                    {
                        times[attendee.Value] = times[attendee.Value] + 1;
                    }
                }
                var sortedTimes = times.Keys.ToList();
                sortedTimes.Sort();
                foreach (var sortedTime in sortedTimes)
                {
                    message = $"{message}  {sortedTime} ({times[sortedTime]})";
                }
            }

            raid.AttendanceIsDirty = false;
            raid.LastAttendanceUpdate = DateTime.Now;
            raid.PendingSend = false;

            bool useDelay = false;
            if (raid.Messages.Any())
            {
                
                try
                {
                    useDelay = raid.Messages.Count > 25;
                    foreach (var raidMessage in raid.Messages)
                    {
                        var editResult = await Bot.EditMessageTextAsync(raidMessage.Chat.Id, raidMessage.MessageId, message,
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
                        raid.Messages.Add(await Bot.SendTextMessageAsync(serviceChannel.Id, message, ParseMode.Html, true,
                            DateTime.Now.Hour < 9 || DateTime.Now.Hour > 22));
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

        private async void PollUpdates()
        {
            int updateId = 0;
            while (!_stopUpdates)
            {
                var updates = await Bot.GetUpdatesAsync(updateId);
                foreach (var update in updates)
                {
                    updateId = update.Id + 1;
                    if (update.Type == UpdateType.Message && update?.Message?.Text != null)
                    {
                        ServiceChannel requestChannel = null;
                        var messageText = update.Message.Text.Split(' ', 2);
                        switch (messageText.First().ToLower())
                        {
                            case "/start":
                            case "/help":
                                await Bot.SendTextMessageAsync(update.Message.Chat.Id,
                                    "Voit hallita haluamiasi hälytyksiä lähettällä yksityisviestillä minulle seuraavia komentoja: \n /add Salin Nimi  - Lisää salin seurantaan. \n /remove Salin Nimi  - Poistaa salin seurannasta.\n /list  - Listaa seuratut salit.",
                                    replyToMessageId: update.Message.MessageId);
                                break;
                            case "/list":
                                requestChannel = Channels.FirstOrDefault(ch => ch.Id == update.Message.Chat.Id);
                                if (requestChannel == null || !requestChannel.Gyms.Any())
                                {
                                    await Bot.SendTextMessageAsync(update.Message.Chat.Id,
                                        "Sinulla ei ole yhtään salia seurannassa.",
                                        replyToMessageId: update.Message.MessageId);
                                }
                                else
                                {
                                    string reply = requestChannel.Gyms.First();
                                    foreach (var requestChannelGym in requestChannel.Gyms.Skip(1))
                                    {
                                        reply = $"{reply}, {requestChannelGym}";
                                    }

                                    await Bot.SendTextMessageAsync(update.Message.Chat.Id, reply,
                                        replyToMessageId: update.Message.MessageId);
                                }
                                
                                break;
                            case "/add":
                                requestChannel = Channels.FirstOrDefault(ch => ch.Id == update.Message.Chat.Id);
                                if (messageText.Length < 2) break;

                                if (requestChannel == null)
                                {
                                    requestChannel = new ServiceChannel
                                    {
                                        Id = update.Message.Chat.Id
                                    };
                                    if (update.Message.Chat.Type == ChatType.Private)
                                    {
                                        requestChannel.Name = update.Message.Chat.Username;
                                    }
                                    else
                                    {
                                        requestChannel.Name = update.Message.Chat.Title;
                                    }
                                    requestChannel.Gyms.Add(messageText[1]);
                                    Channels.Add(requestChannel);
                                    await Bot.SendTextMessageAsync(update.Message.Chat.Id,
                                        $"{messageText[1]} lisätty.");
                                    SaveGyms();
                                }
                                else
                                {
                                    if (requestChannel.Operators.Any() && !requestChannel.Operators.Contains(update.Message.From.Id))
                                    {
                                        await Bot.SendTextMessageAsync(update.Message.Chat.Id,
                                            "Sinulla ei ole oikeuksia lisätä saleja.",
                                            replyToMessageId: update.Message.MessageId);
                                        break;
                                    }
                                    if (!requestChannel.Gyms.Contains(messageText[1]))
                                    {
                                        requestChannel.Gyms.Add(messageText[1]);
                                        await Bot.SendTextMessageAsync(update.Message.Chat.Id,
                                            $"{messageText[1]} lisätty.");
                                        SaveGyms();
                                    }
                                }
                                break;
                            case "/remove":
                                requestChannel = Channels.FirstOrDefault(ch => ch.Id == update.Message.Chat.Id);
                                if (requestChannel == null) break;

                                if (requestChannel.Operators.Any() && !requestChannel.Operators.Contains(update.Message.From.Id))
                                {
                                    await Bot.SendTextMessageAsync(update.Message.Chat.Id,
                                        "Sinulla ei ole oikeuksia poistaa saleja.",
                                        replyToMessageId: update.Message.MessageId);
                                    break;
                                }

                                if (messageText.Length < 2)
                                {
                                    await Bot.SendTextMessageAsync(update.Message.Chat.Id,
                                        "Anna poistettavan salin nimi komennon perään Esim. /remove Esimerkkisali Numero 1",
                                        replyToMessageId: update.Message.MessageId);
                                    break;
                                }

                                if (requestChannel.Gyms.Contains(messageText[1]))
                                {
                                    requestChannel.Gyms.Remove(messageText[1]);
                                    SaveGyms();
                                    await Bot.SendTextMessageAsync(update.Message.Chat.Id,
                                        $"Sali {messageText[1]} poistettu seurannasta.",
                                        replyToMessageId: update.Message.MessageId);
                                }
                                else
                                {
                                    await Bot.SendTextMessageAsync(update.Message.Chat.Id,
                                        $"Sinulla ei ole salia {messageText[1]} seurannassa. Tarkista kirjoititko salin nimen oikein.",
                                        replyToMessageId: update.Message.MessageId);
                                    break;
                                }

                                break;
                        }

                    }
                }
            }
        }
    }
}
