// -----------------------------------------------------------------------
// <copyright file="ZmqBusConfigurationExtensions.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace MassTransit.Transports.ZeroMQ
{
    using System;
    using BusConfigurators;
    using EndpointConfigurators;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public static class ZmqBusConfigurationExtensions
    {

        public static T UseZeroMq<T>(this T configurator)
            where T : EndpointFactoryConfigurator
        {
            configurator.AddTransportFactory<ZeroMqTransportFactory>();

            return configurator;
        }

        /// <summary>
        /// Use MSMQ, and allow the configuration of additional options, such as Multicast or SubscriptionService usage
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="configurator"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static ServiceBusConfigurator UseZeroMq(this ServiceBusConfigurator configurator,
            Action<ServiceBusConfigurator> callback)
        {
            configurator.AddTransportFactory<ZeroMqTransportFactory>();
            callback(configurator);
            return configurator;
        }

    }
}
