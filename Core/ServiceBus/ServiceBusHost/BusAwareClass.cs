using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortBus.ServiceBusHost
{
    public abstract class BusAwareClass
    {
        private ServiceBusHost _host;
        
        protected BusAwareClass(ServiceBusHost host)
        {
            _host = host;
        }

        public ServiceBusHost Host { get { return _host; } }
    }
}
