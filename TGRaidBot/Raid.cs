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

        public List<IMessage> Messages { get; } = new List<IMessage>();

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

        public string ComposeMessage(string link)
        {
            string message = "";
            //if (raid.RaidInfoIsDirty)
            //{
            message =
                $"T{Tier} raid salilla {link}:";
            if (!string.IsNullOrWhiteSpace(Pokemon))
            {
                message = $"{message} {Pokemon},";
            }

            if (StartTime > DateTime.Now)
            {
                message = $"{message} alkaa {StartTime.Hour}:{StartTime.Minute:D2}";
            }
            else
            {
                message = $"{message} loppuu {EndTime.Hour}:{EndTime.Minute:D2}";
            }
            RaidInfoIsDirty = false;
            //if (raid.Message != null)
            //{
            //    var splitMsg = raid.Message.Text.Split('\n');
            //    if (splitMsg.Length > 1)
            //    {
            //        message = $"{message}\n{splitMsg[1]}";
            //    }

            //    var editResult = await Bot.EditMessageTextAsync(ChatId, raid.Message.MessageId, message, ParseMode.Html, true);
            //    raid.Message = editResult;
            //}
            //else
            //{
            //    message = $"{message}\n  Ilmoittautuneita  0";
            //    var result = await Bot.SendTextMessageAsync(ChatId, message, ParseMode.Html, true,
            //        DateTime.Now.Hour < 9 || DateTime.Now.Hour > 22);
            //    raid.Message = result;
            //}
            //}



            message = $"{message}\n  Ilmoittautuneita";

            if (Attendance.Count == 0)
            {
                message = $"{message}  0";
            }
            else
            {
                var times = new Dictionary<string, int>();
                foreach (var attendee in Attendance)
                {
                    if (attendee.Key == null || attendee.Value == null) continue;

                    if (!times.ContainsKey(attendee.Value))
                    {
                        times[attendee.Value] = 1;
                    }
                    else
                    {
                        times[attendee.Value] = times[attendee.Value] + 1;
                    }
                }
                var sortedTimes = times.Keys.ToList();
                sortedTimes.Sort();
                foreach (var sortedTime in sortedTimes)
                {
                    message = $"{message}  {sortedTime} ({times[sortedTime]})";
                }
            }

            AttendanceIsDirty = false;
            LastAttendanceUpdate = DateTime.Now;
            PendingSend = false;

            return message;
        }
    }
}
