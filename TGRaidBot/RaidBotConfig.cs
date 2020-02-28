using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace TGRaidBot
{
    public class RaidBotConfig
    {
        [XmlElement("Telegram", Type = (typeof(TelegramService)))]
        public List<Service> Services { get; } = new List<Service>();

        public RaidBotConfig()
        {

        }
    }
}
