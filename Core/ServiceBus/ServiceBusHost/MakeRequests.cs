using System;
using System.Configuration;
using System.Data;
using ShortBus.Hostable.Shared.Interface;

namespace ShortBus.ServiceBusHost
{
    public class MakeRequests
    {
        public TOut RequestAndWaitReply<TIn, TOut>(TIn msg)
            where TIn : ICorrelatedMessage
            where TOut : ICorrelatedMessage
        {
            var cast = this as INeedProcessed<TIn, TOut>;
            if (cast == null)
                throw new NotSupportedException(string.Format("This class must implement IRequestResponse<{0},{1}> to call this method in this way",typeof(TIn).Name,typeof(TOut).Name));
            if (cast.RequestAndWaitResponse == null)
                throw new ConstraintException("You must register this callback with the bus befire invoking it");
            return cast.RequestAndWaitResponse.Invoke(msg);
        }

        public void FireAndForget<TIn>(TIn msg)
        {
            var cast = this as IFireAndForgetRequest<TIn>;
            if (cast == null)
                throw new NotSupportedException(string.Format("This class must implement IFireAndForgetRequest<{0}> to call this method in this way", typeof(TIn).Name));
            if (cast.FireAndForgetRequest == null)
                throw new ConstraintException("You must register this callback with the bus befire invoking it");
            cast.FireAndForgetRequest.Invoke(msg);
        }
    }
}
