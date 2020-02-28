using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace TGRaidBot
{

    public class Service
    {
        public event EventHandler GymListChanged;

        public virtual string Token { get; set; }

        public List<ServiceChannel> Channels { get; } = new List<ServiceChannel>();

        public void SaveGyms()
        {
            GymListChanged?.Invoke(this, new EventArgs());
        }
    }
}
