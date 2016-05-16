using System;
using System.Collections.Generic;

namespace ShortBus.ServiceBusHost
{
    public class HostSettings
    {
        public string Network;
        public string ClusterServiceName;
        public string SubscriberOnMachine;
        public int NumberOfRetries;
        public TimeSpan? MessageWaitLifespan;
        public Func<ServiceBusHost> HostCreator;

        public List<Func<ServiceBusHost, BusAwareClass>> InstanceFuncs { get; private set; }

        public HostSettings()
        {
            InstanceFuncs = new List<Func<ServiceBusHost, BusAwareClass>>();
        }
    }
}