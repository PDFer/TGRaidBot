using System;
using System.Collections.Generic;
using System.Text;

namespace TGRaidBot
{
    public class ServiceChannel
    {
        public string Name { get; set; }

        public long Id { get; set; }

        public string Nick { get; set; }

        public List<string> Gyms { get; } = new List<string>();

        public List<long> Operators { get; } = new List<long>();

    }
}
