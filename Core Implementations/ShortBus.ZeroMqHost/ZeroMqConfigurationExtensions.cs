using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShortBus.ServiceBusHost;

namespace ShortBus.ZeroMqHost
{
    public static class ZeroMqConfigurationExtensions
    {
        public static void UseZeroMq(this HostConfiguration configuration)
        {
            configuration.Settings.HostCreator = () => new ZeroMqTransitHost();
        }
    }
}
