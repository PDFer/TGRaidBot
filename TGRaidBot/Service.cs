using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace TGRaidBot
{

    public abstract class Service: ISender
    {
        public event EventHandler GymListChanged;

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

        protected abstract void Send(Raid raid);

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
