using System;
using System.Collections.Generic;
using System.Text;

namespace TGRaidBot
{
    class DiscordMessage : IMessage
    {
        public int MessageId { get; set; }
        public long ChatId { get; set; }
        public object Content { get; set; }

        public DiscordMessage(Discord.IMessage message)
        {
            //MessageId = message.Id;
            Content = message;
        }
    }
}
