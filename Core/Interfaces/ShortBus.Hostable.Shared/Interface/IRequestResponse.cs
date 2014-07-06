using System;

namespace ShortBus.Hostable.Shared.Interface
{
    /// <summary>
    /// Allows you to hook into external listeners and request data. The result will be returned 
    /// within 4 seconds or a null response will be returned.
    /// </summary>
    /// <typeparam name="TIn">Your request message with Correlation id</typeparam>
    /// <typeparam name="TOut">The listeners response with the same key</typeparam>
    public interface INeedProcessed<TIn, TOut>
        
    {
        Func<TIn, TOut> RequestAndWaitResponse { get; set; }
    }
}