using System;
using System.Collections.Generic;
using System.Text;

namespace TGRaidBot
{
    public interface IMessage
    {
        int MessageId { get; set; }

        long ChatId { get; set; }

        object Content { get; set; }

    }
}
