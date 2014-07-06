using System;
using System.Diagnostics;
using Language;
using ShortBus.ServiceBusHost;

namespace PointB
{
    class ThisIsSteve :
        BusAwareClass
    {
        public ThisIsSteve(ServiceBusHost host) : base(host)
        {
            Host.Subscribe<AlanNotification>(ListenFor,false);
            Host.Subscribe<FromAlan,SteveNotification>(ProcessMessage,false);
            Publish(ref FireAndForgetRequest);
            Register(ref RequestAndWaitResponse);
        }

        public SteveNotification ProcessMessage(FromAlan input)
        {
            Console.WriteLine("RECEIVED [Response]- > {0}", input.Message);
            return new SteveNotification
                {
                    CorrelationId = input.CorrelationId,
                    Message = Process.GetCurrentProcess().Id.ToString()
                };
        }

        public Func<FromSteve, AlanNotification> RequestAndWaitResponse;
        
        public void ListenFor(AlanNotification input)
        {
            Console.WriteLine("RECEIVED [ListenFor]- > {0}", input.Message);
        }

        public Action<SteveNotification> FireAndForgetRequest;
    }
}
