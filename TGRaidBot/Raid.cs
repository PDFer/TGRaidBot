using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;

namespace TGRaidBot
{
    public class Raid
    {
        private DateTime _startTime;
        private DateTime _endTime;
        private string _pokemon;
        private int _tier;
        private readonly Dictionary<string, string> _attendance = new Dictionary<string, string>();
        private string _monster;

        public int Id { get; }

        public string Name { get; }

        public DateTime StartTime
        {
            get => _startTime;
            set
            {
                if (_startTime == value) return;

                _startTime = value;
                RaidInfoIsDirty = true;
            }
        }

        public DateTime EndTime
        {
            get => _endTime;
            set
            {
                if (_endTime == value ) return;

                _endTime = value;
                RaidInfoIsDirty = true;
            }
        }

        public string Pokemon
        {
            get => _pokemon;
            set
            {
                if (_pokemon == value) return;

                _pokemon = value;
                RaidInfoIsDirty = true;
            }
        }

        public string Monster
        {
            get => _monster;
            set
            {
                if (_monster == value) return;

                _monster = value;
                RaidInfoIsDirty = true;
            }
        }

        public int Tier
        {
            get => _tier;
            set
            {
                if (_tier == value) return;

                _tier = value;
                RaidInfoIsDirty = true;
            }
        }

        public List<Message> Messages { get; } = new List<Message>();

        public bool RaidInfoIsDirty { get; set; }

        public bool AttendanceIsDirty { get; set; }

        public DateTime LastAttendanceUpdate { get; set; }

        public bool PendingSend { get; set; }

        public ReadOnlyDictionary<string, string> Attendance { get; }

        public Raid(int id, string name )
        {
            Id = id;
            Name = name;
            Attendance = new ReadOnlyDictionary<string, string>(_attendance);
            LastAttendanceUpdate = DateTime.Now;
        }

        public bool AddAttendee(string name, string time)
        {
            if (!string.IsNullOrWhiteSpace(time))
            {
                TimeSpan serverOffset = TimeZoneInfo.Local.GetUtcOffset(DateTimeOffset.Now);
                var temp = time.Split(':');
                var hour = temp.FirstOrDefault();
                if (hour != null && temp.Length == 2)
                {
                    int intHour = int.Parse(hour);
                    time = $"{intHour + serverOffset.Hours}:{temp[1]}";
                }
            }
            if (_attendance.ContainsKey(name))
            {
                if (string.IsNullOrEmpty(time))
                {
                    _attendance.Remove(name);
                    AttendanceIsDirty = true;
                }
                else if (_attendance[name] != time)
                {
                    _attendance[name] = time;
                    AttendanceIsDirty = true;
                }
            }
            else
            {
                _attendance.Add(name, time);
                AttendanceIsDirty = true;
            }

            return AttendanceIsDirty;
        }
    }
}
