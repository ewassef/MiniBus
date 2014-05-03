using log4net;

namespace MassTransit.Transports.ZeroMQ
{
    using System;
    using System.Collections.Concurrent;

    public class ZeroMqTransportFactory :
        ITransportFactory
    {
        private readonly IMessageNameFormatter _messageNameFormatter;
        private readonly ConcurrentDictionary<Uri, ZeroMqReceiveTransport> _inboundTransports;
        private readonly ConcurrentDictionary<Uri, Transport> _loopBackTransports;
        private readonly ConcurrentDictionary<Uri, ZeroMqSendTransport> _outboundTransports;
        private static readonly ILog Log = LogManager.GetLogger(typeof(ZeroMqTransportFactory));
        

        public ZeroMqTransportFactory()
        {
            _messageNameFormatter = new DefaultMessageNameFormatter("::", "--", ":", "-");
            _inboundTransports = new ConcurrentDictionary<Uri, ZeroMqReceiveTransport>();
            _loopBackTransports = new ConcurrentDictionary<Uri, Transport>();
            _outboundTransports = new ConcurrentDictionary<Uri, ZeroMqSendTransport>();
        }

        public void Dispose()
        {
        }

        public string Scheme { get { return "tcp"; } }

        public IDuplexTransport BuildLoopback(ITransportSettings settings)
        {
            Transport loopBack;
            Log.DebugFormat("Attempting a duplex transport for {0}", settings.Address.Uri);
            if (!_loopBackTransports.TryGetValue(settings.Address.Uri, out loopBack))
            {
                Log.DebugFormat("Not in cache for {0}", settings.Address.Uri);
                var address = new ZeroMqAddress(settings.Address.Uri);
                var tSettings = new TransportSettings(address);
                if (!address.IsLocal)
                {
                    Log.DebugFormat("Address {0} is remote. Creating Empty recieve and full send", address.Uri);

                    var outbound = BuildOutbound(tSettings);
                    //Then we dont need a recieve from here...right?
                    loopBack = new Transport(settings.Address, ZeroMqReceiveTransport.EmptyTransport, () => outbound);
                }
                else
                {
                    Log.DebugFormat("Address {0} is local. Creating Empty send and full recieve", address.Uri);

                    var inbound = BuildInbound(tSettings);
                    //var outbound = BuildOutbound(tSettings);
                    //Because we swap out the port on the control, we need to return the uri on the transport
                    loopBack = new Transport(inbound.Address, () => inbound, ()=>new ZeroMqLoopBackTransport(inbound as ZeroMqReceiveTransport));
                }
                _loopBackTransports[settings.Address.Uri] = loopBack;
                Log.DebugFormat("Adding {0} to loopback cache", settings.Address.Uri);
            }
            Log.DebugFormat("Duplex Transport returned with address {0}", loopBack.Address);
            return loopBack; 
        } 

        public IInboundTransport BuildInbound(ITransportSettings settings)
        {
            ZeroMqReceiveTransport inbound;
            Log.DebugFormat("Building inbound transport for {0}", settings.Address.Uri);
            if (!_inboundTransports.TryGetValue(settings.Address.Uri, out inbound))
            {
                Log.DebugFormat("Didnt find {0} in inbound cache", settings.Address.Uri);
                inbound = new ZeroMqReceiveTransport(settings.Address.Uri);
                _inboundTransports[inbound.Address.Uri] = inbound;
            }
            Log.DebugFormat("Inbound Transport returned with address {0}", inbound.Address);
            return inbound;
        }

        public IOutboundTransport BuildOutbound(ITransportSettings settings)
        {
            ZeroMqSendTransport outbound;
            Log.DebugFormat("Building outbound transport for {0}", settings.Address.Uri);
            if (!_outboundTransports.TryGetValue(settings.Address.Uri, out outbound))
            {
                Log.DebugFormat("Didint find {0} in outbound cache", settings.Address.Uri);
                outbound = new ZeroMqSendTransport(settings.Address.Uri);
                _outboundTransports[settings.Address.Uri] = outbound;
            }
            Log.DebugFormat("Outbound Transport returned with address {0}", outbound.Address);
            return outbound;
        }

        public IOutboundTransport BuildError(ITransportSettings settings)
        {
            Log.DebugFormat("Building empty error send transport {0}", settings.Address.Uri);
            return ZeroMqSendTransport.EmptyTransport();
        }

        public IMessageNameFormatter MessageNameFormatter { get { return _messageNameFormatter; } }

        public IEndpointAddress GetAddress(Uri uri, bool transactional)
        {
            return new EndpointAddress(uri);
        }

        
    }
}