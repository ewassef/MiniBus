using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Language;
using ShortBus.ServiceBusHost;
using ShortBus.ZeroMqHost;
using log4net.Config;

namespace PointB
{
    class Program
    {
        private static ThisIsSteve steve;
        static void Main(string[] args)
        {
            XmlConfigurator.Configure();
            steve = new ThisIsSteve();
            var bus = ServiceBusHost.Create(configuration =>
            {
                configuration.Host(steve);
                configuration.SubscriptionServiceHost(Environment.MachineName);
                configuration.UseZeroMq();
            });
            Console.WriteLine("{0}... click any key to start ...", Process.GetCurrentProcess().Id.ToString());
            var key = Console.ReadKey();
            while (key.KeyChar != 'q')
            {
                CallAlan();
                key = Console.ReadKey();  
            }
            bus.Cleanup();
            bus.Dispose();
        }

        static void CallAlan()
        {
            Console.WriteLine("Sending ...");
            steve.FireAndForgetRequest(new SteveNotification
                {
                    Message = Process.GetCurrentProcess().Id.ToString(),
                    
                });
            //Console.WriteLine("Sending ...");
            //steve.FireAndForgetRequest(new SteveNotification
            //{
            //    Message = Process.GetCurrentProcess().Id.ToString(),

            //});
            //Console.WriteLine("Sending ...");
            //steve.FireAndForgetRequest(new SteveNotification
            //{
            //    Message = Process.GetCurrentProcess().Id.ToString(),

            //});
            //Console.WriteLine("Sending ...");
            //var res = steve.RequestAndWaitResponse(new FromSteve()
            //    {
            //        CorrelationId = Guid.NewGuid(),
            //        Response = Process.GetCurrentProcess().Id.ToString()
            //    });
            //Console.WriteLine(res != null ? res.Message : "No response");
        }
    }
}
