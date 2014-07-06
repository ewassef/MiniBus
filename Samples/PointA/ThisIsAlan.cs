using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Language;
using ShortBus.ServiceBusHost;
using ShortBus.Hostable.Shared.Interface;

namespace PointA
{
    public class ThisIsAlan :
        BusAwareClass
    {

        public ThisIsAlan(ServiceBusHost host):base(host)
        {
            Publish(ref FireAndForgetRequest);
            Register(ref RequestAndWaitResponse);
            
            Host.Subscribe<SteveNotification>(ListenFor, false);
            Host.Subscribe<FromSteve, AlanNotification>(ProcessMessage, false);
        }

        public Func<FromAlan, SteveNotification> RequestAndWaitResponse;

        public AlanNotification ProcessMessage(FromSteve input)
        {
            Console.WriteLine("RECEIVED [Respond] - > {0}", input.Response);
            return new AlanNotification
                {
                    CorrelationId = input.CorrelationId,
                    Message = Process.GetCurrentProcess().Id.ToString()
                };
        }

        public void ListenFor(SteveNotification input)
        {
            Console.WriteLine("RECEIVED [ListenFor]- > {0}", input.Message);
        }

        public Action<AlanNotification> FireAndForgetRequest;
    }
}
