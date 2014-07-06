namespace ShortBus.Hostable.Shared.Interface
{
    /// <summary>
    /// Allows you to accept messages from external requesters and respond to them according to your 
    /// own implemented logic
    /// </summary>
    /// <typeparam name="TIn">The input message type.</typeparam>
    /// <typeparam name="TOut">The return type expected. NOTE: make sure you correlated the ID with the request</typeparam>
    public interface IHandleMessage<in TIn, out TOut>
    {
        TOut ProcessMessage(TIn input);
    }
}