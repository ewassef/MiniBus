using ShortBus.ServiceBusHost;

namespace ShortBus.MassTransitHost
{
    public static class MassTransitConfigurationExtensions
    {
        public static void UseMassTransit(this HostConfiguration configuration) 
        {
            configuration.Settings.HostCreator = () => new MassTransitHost();
        }
    }
}