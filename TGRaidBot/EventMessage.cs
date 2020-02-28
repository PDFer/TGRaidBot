using System;
using System.Collections.Generic;
using System.Text;

namespace TGRaidBot
{
    public class EventMessage
    {
        public enum EventType
        {
            Raid,
            Attendance
        };

        public EventType Event { get; set; }

        public string Message { get; set; }

        public EventData Data { get; set; }

        public EventMessage()
        {

        }
    }
}
