using System;

namespace ShortBus.ServiceBusHost
{
    public class HostConfiguration
    {
        public readonly HostSettings Settings = new HostSettings();

        public void Host<TIn>(Action<TIn> callback) where TIn : BusAwareClass
        {
            Settings.InstanceFuncs.Add((bus) =>
            {
                var instance = Activator.CreateInstance(typeof(TIn), bus) as TIn;
                callback(instance);
                return instance;
            });
        }

        public void Host(Type type, Action<BusAwareClass> callback)
        {
            Settings.InstanceFuncs.Add((bus) =>
            {
                var instance = Activator.CreateInstance(type, bus) as BusAwareClass;
                callback(instance);
                return instance;
            });
        }

        public void PartOfServiceGroup(string serviceGroupName)
        {
            Settings.ClusterServiceName = serviceGroupName;
        }

        public void SetNetwork(string network)
        {
            Settings.Network = network;
        }

        public void SubscriptionServiceHost(string machineName)
        {
            Settings.SubscriberOnMachine = machineName;
        }
    }
}