using System;
using System.Configuration;
using System.Diagnostics;
using MassTransit.Logging;
using Topshelf;
using ShortBus.MassTransitHelper.Subscription;
using log4net.Config;


namespace SubscriptionHoster
{
    internal static class Program
    {
        private static readonly ILog _log = Logger.Get("SubscriptionHoster");
        [STAThread]
        static void Main()
        {
            XmlConfigurator.Configure();

            var sss = HostFactory.New(x =>
                {
                    x.RunAsLocalService();

                    x.SetDescription("Volatile Subscription service for the handlers on the bus");
                    x.SetDisplayName("Volatile Subscription Service");
                    x.SetServiceName("VolatileSubscriptionService");
                    DisplayStateMachine();
                    var onDisk = true;
                    var server = ConfigurationManager.AppSettings["ServerName"];
                    if (
                        bool.TryParse(ConfigurationManager.AppSettings["PersistSubscription"], out onDisk)
                        && onDisk
                        && !string.IsNullOrWhiteSpace(server))
                    {
                        x.Service<PersistentSubscriptionService>(s =>
                            {
                                s.ConstructUsing(f => new PersistentSubscriptionService(_log));
                                s.WhenStarted(f => f.Start()); 
                                s.WhenStopped(f => f.Dispose());
                            });
                    }
                    else
                    {
                        {
                            x.Service<InMemorySubscriptionService>(s =>
                        {
                            s.ConstructUsing(f => new InMemorySubscriptionService(_log));
                            s.WhenStarted(f => f.Start());
                            s.WhenStopped(f =>
                            {
                                f.Stop();
                                f.Dispose();
                            });
                        });
                        }
                    }
                });
            sss.Run();


        }
        static void DisplayStateMachine()
        {
            Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
        }
    }
}
