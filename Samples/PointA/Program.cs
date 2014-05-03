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

namespace PointA
{
    class Program
    {
        private static ThisIsAlan alan;

        static void Main(string[] args)
        {
            XmlConfigurator.Configure();
            alan = new ThisIsAlan();
            var bus = ServiceBusHost.Create(configuration =>
                {
                    configuration.Host(alan);
                    configuration.SubscriptionServiceHost(Environment.MachineName);
                    configuration.UseZeroMq();
                });
            Console.WriteLine("{0}... click any key to start calling ...", Process.GetCurrentProcess().Id.ToString());
            var key = Console.ReadKey();
            while (key.KeyChar != 'q')
            {
                CallSteve();
                key = Console.ReadKey();
            }
            bus.Cleanup();
            bus.Dispose();
        }

        private static int counter;
        static void CallSteve()
        {
            for (int i = 0; i < 1; i++)
            {
                Console.WriteLine("Sending ...");
                alan.FireAndForgetRequest(new AlanNotification
                {
                    Message = string.Format("{0} > {1}", Process.GetCurrentProcess().Id.ToString(), counter++),
                });
            //    Console.WriteLine("Sending ...");
            //    alan.FireAndForgetRequest(new AlanNotification
            //    {
            //        Message = string.Format("{0} > {1}", Process.GetCurrentProcess().Id.ToString(), counter++),
            //    });
            //    Console.WriteLine("Sending ...");
            //    alan.FireAndForgetRequest(new AlanNotification
            //    {
            //        Message = string.Format("{0} > {1}", Process.GetCurrentProcess().Id.ToString(), counter++),
            //    });
                
            }
                //Console.WriteLine("Sending ...");
            //    var res = alan.RequestAndWaitResponse(new FromAlan
            //    {
            //        CorrelationId = Guid.NewGuid(),
            //        Message = string.Format("{0} > {1}", Process.GetCurrentProcess().Id.ToString(), counter++),
            //    });
            //    Console.WriteLine(res != null ? res.Message : "No response"); 
        }
    }
}
