using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.Extensions.Configuration;


namespace TGRaidBot
{
    class Program
    {
        private static RaidBotConfig _raidBotConfig;

        static void Main(string[] args)
        {
            Console.WriteLine("Starting Telegram Raid Bot v1.1.1");

            //IConfiguration config = new ConfigurationBuilder()
            //    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            //    .Build();

            //_raidBotConfig = new RaidBotConfig();
            //_raidBotConfig.Services.Add(new TelegramService());
            var serializer = new XmlSerializer(typeof(RaidBotConfig));
            using (var reader = new StreamReader("RaidBotConfig.xml"))
            {
                _raidBotConfig = serializer.Deserialize(reader) as RaidBotConfig;
            }

            CancellationTokenSource tokenSource = new CancellationTokenSource();

            var kaluListener = new RaidiKaluListener();
            kaluListener.Start(tokenSource.Token);

            foreach (var service in _raidBotConfig.Services)
            {
                service.GymListChanged += ServiceOnGymListChanged;
                if (service is ISender sender)
                {
                    kaluListener.DataReceived += sender.OnMessageReceived;
                }
            }
            

            var input = Console.ReadLine();
            while (input == null || !input.StartsWith("q"))
            {
                if (input != null && input.StartsWith("s"))
                {
                    Console.WriteLine(kaluListener.GetStatus());
                    foreach (var service in _raidBotConfig.Services)
                    {
                        if (service is ISender sender)
                        {
                            Console.WriteLine(sender.GetStatus());
                        }
                    }
                }
                input = Console.ReadLine();
            }
            tokenSource.Cancel();
            foreach (var service in _raidBotConfig.Services)
            {
                if (service is ISender sender)
                {
                    sender.Save();
                }
            }
            while (!kaluListener.IsFinished)
            {
                Task.Delay(100).Wait();
            }
        }

        private static void ServiceOnGymListChanged(object sender, EventArgs e)
        {
            using (var writer = new StreamWriter("RaidBotConfig.xml"))
            {
                var serializer = new XmlSerializer(typeof(RaidBotConfig));
                serializer.Serialize(writer, _raidBotConfig);
            }
        }
    }

    
}
