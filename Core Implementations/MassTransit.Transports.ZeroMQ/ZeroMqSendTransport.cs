using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Linq;
using Newtonsoft.Json;
using log4net;

namespace MassTransit.Transports.ZeroMQ
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Threading.Tasks;
    using ZMQ;
    using Exception = ZMQ.Exception;


    public class ZeroMqSendTransport :
        IOutboundTransport
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(ZeroMqSendTransport));

        readonly Context _zmqContext;
        readonly Socket _zmqSocket;
        private readonly Uri _address;
        bool _needsToConnect = true;
        readonly BlockingCollection<byte[]> _queue;
        bool _running = false;
        readonly Task _sending;

        public ZeroMqSendTransport(Uri address)
        {
            _address = address;
            _zmqContext = new Context(4);
            _zmqSocket = _zmqContext.Socket(SocketType.DEALER);
            _zmqSocket.SndBuf = 10 * 1024 * 1024;
            _queue = new BlockingCollection<byte[]>();
            _running = true;
            _sending = Task.Factory.StartNew(SendBytes);
            Log.Info(string.Format("Dealer socket created, still not connected"));
        }

        private ZeroMqSendTransport(bool empty)
        {
            Log.Info("Creating an Empty Send Socket...");
            _address = new Uri("null://null");
        }

        public static ZeroMqSendTransport EmptyTransport()
        {
            return new ZeroMqSendTransport(true);
        }

        void SendBytes()
        {
            while (_running)
            {
                try
                {
                    var bites = _queue.Take();
                    _zmqSocket.Send(bites);
                    Log.DebugFormat("Messge SENT to {0}", Address);
                }
                catch (Exception exception)
                {
                    if (_running)
                        Log.Error(exception.Message, exception);
                }
            }
        }

        public void Dispose()
        {
            Log.InfoFormat("Shutting down the dealer socket");
            _running = false;
            if (_sending != null)
                _sending.Wait(TimeSpan.FromSeconds(10));
            if (_zmqSocket != null)
                _zmqSocket.Dispose();
            if (_zmqContext != null)
                _zmqContext.Dispose();
        }

        public IEndpointAddress Address
        {
            get { return new EndpointAddress(_address); }
        }

        public void Send(ISendContext context)
        {
            if (!_running)
            {
                Log.Debug("Calling the Empty Send Socket. Returning");
                return;
            }

            var msg = new ZmqMessage();
            using (var mem = new MemoryStream())
            {
                context.SerializeTo(mem);
                msg.Body = mem.ToArray();
            }
            msg.ContentType = context.ContentType;
            msg.OriginalMessageId = context.OriginalMessageId;
            if (_needsToConnect)
                lock (_zmqSocket)
                {
                    if (_needsToConnect)
                    {
                        var uri = new UriBuilder(_address.Scheme, _address.Host, _address.Port);
                        _zmqSocket.Connect(uri.Uri.ToString().TrimEnd('/'));
                        Log.InfoFormat("Socket connected to address {0} for SENDING", context.DestinationAddress);
                    }
                    _needsToConnect = false;
                }
            if (Log.IsDebugEnabled)
            {
                var stream = new MemoryStream(msg.Body);
                string msgContents;
                if (msg.ContentType.Contains("xml"))
                    msgContents = XDocument.Load(stream).ToString();
                else
                {
                    using (var x = new Newtonsoft.Json.Bson.BsonReader(new BinaryReader(new MemoryStream(msg.Body))))
                    {
                        var obj = JsonSerializer.Create().Deserialize(x);
                        msgContents = JsonConvert.SerializeObject(obj, Formatting.Indented);
                    }
                }

                Log.DebugFormat("Msg=>{0}{1}", Environment.NewLine, msgContents);
            }
            _queue.Add(msg.Serialize());
            Log.DebugFormat("Messge queued for sending to {0}", context.DestinationAddress);
        }
    }

    public class ZeroMqLoopBackTransport : IOutboundTransport
    {
        private ZeroMqReceiveTransport _inbound;
        public ZeroMqLoopBackTransport(ZeroMqReceiveTransport inbound)
        {
            _inbound = inbound;
        }

        public void Dispose()
        {

        }

        public IEndpointAddress Address { get; private set; }
        public void Send(ISendContext context)
        {
            var msg = new ZmqMessage();
            using (var mem = new MemoryStream())
            {
                context.SerializeTo(mem);
                msg.Body = mem.ToArray();
            }
            msg.ContentType = context.ContentType;
            msg.OriginalMessageId = context.OriginalMessageId;
            _inbound.Inject(msg.Serialize());
        }
    }
}