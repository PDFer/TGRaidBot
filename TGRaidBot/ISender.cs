using System;
using System.Collections.Generic;
using System.Text;

namespace TGRaidBot
{
    interface ISender
    {
        void OnMessageReceived(object sender, DataEventArgs args);

        void Save();

        string GetStatus();
    }
}
