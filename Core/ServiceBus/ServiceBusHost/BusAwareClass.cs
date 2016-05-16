using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortBus.ServiceBusHost
{
    public abstract class BusAwareClass : IDisposable
    {
        private readonly ServiceBusHost _host;
        
        protected BusAwareClass(ServiceBusHost host)
        {
            _host = host;
        }

        public ServiceBusHost Host { get { return _host; } }


        protected void Publish<TIn>(ref Action<TIn> actionToUse,bool partOfADistrubutedService = false) where TIn : class
        {
            actionToUse = Host.OnPublish(actionToUse, partOfADistrubutedService);
        }

        protected void Register<TIn,TOut>(ref Func<TIn,TOut> funcToUse, bool partOfADistrubutedService = false) where TIn : class where TOut : class, new()
        {
            funcToUse = Host.OnRequesting(funcToUse, partOfADistrubutedService);
        }

        public void Dispose()
        {
            _host.Dispose();
        }
    }
}
