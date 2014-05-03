using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Language;
using ShortBus.Hostable.Shared.Interface;
using ShortBus.ServiceBusHost;

namespace PointB
{
    class ThisIsSteve :
        MakeRequests,
        INeedProcessed<FromSteve,AlanNotification>,
        IHandleMessage<FromAlan, SteveNotification>,
        IListen<AlanNotification>,
        IFireAndForgetRequest<SteveNotification>
    {
        public SteveNotification ProcessMessage(FromAlan input)
        {
            Console.WriteLine("RECEIVED [Response]- > {0}", input.Message);
            return new SteveNotification
                {
                    CorrelationId = input.CorrelationId,
                    Message = Process.GetCurrentProcess().Id.ToString()
                };
        }

        public Func<FromSteve,AlanNotification> RequestAndWaitResponse { get; set; }
        
        public void ListenFor(AlanNotification input)
        {
            Console.WriteLine("RECEIVED [ListenFor]- > {0}", input.Message);
        }

        public Action<SteveNotification> FireAndForgetRequest { get; set; }
    }
}
