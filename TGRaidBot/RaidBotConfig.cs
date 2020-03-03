using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace TGRaidBot
{
    public class RaidBotConfig
    {
        [XmlElement("Telegram", Type = (typeof(TelegramService)))]
        [XmlElement("Discord", Type = (typeof(DiscordService)))]
        public List<Service> Services { get; } = new List<Service>();


        public RaidBotConfig()
        {

        }

        public void Initialize()
        {
            foreach(var service in Services)
            {
                foreach(var channel in service.Channels)
                {
                    var profile = channel.Profiles.FirstOrDefault(p => p.Default);
                    if (profile != null)
                    {
                        channel.Gyms.Clear();
                        channel.Gyms.AddRange(profile.Gyms);
                    }
                }
            }
        }

    }
}
