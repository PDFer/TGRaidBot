using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace TGRaidBot
{

    public abstract class Service: ISender
    {
        public event EventHandler GymListChanged;

        protected abstract string Prefix { get; }

        public virtual string Token { get; set; }

        public List<ServiceChannel> Channels { get; } = new List<ServiceChannel>();

        [XmlIgnore]
        public Collection<Raid> Raids { get; private set; } = new Collection<Raid>();

        public Service()
        {
            LoadRaids();
        }

        public void OnMessageReceived(object sender, DataEventArgs args)
        {
            ParseMessage(args.Message);
        }

        protected abstract Task Send(Raid raid);

        protected abstract Task Send(ServiceChannel serviceChannel, string message);

        public void SaveGyms()
        {
            GymListChanged?.Invoke(this, new EventArgs());
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

        public async Task ProcessCommand(ServiceChannel requestChannel, long userId, string command, params string[] parameters)
        {
            switch (command.ToLower())
            {
                case "start":
                case "help":

                    await Send(requestChannel,
$@"Voit hallita haluamiasi hälytyksiä lähettällä yksityisviestillä minulle seuraavia komentoja: 
{Prefix}add Salin Nimi  - Lisää salin seurantaan.
{Prefix}remove Salin Nimi  - Poistaa salin seurannasta.
{Prefix}list  - Listaa seuratut salit.
{Prefix}set ProfiiliNimi aika - Asettaa profiilin, vapaaehtoiseen aikakentään voi määrittää kuinka monta {ServiceChannel.GetTimerUnit()} profiili pysyy päällä.
{Prefix}profiles
{Prefix}save ProfiiliNimi - Tallentaa tämän hetkiset salit profiiliin");
                    break;
                case "list":

                    if (requestChannel == null || !requestChannel.Gyms.Any())
                    {
                        await Send(requestChannel, "Sinulla ei ole yhtään salia seurannassa.");
                    }
                    else
                    {
                        string reply = requestChannel.Gyms.First();
                        foreach (var requestChannelGym in requestChannel.Gyms.Skip(1))
                        {
                            reply = $"{reply}, {requestChannelGym}";
                        }

                        await Send(requestChannel, reply);
                    }
                    break;
                case "add":
                    if (parameters.Length != 1) break;

                    if (!Channels.Contains(requestChannel))
                    {
                        requestChannel.Gyms.Add(parameters[0]);
                        Channels.Add(requestChannel);
                        await Send(requestChannel, $"{parameters[0]} lisätty.");
                        SaveGyms();
                    }
                    else
                    {
                        if (requestChannel.Operators.Any() && !requestChannel.Operators.Contains(userId))
                        {
                            await Send(requestChannel, "Sinulla ei ole oikeuksia lisätä saleja.");
                            break;
                        }
                        if (!requestChannel.Gyms.Contains(parameters[0]))
                        {
                            requestChannel.Gyms.Add(parameters[0]);
                            await Send(requestChannel, $"{parameters[0]} lisätty.");
                            SaveGyms();
                        }
                    }
                    break;
                case "remove":

                    if (requestChannel.Operators.Any() && !requestChannel.Operators.Contains(userId))
                    {
                        await Send(requestChannel, "Sinulla ei ole oikeuksia poistaa saleja.");
                        break;
                    }

                    if (parameters.Length < 1)
                    {
                        await Send(requestChannel, $"Anna poistettavan salin nimi komennon perään Esim. {Prefix}remove Esimerkkisali Numero 1");
                        break;
                    }

                    if (requestChannel.Gyms.Contains(parameters[0]))
                    {
                        requestChannel.Gyms.Remove(parameters[0]);
                        SaveGyms();
                        await Send(requestChannel, $"Sali {parameters[0]} poistettu seurannasta.");
                    }
                    else
                    {
                        await Send(requestChannel, $"Sinulla ei ole salia {parameters[0]} seurannassa. Tarkista kirjoititko salin nimen oikein.");
                        break;
                    }
                    break;
                case "set":
                    if (parameters.Length < 1)
                    {
                        await Send(requestChannel, $"Anna profiilin nimi. Esim: {Prefix}SetProfile Työ.");
                    }
                    var splitMessageText = parameters[0].Split(" ", 2);
                    int duration = 0;
                    if (splitMessageText.Length > 1 && int.TryParse(splitMessageText[1], out duration))
                    { }
                    if (!requestChannel.SetProfile(splitMessageText[0], duration))
                    {
                        await Send(requestChannel, $"Profiilia {splitMessageText[0]} ei löydy.");
                    }
                    else
                    {
                        await Send(requestChannel, $"Profiili {splitMessageText[0]} asetettu aktiiviseksi.");
                        if (duration > 0)
                        {
                            await Send(requestChannel, $"Vakioprofiili asetetaan takaisin {duration} {ServiceChannel.GetTimerUnit()} päästä.");
                        }

                    }

                    break;
                case "profiles":
                    if (!requestChannel.Profiles.Any())
                    {
                        await Send(requestChannel, "Sinulla ei ole profiileja.");
                        break;
                    }
                    string profiilit = "";
                    foreach (var profiili in requestChannel.Profiles)
                    {
                        profiilit += $"{profiili.Name} ";
                    }
                    await Send(requestChannel, profiilit);
                    break;
                case "save":
                    if (parameters.Length < 1)
                    {
                        await Send(requestChannel, $"Anna tallennettavan profiilin nimi. Esim: {Prefix}save Koti");
                        break;
                    }
                    if (parameters[0].Contains(" "))
                    {
                        await Send(requestChannel, "Profiilin nimessä ei saa olla välilyöntejä.");
                        break;
                    }
                    requestChannel.SaveProfile(parameters[0]);
                    SaveGyms();
                    await Send(requestChannel, $"Profiili {parameters[0]} tallennettu");
                    break;
            }
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

        public virtual void Save()
        {
            throw new NotImplementedException();
        }

        public virtual string GetStatus()
        {
            throw new NotImplementedException();
        }
    }
}
