using System;
using System.Configuration;
using MassTransit;
using MassTransit.Log4NetIntegration;
using MassTransit.Logging;
using MassTransit.Saga;
using MassTransit.Services.Subscriptions.Server;
using MassTransit.Subscriptions.Coordinator;
using MassTransit.Transports.ZeroMQ;

namespace ShortBus.MassTransitHelper.Subscription
{
    public class PersistentSubscriptionService : SubscriptionService
    {
        private static readonly IServiceBus Bus;
        private static readonly ISagaRepository<SubscriptionSaga> SubscriptionSagas;
        private static readonly ISagaRepository<SubscriptionClientSaga> SubscriptionClientSagas;
        private static SubscriptionStorage _storage;
        private static ILog _log;
        private static string server;
        public PersistentSubscriptionService(MassTransit.Logging.ILog log) :
            base(Bus, SubscriptionSagas, SubscriptionClientSagas)
        {
            _log = log;
        }

        public new void Start()
        {
            base.Start();
            _log.Info("Subscription Service Started");
        }

        public new void Stop()
        {
            base.Stop();
            _log.Info("Subscription Service Started");
        }

        static PersistentSubscriptionService()
        {
            server = ConfigurationManager.AppSettings["ServerName"];
            Bus = ServiceBusFactory.New(sbc =>
                {
                    sbc.ReceiveFrom("tcp://Current:50000");
                    sbc.UseSubscriptionStorage(SubscriptionStorageFactory);
                    sbc.SetConcurrentConsumerLimit(1);
                    sbc.UseZeroMq();
                    sbc.SetPurgeOnStartup(true);
                    sbc.UseLog4Net();
                });

            SubscriptionClientSagas = new MongoSagaStorage<SubscriptionClientSaga>();
            SubscriptionSagas = new MongoSagaStorage<SubscriptionSaga>();
        }

        private static SubscriptionStorage SubscriptionStorageFactory()
        {
            return _storage ?? (_storage = new MongoSubscriptionStorage(server));
        }
    }
}