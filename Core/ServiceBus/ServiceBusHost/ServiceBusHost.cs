using System;
using System.Configuration;

namespace ShortBus.ServiceBusHost
{
    /// <summary>
    /// The base class of any bus implementation. This will allow you to utilize the same
    /// linking methods regardless of the underlying bus infrastructure
    /// </summary>
    public abstract class ServiceBusHost : IDisposable
    {
        #region Methods
        /// <summary>
        /// Will provide your implementation with an Action<typeparam name="TIn">Input message</typeparam>
        /// That your external code can use to publish messages onto the underlying bus
        /// </summary>
        /// <typeparam name="TIn">The kind of message you would like to send</typeparam>
        /// <param name="callingMethod">The stub of the action signature you would like to recieve a reference for</param>
        /// <param name="useDistributedWhenPossible">self explanatory</param>
        /// <returns></returns>
        public Action<TIn> OnPublish<TIn>(Action<TIn> callingMethod, bool useDistributedWhenPossible) where TIn : class
        {
            if (callingMethod == null)
            {
                throw new ArgumentNullException("callingMethod");
            }
            if (useDistributedWhenPossible)
                RegisterDistributedFor<TIn>();
            callingMethod = RegisterFireAndForgetCall;
            return callingMethod;
        }

        /// <summary>
        /// Will provide your implementation with a Func<typeparam name="TIn"></typeparam>,<typeparam name="TOut"></typeparam>
        /// that will allow your external code to request a message from the bus and recieve a reply
        /// </summary>
        /// <typeparam name="TIn">Your request message. </typeparam>
        /// <typeparam name="TOut">Your response message </typeparam>
        /// <param name="callingMethod">The stub of the Func signature that you would like to recieve a reference for</param>
        /// <param name="useDistributedWhenPossible">self explanatory</param>
        /// <returns></returns>
        public Func<TIn, TOut> OnRequesting<TIn, TOut>(Func<TIn, TOut> callingMethod, bool useDistributedWhenPossible)
            where TOut : class, new()
            where TIn : class
        {
            if (callingMethod == null)
            {
                throw new ArgumentNullException("callingMethod");
            }
            if (useDistributedWhenPossible)
                RegisterDistributedFor<TIn>();

            callingMethod = RegisterRequstReponse<TIn, TOut>;
            return callingMethod;
        }

        /// <summary>
        /// Will correlate your method with a subscribtion on the under lying bus and invoke it when a message arrives
        /// </summary>
        /// <typeparam name="TIn">The kind of message you would like to receive</typeparam>
        /// <param name="methodToCall">The action to invoke upon reciept of that message</param>
        /// <param name="canBeDistributed"></param>
        public void Subscribe<TIn>(Action<TIn> methodToCall, bool canBeDistributed) where TIn : class
        {
            RegisterSubscriber(methodToCall, canBeDistributed);
        }

        /// <summary>
        /// Will correlate your method with a subscribtion on the under lying bus and invoke it when a message arrives
        /// </summary>
        /// <typeparam name="TIn">The type of messages you can handle</typeparam>
        /// <typeparam name="TOut">The return message for that Func.</typeparam>
        /// <param name="method">The func to invoke upon reciept of the message</param>
        /// <param name="canBeDistributed">Determines if message can be distributed</param>
        public void Subscribe<TIn, TOut>(Func<TIn, TOut> method, bool canBeDistributed)
            where TOut : class, new()
            where TIn : class
        {
            RegisterHandler(method, canBeDistributed);
        }

        /// <summary>
        /// Abstract method to implement on the bus for allow for publish only calls
        /// </summary>
        /// <typeparam name="TIn">The message to be published.  and must have a value</typeparam>
        /// <param name="msg">The instance of the message to be published and must have a value</param>
        protected abstract void RegisterFireAndForgetCall<TIn>(TIn msg) where TIn : class;

        /// <summary>
        /// Abstract method to implement on the bus for allow for Request/Response calls
        /// </summary>
        /// <typeparam name="TIn">The message to be sent in the requst. </typeparam>
        /// <typeparam name="TOut">The message to be received in the response. </typeparam>
        /// <param name="msg">The request message</param>
        /// <returns></returns>
        protected abstract TOut RegisterRequstReponse<TIn, TOut>(TIn msg)
            where TIn : class
            where TOut : class,new();

        /// <summary>
        /// Abstract method to implement on the bus for allow for Responding to Request/Response calls
        /// </summary>
        /// <typeparam name="TIn">The message to be received from the requester. </typeparam>
        /// <typeparam name="TOut">The message to be returned to the requester. </typeparam>
        /// <param name="hostedClassesFunc">The func to invoke to get the appropriate data</param>
        /// <param name="canBeDistributed">self explanatory</param>
        protected abstract void RegisterHandler<TIn, TOut>(Func<TIn, TOut> hostedClassesFunc, bool canBeDistributed)
            where TIn : class
            where TOut : class, new();

        /// <summary>
        /// Abstract method to implement on the bus for allow for Listener only calls
        /// </summary>
        /// <typeparam name="TIn">The message to listen for.</typeparam>
        /// <param name="hostedClassesAction">The Action to invoke when that message arrives</param>
        /// <param name="canBeDistributed">self explanatory</param>
        protected abstract void RegisterSubscriber<TIn>(Action<TIn> hostedClassesAction, bool canBeDistributed) where TIn : class;

        /// <summary>
        /// Use this method to initialize the concrete implementation of this class. These allow you to limit and control the scope of the underlying bus
        /// </summary>
        /// <param name="settings">Generic Host Settings</param>
        protected abstract void Register(HostSettings settings);

        protected abstract void RegisterDistributedFor<T>() where T : class;

        public abstract void Cleanup();

        public abstract object Bus { get; protected set; }

        #endregion Methods

        public static ServiceBusHost Create(Action<HostConfiguration> configuration)
        {
            var conf = new HostConfiguration();
            configuration.Invoke(conf);
            if (conf.Settings.HostCreator != null)
            {
                var host = conf.Settings.HostCreator.Invoke();

                if (conf.Settings.InstanceFuncs != null)
                    conf.Settings.InstanceFuncs.ForEach(x => x(host));

                host.Register(conf.Settings);
                return host;
            }
            throw new ConfigurationErrorsException("Configuration issue: Set the type of bus to use");
        }

        public void Dispose()
        {
            Cleanup();
        }
    }
}