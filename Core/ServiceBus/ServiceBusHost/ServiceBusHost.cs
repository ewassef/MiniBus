using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using ShortBus.Hostable.Shared.Interface;

namespace ShortBus.ServiceBusHost
{
    /// <summary>
    /// The base class of any bus implementation. This will allow you to utilize the same 
    /// linking methods regardless of the underlying bus infrastructure
    /// </summary>
    public abstract class ServiceBusHost : IDisposable
    {
        #region Properties

        /// <summary>
        /// Returns the number of commands in the registry
        /// </summary>
        public int RegistrySize
        {
            get { return 0; }
        }
        #endregion

        #region Methods

        /// <summary>
        /// Will provide your implementation with an Action<typeparam name="TIn">Input message</typeparam>
        /// That your external code can use to publish messages onto the underlying bus
        /// </summary>
        /// <typeparam name="TIn">The kind of message you would like to send</typeparam>
        /// <param name="callingMethod">The stub of the action signature you would like to recieve a reference for</param>
        /// <returns></returns>
        internal Action<TIn> OnPublish<TIn>(Action<TIn> callingMethod, bool useDistributedWhenPossible) where TIn : class
        {
            if (useDistributedWhenPossible)
                RegisterDistributedFor<TIn>();
            callingMethod = RegisterFireAndForgetCall;
            return callingMethod;
        }


