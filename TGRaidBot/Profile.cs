using System;
using System.Collections.Generic;
using System.Text;

namespace TGRaidBot
{
    public class Profile
    {
        public string Name { get; set; }

        public bool Default { get; set; }

        public List<string> Gyms { get; set; } = new List<string>();




    }
}
