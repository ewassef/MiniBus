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
        MakeRequests,
        INeedProcessed<FromAlan,SteveNotification>,
        IHandleMessage<FromSteve,AlanNotification>,
        IListen<SteveNotification>,
        IFireAndForgetRequest<AlanNotification>
    {
        public Func<FromAlan, SteveNotification> RequestAndWaitResponse { get; set; }

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

        public Action<AlanNotification> FireAndForgetRequest { get; set; }
    }
}
