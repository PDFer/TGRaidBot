using System;
using System.Collections.Generic;
using System.Text;

namespace TGRaidBot
{
    public class TGMessage : IMessage
    {
        public int MessageId { get; set; }
        public object Content { get; set; }
        public long ChatId { get; set; }

        public TGMessage(Telegram.Bot.Types.Message message)
        {
            Content = message;
            MessageId = message.MessageId;
            ChatId = message.Chat.Id;

        }

    }
}
