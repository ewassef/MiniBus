using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Xml.Linq;
using Newtonsoft.Json;
using log4net;
using NetMQ;
using NetMQ.Sockets;

namespace MassTransit.Transports.ZeroMQ
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Threading.Tasks;
    using Context; 

    public class ZeroMqReceiveTransport : IInboundTransport
    {

        static readonly ILog Log = LogManager.GetLogger(typeof(ZeroMqReceiveTransport));
        readonly NetMQContext _zmqContext;
        readonly RouterSocket _zmqSocket;
        readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();
        readonly int _socket = 10000;
        private readonly Uri _address;
        BlockingCollection<byte[]> queue;
        bool running;
        Task _task;
        public void Dispose()
        {
            running = false;
            _tokenSource.Cancel();
            _task.Wait(TimeSpan.FromSeconds(1));
            if (_zmqSocket != null)
            {
                Log.InfoFormat("Shutting down the router socket");
                _zmqSocket.Dispose();
            }
            if (_zmqContext != null)
            {
                Log.InfoFormat("Shutting down the context");
                _zmqContext.Dispose();
            }
        }

        
        public ZeroMqReceiveTransport(Uri address)
        {
            _zmqContext = NetMQContext.Create();
            _zmqSocket = _zmqContext.CreateRouterSocket();
            queue = new BlockingCollection<byte[]>();
            _address = address;
            _socket = address.Port;
            try
            {
                _zmqSocket.Bind(string.Format("tcp://*:{0}", _socket));
                _zmqSocket.Options.ReceiveBuffer = 10 * 1024 * 1024;
                running = true;
                _task = Task.Factory.StartNew(Read, _tokenSource.Token);
                Log.InfoFormat("Creating a new ROUTER Socket and bound to {0}. Starting to listen", _socket);
            }
            catch (Exception e)
            {
                Log.Error(e.Message, e);
            }

        }
        private ZeroMqReceiveTransport(bool empty)
        {
            Log.Info("Creating an Empty Recieve Socket");
        }
        public static ZeroMqReceiveTransport EmptyTransport()
        {
            return new ZeroMqReceiveTransport(true);
        }
         
        //public IEndpointAddress Address { get { return new EndpointAddress(_zmqSocket.Address.Replace("*", Environment.MachineName)); } }
        public IEndpointAddress Address { get { return new EndpointAddress(_address); } }
        private void Read()
        {
            while (running)
            {
                try
                {
                    byte[] bites;
                    var success = _zmqSocket.TryReceiveFrameBytes(TimeSpan.FromMilliseconds(250),out bites);
                    if (success && bites != null && bites.Length > 17)
                    {
                        queue.Add(bites);
                        Log.DebugFormat("Received a message {0} bytes", bites.Length);
                    }
                }
                catch (System.Exception exception)
                {
                    if (running)
                        Log.Error(exception.Message, exception);
                }
            }
        }
        public void Receive(Func<IReceiveContext, Action<IReceiveContext>> lookupSinkChain, TimeSpan timeout)
        {
            byte[] bites = null;
            if (queue == null || !queue.TryTake(out bites, timeout)) 
                return;
            try
            {
                if (bites != null)
                {
                    var msg = ZmqMessage.Deserialize(bites);
                    if (msg == null)
                        return;
                    Log.DebugFormat("Message deserialized successfully");
                    IReceiveContext context = ReceiveContext.FromBodyStream(new MemoryStream(msg.Body));
                    context.SetMessageId(msg.OriginalMessageId);
                    context.SetContentType(msg.ContentType);
                    context.SetInputAddress(Address);

                    if (Log.IsDebugEnabled)
                    {
                        try
                        {
                            var stream = new MemoryStream(msg.Body);
                            string msgContents;
                            if (msg.ContentType.Contains("xml"))
                                msgContents = XDocument.Load(stream).ToString();
                            else
                            {
                                using (var x = new Newtonsoft.Json.Bson.BsonReader(new BinaryReader(new MemoryStream(msg.Body))))
                                {
                                    var obj = JsonSerializer.Create(new JsonSerializerSettings()).Deserialize(x);
                                    msgContents = JsonConvert.SerializeObject(obj, Formatting.Indented);
                                }
                            }
                            Log.DebugFormat("Msg=>{0}{1}", Environment.NewLine, msgContents);
                        }
                        catch
                        {
                        
                        }
                    }

                    Action<IReceiveContext> receive = lookupSinkChain(context);
                    if (receive == null)
                    {
                        Log.InfoFormat("No recieve context found in the lookup chain. Msg came in as {0}", context.MessageType);
                        return;
                    }
                    receive(context);
                }
            }
            catch (System.Exception e)
            {
                throw;
            }
        }

        public void Inject(byte[] bites)
        {
            queue.Add(bites);
        }
    }
}