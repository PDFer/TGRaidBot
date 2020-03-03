using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Xml.Serialization;

namespace TGRaidBot
{
    public class ServiceChannel
    {
        public enum TimerUnit
        {
            Second = 1000,
            Minute = 60_000,
            Hour = 36_000_000

        }

        public static TimerUnit ProfileTimerUnit = TimerUnit.Second;

        public string Name { get; set; }

        public long Id { get; set; }

        public string Nick { get; set; }

        public List<string> Gyms { get; } = new List<string>();

        public List<Profile> Profiles { get; } = new List<Profile>();

        public List<long> Operators { get; } = new List<long>();

        

        [XmlIgnore]
        private System.Timers.Timer ProfileTimer = new System.Timers.Timer();

        public bool SetProfile(string profileName, int duration = 0)
        {
            var profile = Profiles.FirstOrDefault(p => p.Name == profileName);
            if (profile == null)
            {
                return false;
            }

            Gyms.Clear();
            Gyms.AddRange(profile.Gyms);

            if (duration > 0)
            {
                ProfileTimer.Elapsed += ProfileTimer_Elapsed;
                ProfileTimer.Interval = duration * (int)ProfileTimerUnit;
                ProfileTimer.Start();
            }

            return true;
        }

        public void SaveProfile(string profileName)
        {
            var profile = Profiles.FirstOrDefault(p => p.Name == profileName);
            if (profile != null)
            {
                profile = new Profile { Name = profileName };
                profile.Gyms.AddRange(Gyms);
            }
            else
            {
                profile.Gyms.Clear();
                profile.Gyms.AddRange(Gyms);
            }

        }

        public static string GetTimerUnit()
        {
            switch (ProfileTimerUnit)
            {
                case TimerUnit.Second:
                    return "sekunnin";
                case TimerUnit.Minute:
                    return "minuutin";
                case TimerUnit.Hour:
                    return "tunnin";
            }
            return "sekunnin";
        }

        private void ProfileTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Gyms.Clear();
            var profile = Profiles.FirstOrDefault(p => p.Default == true);
            if (profile != null)
            {
                Gyms.AddRange(profile.Gyms);
            }
        }

    }
}
