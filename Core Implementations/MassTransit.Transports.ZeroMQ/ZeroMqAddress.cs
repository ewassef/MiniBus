using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using log4net;

namespace MassTransit.Transports.ZeroMQ
{
    using System;
    using System.Diagnostics;

    [DebuggerDisplay("ZMQ: {RebuiltUri}")]
    public class ZeroMqAddress : IEndpointAddress
    { 
        readonly Uri _rawUri;
        readonly Uri _rebuiltUri;
        private static ConcurrentBag<int> _localPorts = new ConcurrentBag<int>();
        private static readonly ILog Log = LogManager.GetLogger(typeof(ZeroMqTransportFactory));

        public static void RegisterLocalPort(int port)
        {
            Log.InfoFormat("Registering {0} as a local port. We now have {1} ports local to the process",port,_localPorts.Count+1);
            _localPorts.Add(port);
        }

        public ZeroMqAddress(Uri uri)
        {
            _rawUri = uri;
            Log.InfoFormat("Creating ZMQ address from {0}",uri.ToString());
            Port = uri.Port;
            Host = uri.Host;
            Path = uri.Segments.Length>1?uri.Segments[1]:string.Empty;

            IsTransactional = false;
            if (_localPorts.Contains(Port))
                if (Host.Equals("Current", StringComparison.CurrentCultureIgnoreCase) || Host.Equals(Environment.MachineName, StringComparison.CurrentCultureIgnoreCase))
                {
                    IsLocal = true;
                    Host = Environment.MachineName;
                } 
            UriBuilder builder;

            if (IsLocal && uri.Segments.Any(x => x.Equals("_control", StringComparison.CurrentCultureIgnoreCase)))
            {
                var nPort = FindFreePort();
                RegisterLocalPort(nPort);
                builder = new UriBuilder("tcp", Host, nPort, Path);
            }
            else
            {
                builder = new UriBuilder("tcp", Host, Port, Path);
            }

            _rebuiltUri = builder.Uri;
        }

        public string Host { get; set; }
        public int Port { get; set; }
        public string Path { get; set; }
        public Uri RawUri
        {
            get { return _rawUri; }
        }
        public Uri RebuiltUri
        {
            get { return _rebuiltUri; }
        }

        public Uri GetConnectionUri()
        {
            return RebuiltUri;
        }

        #region object overrides
        public bool Equals(ZeroMqAddress other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Equals(other._rebuiltUri, _rebuiltUri);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != typeof(ZeroMqAddress))
                return false;
            return Equals((ZeroMqAddress)obj);
        }

        public override int GetHashCode()
        {
            return (_rebuiltUri != null ? _rebuiltUri.GetHashCode() : 0);
        }
        #endregion

        public Uri Uri { get { return _rebuiltUri; } }
        public bool IsLocal { get; private set; }
        public bool IsTransactional { get; private set; }

        private int FindFreePort()
        {
            var randomizer = new Random(DateTime.Now.Millisecond);
            var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            var tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();
            //try up to 1000 ports
            var attempts = 0;
            while (attempts < 1000)
            {
                var port = randomizer.Next(10000, 20000);
                if (tcpConnInfoArray.All(x => x.LocalEndPoint.Port != port))
                    return port;
                attempts++;
            }
            throw new SystemException("Couldnt find a free port to use");
        }
    }
}
