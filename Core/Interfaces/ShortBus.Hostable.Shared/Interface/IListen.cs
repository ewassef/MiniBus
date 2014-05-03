namespace ShortBus.Hostable.Shared.Interface
{
    /// <summary>
    /// Use this interface when you want to have a method that is accepting any message 
    /// from any other clients of your specific type TIn
    /// </summary>
    /// <typeparam name="TIn">What kind of message you wish to listen to</typeparam>
    public interface IListen<in TIn> where TIn:class
    {
        void ListenFor(TIn input);
    }
}