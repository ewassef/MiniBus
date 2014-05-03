using System;
using MassTransit;
using MassTransit.Log4NetIntegration;
using MassTransit.Logging;
using MassTransit.Saga;
using MassTransit.Services.Subscriptions.Server;
using MassTransit.Subscriptions.Coordinator;
using MassTransit.Transports.ZeroMQ;

namespace ShortBus.MassTransitHelper.Subscription
{
    public class InMemorySubscriptionService: SubscriptionService
    {
        private static readonly IServiceBus Bus;
        private static readonly ISagaRepository<SubscriptionSaga> SubscriptionSagas;
        private static readonly ISagaRepository<SubscriptionClientSaga> SubscriptionClientSagas;
        private static SubscriptionStorage _storage;
        private static ILog _log;
        public InMemorySubscriptionService(ILog log)
            : base(Bus, SubscriptionSagas, SubscriptionClientSagas)
        {
            _log = log;
        }
        static InMemorySubscriptionService()
        {
            Bus = ServiceBusFactory.New(sbc =>
                {
                    ZeroMqAddress.RegisterLocalPort(50000);
                    sbc.ReceiveFrom(string.Format("tcp://{0}:50000",Environment.MachineName));
                    sbc.UseSubscriptionStorage(SubscriptionStorageFactory);
                    sbc.SetConcurrentConsumerLimit(1);
                    sbc.UseControlBus();
                    sbc.UseZeroMq();
                    sbc.UseBsonSerializer();
                    sbc.UseLog4Net();
                });

            SubscriptionClientSagas = new InMemorySagaRepository<SubscriptionClientSaga>();
            SubscriptionSagas = new InMemorySagaRepository<SubscriptionSaga>();
        }
        private static SubscriptionStorage SubscriptionStorageFactory()
        {
            return _storage ?? (_storage = new InMemorySubscriptionStorage());
        }
    }
}