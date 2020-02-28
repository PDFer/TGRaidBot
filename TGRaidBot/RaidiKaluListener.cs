using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;

namespace TGRaidBot
{
    public class DataEventArgs : EventArgs
    {
        public EventMessage Message { get; set; }
    }


    public class RaidiKaluListener
    {
        public bool IsFinished { get; private set; }

        public event EventHandler<DataEventArgs> DataReceived;

        public int MessagesReceived { get; private set; }
        public DateTime StartTime { get; private set; }

        private string Message { get; set; }

        private ClientWebSocket RaidiKaluSocket { get; set;  } = new ClientWebSocket();

        private Logger logger => NLog.LogManager.GetCurrentClassLogger();

        public RaidiKaluListener()
        {
            
        }

        public async void Start(CancellationToken token)
        {
            logger.Info("Starting Raidikalu reader");
            RaidiKaluSocket.Options.KeepAliveInterval = new TimeSpan(0, 0, 10);
            await RaidiKaluSocket.ConnectAsync(new Uri("wss://raidikalu.herokuapp.com/ws/"), token);

            if (RaidiKaluSocket.State == WebSocketState.Open)
            {
                logger.Info("Connected successfully!");
                //Console.WriteLine("Connected successfully!");
                StartTime = DateTime.Now;
            }
            else
            {
                logger.Error("Connection failed!");
                //Console.WriteLine("Connection failed!");
                return;
            }
            

            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (RaidiKaluSocket.State != WebSocketState.Open)
                    {
                        logger.Info("Connecting to Websocket..");
                        //Console.WriteLine("Connecting to new Websocket..");
                        RaidiKaluSocket = new ClientWebSocket();
                        RaidiKaluSocket.Options.KeepAliveInterval = new TimeSpan(0, 0, 10);
                        await RaidiKaluSocket.ConnectAsync(new Uri("wss://raidikalu.herokuapp.com/ws/"), token);
                        if (RaidiKaluSocket.State == WebSocketState.Open)
                        {
                            logger.Info("Connected successfully!");
                            //Console.WriteLine("Connected successfully!");
                            StartTime = DateTime.Now;
                        }
                        else
                        {
                            logger.Error("Connection failed!");
                            //Console.WriteLine("Connection failed!");
                            return;
                        }
                    }

                    await ReadData(token).ContinueWith((readTask) =>
                    {
                        if (readTask.IsCanceled) return;

                        if (readTask.IsFaulted)
                        {
                            logger.Error(readTask.Exception, $"Error reading data: {readTask.Exception.Message}");
                            //Console.WriteLine($"Error reading data: {readTask.Exception.Message}");
                            RaidiKaluSocket.Abort();
                        }
                    });
                }
                catch (Exception e)
                {
                    logger.Error(e);
                }
            }
            IsFinished = true;

        }

        public string GetStatus()
        {
            return
                $"Connection to Raidikalu is {RaidiKaluSocket.State}. Messages received {MessagesReceived} since {StartTime}.";
        }


        private async Task ReadData(CancellationToken token)
        {
            logger.Info("Reading from raidikalu..");
            ArraySegment<Byte> receiveBuffer = new ArraySegment<byte>(new byte[1000]);
            try
            {
                var result = await RaidiKaluSocket.ReceiveAsync(receiveBuffer, token);
                
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    Message += System.Text.Encoding.UTF8.GetString(receiveBuffer.Array, 0, result.Count);
                }

                if (result.EndOfMessage)
                {
                    MessagesReceived += 1;

                    EventMessage eventMessage = null;
                    try
                    {
                        eventMessage = JsonConvert.DeserializeObject<EventMessage>(Message, new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Ignore });
                    }
                    catch (Exception e)
                    {
                        logger.Error(e, "Error parsing message");
                        //Console.WriteLine($"Error parsing message: {e.Message}");
                    }
                    logger.Debug(Message);
                    logger.Info($"Received: {eventMessage.Event} : {eventMessage.Message}");
                    var args = new DataEventArgs { Message = eventMessage };
                    DataReceived?.Invoke(this, args);
                    Message = String.Empty;
                }
            }
            catch (OperationCanceledException)
            {
                logger.Info("Read was cancelled by user.");
                //Console.WriteLine("Read was cancelled");
            }
            catch (Exception e)
            {
                logger.Error($"Error reading data: {e.Message}");
                //Console.WriteLine($"Error reading data: {e.Message}");
                RaidiKaluSocket.Abort();
            }
        }
    }
}