        /// <summary>
        /// Will provide your implementation with a Func<typeparam name="TIn"></typeparam>,<typeparam name="TOut"></typeparam>
        /// that will allow your external code to request a message from the bus and recieve a reply
        /// </summary>
        /// <typeparam name="TIn">Your request message.<remarks>Implements <see cref="ICorrelatedMessage"/>ICorrelatedMessage</remarks> and must have a value</typeparam>
        /// <typeparam name="TOut">Your response message.<remarks>Implements <see cref="ICorrelatedMessage"/>ICorrelatedMessage</remarks> and must have a value</typeparam>
        /// <param name="callingMethod">The stub of the Func signature that you would like to recieve a reference for</param>
        /// <returns></returns>
        internal Func<TIn, TOut> OnRequesting<TIn, TOut>(Func<TIn, TOut> callingMethod, bool useDistributedWhenPossible)
            where TOut : class, ICorrelatedMessage, new() where TIn : class, ICorrelatedMessage
        {
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
        internal void Subscribe<TIn>(Action<TIn> methodToCall, bool canBeDistributed) where TIn : class
        {
            RegisterSubscriber(methodToCall, canBeDistributed);
        }

        /// <summary>
        /// Will correlate your method with a subscribtion on the under lying bus and invoke it when a message arrives
        /// </summary>
        /// <typeparam name="TIn">The type of messages you can handle.<remarks>Implements <see cref="ICorrelatedMessage"/>ICorrelatedMessage</remarks> and must have a value</typeparam>
        /// <typeparam name="TOut">The return message for that Func.<remarks>Implements <see cref="ICorrelatedMessage"/>ICorrelatedMessage</remarks> and must match the request messages</typeparam>
        /// <param name="method">The func to invoke upon reciept of the message</param>
        internal void Subscribe<TIn, TOut>(Func<TIn, TOut> method, bool canBeDistributed)
            where TOut : class, ICorrelatedMessage, new() where TIn : class, ICorrelatedMessage
        {
            RegisterHandler(method, canBeDistributed);
        }


        /// <summary>
        /// Abstract method to implement on the bus for allow for publish only calls
        /// </summary>
        /// <typeparam name="TIn">The message to be published.<remarks>Implements <see cref="ICorrelatedMessage"/>ICorrelatedMessage</remarks> and must have a value</typeparam>
        /// <param name="msg">The instance of the message to be published<remarks>Implements <see cref="ICorrelatedMessage"/>ICorrelatedMessage</remarks> and must have a value</param>
        protected abstract void RegisterFireAndForgetCall<TIn>(TIn msg) where TIn : class;

        /// <summary>
        /// Abstract method to implement on the bus for allow for Request/Response calls
        /// </summary>
        /// <typeparam name="TIn">The message to be sent in the requst.<remarks>Implements <see cref="ICorrelatedMessage"/>ICorrelatedMessage</remarks> and must have a value</typeparam>
        /// <typeparam name="TOut">The message to be received in the response.<remarks>Implements <see cref="ICorrelatedMessage"/>ICorrelatedMessage</remarks> and must have a value</typeparam>
        /// <param name="msg">The request message</param>
        /// <returns></returns>
        protected abstract TOut RegisterRequstReponse<TIn, TOut>(TIn msg)
            where TIn : class,ICorrelatedMessage
            where TOut : class,ICorrelatedMessage,new();


        /// <summary>
        /// Abstract method to implement on the bus for allow for Responding to Request/Response calls
        /// </summary>
        /// <typeparam name="TIn">The message to be received from the requester.<remarks>Implements <see cref="ICorrelatedMessage"/>ICorrelatedMessage</remarks> and must have a value</typeparam>
        /// <typeparam name="TOut">The message to be returned to the requester.<remarks>Implements <see cref="ICorrelatedMessage"/>ICorrelatedMessage</remarks> and must have a value</typeparam>
        /// <param name="hostedClassesFunc">The func to invoke to get the appropriate data</param>
        protected abstract void RegisterHandler<TIn, TOut>(Func<TIn, TOut> hostedClassesFunc, bool canBeDistributed)
            where TIn : class,ICorrelatedMessage
            where TOut : class,ICorrelatedMessage, new();


        /// <summary>
        /// Abstract method to implement on the bus for allow for Listener only calls
        /// </summary>
        /// <typeparam name="TIn">The message to listen for.</typeparam>
        /// <param name="hostedClassesAction">The Action to invoke when that message arrives</param>
        protected abstract void RegisterSubscriber<TIn>(Action<TIn> hostedClassesAction, bool canBeDistributed) where TIn : class;


        /// <summary>
        /// Use this method to initialize the concrete implementation of this class. These allow you to limit and control the scope of the underlying bus
        /// </summary>
        /// <param name="network">All items with this same name will be on the same 'bus'</param>
        /// <param name="localIdentifier">If you need additional details, such as a queuename, this would be passed here</param>
        protected abstract void Register(HostSettings settings);

        protected abstract void RegisterDistributedFor<T>() where T : class;

        public abstract void Cleanup();

        public abstract object Bus { get; protected set; }

        #endregion

        public static ServiceBusHost Create(Action<HostConfiguration> configuration)
        {
            var conf = new HostConfiguration();
            configuration.Invoke(conf);
            if (conf.Settings.HostCreator != null)
            {
                var host = conf.Settings.HostCreator.Invoke();
                conf.Settings.PublishActions.ForEach(x => x.Invoke(host));
                conf.Settings.RequestFuncs.ForEach(x => x.Invoke(host));
                conf.Settings.SubscriptionActions.ForEach(x => x.Invoke(host));
                conf.Settings.SubscriptionFuncs.ForEach(x => x.Invoke(host));
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

    public class HostConfiguration
    {
        public readonly HostSettings Settings = new HostSettings();

        public void Host(object instance)
        {
            Settings.Instance = instance;
            //Go through the instance using reflection and register the calls
            var interfaces = instance.GetType().GetInterfaces();
            var methods = GetType().GetMethods(System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.DeclaredOnly);
            //IFireAndForgetRequest -> Register<T>
            var method = methods.First(x => x.ToString().Contains("Register[TIn]"));
            foreach (var face in interfaces.Where(x => x.GetGenericTypeDefinition() == typeof(IFireAndForgetRequest<>)))
            {
                var arg = face.GetGenericArguments();
                var generic = method.MakeGenericMethod(arg);
                generic.Invoke(this, new object[] { true });
            }
            //IRequestResponse -> Register<T1,T2>
            method = methods.First(x => x.ToString().Contains("Register[TIn,TOut]"));
            foreach (var face in interfaces.Where(x => x.GetGenericTypeDefinition() == typeof(INeedProcessed<,>)))
            {
                var arg = face.GetGenericArguments();
                var generic = method.MakeGenericMethod(arg);
                generic.Invoke(this, new object[] {true});
            }
            //IListen -> Subscribe<T>
            method = methods.First(x => x.ToString().Contains("Subscribe[TIn]"));
            foreach (var face in interfaces.Where(x => x.GetGenericTypeDefinition() == typeof(IListen<>)))
            {
                var arg = face.GetGenericArguments();
                var generic = method.MakeGenericMethod(arg);
                generic.Invoke(this, new object[] { true });
            }
            //ICommand -> Subscribe<T1,T2>
            method = methods.First(x => x.ToString().Contains("Subscribe[TIn,TOut]"));
            foreach (var face in interfaces.Where(x => x.GetGenericTypeDefinition() == typeof(IHandleMessage<,>)))
            {
                var arg = face.GetGenericArguments();
                var generic = method.MakeGenericMethod(arg);
                generic.Invoke(this, new object[] { true });
            }

        }

        public void PartOfServiceGroup(string serviceGroupName)
        {
            Settings.ClusterServiceName = serviceGroupName;
        }
        public void SetNetwork(string network)
        {
            Settings.Network = network;
        }
        public void SubscriptionServiceHost(string machineName)
        {
            Settings.SubscriberOnMachine = machineName;
        }

        protected void Register<TIn>(bool useDistributed = true) where TIn : class
        {
            Settings.PublishActions.Add(host =>
                {
                    var cast = Settings.Instance as IFireAndForgetRequest<TIn>;
                    if (cast == null)
                        throw new NotSupportedException(string.Format("This class must implement IFireAndForgetRequest<{0}> to call this method in this way", typeof(TIn).Name));
                    cast.FireAndForgetRequest = host.OnPublish(cast.FireAndForgetRequest, useDistributed);

                });
        }

        protected void Register<TIn, TOut>(bool useDistributed = true)
            where TOut : class, ICorrelatedMessage, new() where TIn : class, ICorrelatedMessage
        {
            Settings.RequestFuncs.Add(host =>
                {
                    var cast = Settings.Instance as INeedProcessed<TIn, TOut>;
                    if (cast == null)
                        throw new NotSupportedException(string.Format("This class must implement IRequestResponse<{0},{1}> to call this method in this way", typeof(TIn).Name, typeof(TOut).Name));
                    cast.RequestAndWaitResponse = host.OnRequesting(cast.RequestAndWaitResponse,useDistributed);
                });
        }

        protected void Subscribe<TIn>(bool canBeDistributed = true) where TIn : class
        {
            Settings.SubscriptionActions.Add(host =>
                {
                    var cast = Settings.Instance as IListen<TIn>;
                    if (cast == null)
                        throw new NotSupportedException(string.Format("This class must implement IListen<{0}> to call this method in this way", typeof(TIn).Name));
                    host.Subscribe<TIn>(cast.ListenFor,canBeDistributed);
                });
        }

        protected void Subscribe<TIn, TOut>(bool canBeDistributed = true)
            where TOut : class, ICorrelatedMessage, new() where TIn : class, ICorrelatedMessage
        {
            Settings.SubscriptionFuncs.Add(host =>
                {
                    var cast = Settings.Instance as IHandleMessage<TIn, TOut>;
                    if (cast == null)
                        throw new NotSupportedException(string.Format("This class must implement ICommand<{0},{1}> to call this method in this way", typeof(TIn).Name, typeof(TOut).Name));
                    host.Subscribe<TIn, TOut>(cast.ProcessMessage,canBeDistributed);
                });
        }



    }

    public class HostSettings
    {
        public string Network;
        public string ClusterServiceName;
        public string SubscriberOnMachine;
        public object Instance;

        public Func<ServiceBusHost> HostCreator;

        private readonly List<Action<ServiceBusHost>> _published = new List<Action<ServiceBusHost>>();
        private readonly List<Action<ServiceBusHost>> _requests = new List<Action<ServiceBusHost>>();
        private readonly List<Action<ServiceBusHost>> _hostedActions = new List<Action<ServiceBusHost>>();
        private readonly List<Action<ServiceBusHost>> _hostFuncs = new List<Action<ServiceBusHost>>();

        public List<Action<ServiceBusHost>> PublishActions { get { return _published; } }
        public List<Action<ServiceBusHost>> RequestFuncs { get { return _requests; } }
        public List<Action<ServiceBusHost>> SubscriptionActions { get { return _hostedActions; } }
        public List<Action<ServiceBusHost>> SubscriptionFuncs { get { return _hostFuncs; } }


    }
}
