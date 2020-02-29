using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TGRaidBot;

namespace TGRaidBotTests
{
    [TestClass]
    public class TelegramSenderTests
    {
        [TestMethod]
        public void ParseMessage_WithRaid_Success()
        {
            RaidBotConfig services;
            var serializer = new XmlSerializer(typeof(RaidBotConfig));
            using (var reader = new StreamReader("RaidBotConfig.xml"))
            {
                services = serializer.Deserialize(reader) as RaidBotConfig;
            }
            //TimeSpan serverOffset = TimeZoneInfo.Local.GetUtcOffset(DateTimeOffset.Now);

            Task.Delay(2000).Wait();

            EventMessage message = new EventMessage
            {
                Data = new EventData
                {
                    Gym = "testi",
                    Start = (int)DateTime.UtcNow.AddHours(1).Subtract(DateTime.UnixEpoch).TotalSeconds,
                    End = (int)DateTime.UtcNow.AddHours(2).Subtract(DateTime.UnixEpoch).TotalSeconds,
                    Pokemon = "Hippi�inen",
                    Tier = 4
                },
                Event = EventMessage.EventType.Raid
            };

            foreach(var service in services.Services)
            {
                service.OnMessageReceived(this, new DataEventArgs { Message = message });
            }

            Task.Delay(3000);

            //var raidTestMessage =
            //    $"{{ \"event\": \"raid\", \"message\": \"Raidi 91196 p�ivitetty\", \"data\": {{ \"raid\": 91187, \"gym\": \"Keinupuisto\", \"pokemon\": \"Regice\", \"tier\": 5, \"lat\": \"61.493526\", \"lng\": \"23.788385\", \"start\": {new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds()}, \"end\": {new DateTimeOffset(DateTime.Now + TimeSpan.FromMinutes(45)).ToUnixTimeSeconds()}, \"created\": false}} }}";

            //var attendanceTestMessage =
            //    "{\"event\": \"attendance\", \"message\": \"Anonyymi 5177 tulee raidille 16:40\", \"data\": {\"raid\": 91187, \"choice\": 3, \"time\": \"16:40\", \"submitter\": \"Anonyymi 5177\"}}";

            //var attendanceTestMessage2 =
            //    "{\"event\": \"attendance\", \"message\": \"Kerpele tulee raidille 16:50\", \"data\": {\"raid\": 91187, \"choice\": 3, \"time\": \"16:40\", \"submitter\": \"Kerpele\"}}";

            //var attendanceTestMessage3 =
            //    "{\"event\": \"attendance\", \"message\": \"heps tulee raidille 16:50\", \"data\": {\"raid\": 91187, \"choice\": 3, \"time\": \"16:50\", \"submitter\": \"heps\"}}";

            //var attendanceTestMessage4 =
            //    "{\"event\": \"attendance\", \"message\": \"heps tulee raidille 16:50\", \"data\": {\"raid\": 91187, \"choice\": 0, \"time\": \"\", \"submitter\": \"Kerpele\"}}";

            //var attendanceTestMessage5 =
            //    "{\"event\": \"attendance\", \"message\": \"heps tulee raidille 16:50\", \"data\": {\"raid\": 91187, \"choice\": 3, \"time\": \"\", \"submitter\": \"heps\"}}";

            //var attendanceTestMessage6 =
            //    "{\"event\": \"attendance\", \"message\": \"Anonyymi 5177 tulee raidille 16:40\", \"data\": {\"raid\": 91187, \"choice\": 3, \"time\": \"\", \"submitter\": \"Anonyymi 5177\"}}";


            //var tgTest = new TelegramService();
            //tgTest.ParseMessage(raidTestMessage);
            //Thread.Sleep(2000);
            //tgTest.ParseMessage(attendanceTestMessage);
            //Thread.Sleep(2000);
            //tgTest.ParseMessage(attendanceTestMessage2);
            //Thread.Sleep(2000);
            //tgTest.ParseMessage(attendanceTestMessage3);
            //Thread.Sleep(2000);
            //tgTest.ParseMessage(attendanceTestMessage4);
            //Thread.Sleep(2000);
            //tgTest.ParseMessage(attendanceTestMessage5);
            //Thread.Sleep(2000);
            //tgTest.ParseMessage(attendanceTestMessage6);

            //Thread.Sleep(5000);

        }
    }
}
