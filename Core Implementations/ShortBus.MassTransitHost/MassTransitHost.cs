using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MassTransit;
using MassTransit.Distributor;
using MassTransit.Distributor.DistributorConfigurators;
using MassTransit.Distributor.WorkerConfigurators;
using MassTransit.Log4NetIntegration;
using MassTransit.SubscriptionConfigurators;
using ShortBus.Hostable.Shared.Specialized;
using ShortBus.MassTransit.CacheSelectorFactory;
using ShortBus.ServiceBusHost;

namespace ShortBus.MassTransitHost
{
    /// <summary>
    /// Implements the Base Service Bus Host and allows us to generically link external classes
    /// to the bus to send and receive messages
    /// </summary>
    public class MassTransitHost : ServiceBusHost.ServiceBusHost
    {
        private IServiceBus _bus;
        private readonly List<UnsubscribeAction> _unsubscribeActions = new List<UnsubscribeAction>();

        private readonly List<Action<WorkerBusServiceConfigurator>> distributedActions = new List<Action<WorkerBusServiceConfigurator>>();
        private readonly List<Action<SubscriptionBusServiceConfigurator>> nonDistributedActions = new List<Action<SubscriptionBusServiceConfigurator>>();
        private readonly List<Action<DistributorBusServiceConfigurator>> distributerSettings = new List<Action<DistributorBusServiceConfigurator>>();

        /// <summary>
        /// Registers this instance with a particular bus. Every instance with the same name
        /// will end up on the same bus
        /// </summary>
        protected override void Register(HostSettings settings)
        {

            _bus = ServiceBusFactory.New(x =>
            {

                x.ReceiveFrom(string.Format("msmq://localhost/{0}_{1}", settings.Network,
                                            settings.ClusterServiceName));
                x.SetPurgeOnStartup(true);
                x.SetNetwork(settings.Network);
                x.UseMsmq(configurator =>
                {
                    configurator.VerifyMsmqConfiguration();
                    if (!string.IsNullOrWhiteSpace(settings.SubscriberOnMachine))
                        configurator.UseSubscriptionService(string.Format("msmq://{0}/subscriptions",
                                                                          settings.SubscriberOnMachine));
                    else
                        configurator.UseMulticastSubscriptionClient();
                });
                x.UseControlBus();
                x.UseLog4Net();
                x.EnableMessageTracing();
                foreach (var a in distributedActions)
                {
                    x.Worker(a);
                }
                foreach (var a in nonDistributedActions)
                {
                    x.Subscribe(a);
                }
                foreach (var s in distributerSettings)
                {
                    x.Distributor(s);
                }
            });
            Bus = _bus;
        }

        protected override void RegisterDistributedFor<T>()
        {
            //HACK: This is to fix a bug with MT 2.8
            //return;
            distributerSettings.Add((c) =>
            {
                if (typeof(T).GetCustomAttributes(true).Any(s => s is CachableItemAttribute))
                {
                    c.Handler<T>().UseWorkerSelector<EuclidSelectorFactory>();
                }
                else
                {
                    c.Handler<T>().UseWorkerSelector<LeastBusyWorkerSelectorFactory>();
                }
            });
        }

        public override void Cleanup()
        {
            _unsubscribeActions.ForEach(ua =>
            {
                if (ua != null)
                    ua.Invoke();
            });

            _bus.Dispose();
            _bus = null;
        }

        public override object Bus { get; protected set; }

        /// <summary>
        /// Abstract method to implement on the bus for allow for publish only calls
        /// </summary>
        /// <typeparam name="TIn">The message to be published.<remarks>Implements <see cref="ICorrelatedMessage"/>ICorrelatedMessage</remarks> and must have a value</typeparam>
        /// <param name="msg">The instance of the message to be published<remarks>Implements <see cref="ICorrelatedMessage"/>ICorrelatedMessage</remarks> and must have a value</param>
        protected override void RegisterFireAndForgetCall<TIn>(TIn msg)
        {
            _bus.Publish(msg);
        }

        /// <summary>
        /// Abstract method to implement on the bus for allow for Request/Response calls
        /// </summary>
        /// <typeparam name="TIn">The message to be sent in the requst.<remarks>Implements <see cref="ICorrelatedMessage"/>ICorrelatedMessage</remarks> and must have a value</typeparam>
        /// <typeparam name="TOut">The message to be received in the response.<remarks>Implements <see cref="ICorrelatedMessage"/>ICorrelatedMessage</remarks> and must have a value</typeparam>
        /// <param name="msg">The request message</param>
        /// <returns></returns>
        protected override TOut RegisterRequstReponse<TIn, TOut>(TIn msg)
        {
            TOut result = null;
            var handlerComplete = new ManualResetEvent(false);
            _bus.PublishRequest(msg, resp =>
            {
                resp.Handle<TOut>(respMsg =>
                {
                    result = respMsg;
                    handlerComplete.Set();
                });
                resp.HandleTimeout(TimeSpan.FromSeconds(10), () => handlerComplete.Set());
                resp.HandleFault(fault =>
                {
                    //log or something
                    Console.WriteLine(fault.FailedMessage);
                    handlerComplete.Set();
                });
            });
            handlerComplete.WaitOne();

            return result;

        }

        /// <summary>
        /// Abstract method to implement on the bus for allow for Responding to Request/Response calls
        /// </summary>
        /// <typeparam name="TIn">The message to be received from the requester.<remarks>Implements <see cref="ICorrelatedMessage"/>ICorrelatedMessage</remarks> and must have a value</typeparam>
        /// <typeparam name="TOut">The message to be returned to the requester.<remarks>Implements <see cref="ICorrelatedMessage"/>ICorrelatedMessage</remarks> and must have a value</typeparam>
        /// <param name="hostedClassesFunc">The func to invoke to get the appropriate data</param>
        /// <param name="canBeDistributed"></param>
        protected override void RegisterHandler<TIn, TOut>(Func<TIn, TOut> hostedClassesFunc, bool canBeDistributed = false)
        {
            //HACK: This is to fix a bug with MT 2.8
            if (canBeDistributed)
            {
                Action<WorkerBusServiceConfigurator> x = (c) => c.Handler<TIn>(msg =>
                {
                    var output = hostedClassesFunc.Invoke(msg);

                    var context = _bus.MessageContext<TIn>();
                    context.Respond(output ?? new TOut());
                });
                distributedActions.Add(x);
            }
            else
            {
                nonDistributedActions.Add(configurator => configurator.Handler<TIn>(msg =>
                {
                    var output = hostedClassesFunc.Invoke(msg);
                    var context = _bus.MessageContext<TIn>();
                    context.Respond(output ?? new TOut());
                }));
            }
        }

        /// <summary>
        /// Abstract method to implement on the bus for allow for Listener only calls
        /// </summary>
        /// <typeparam name="TIn">The message to listen for.</typeparam>
        /// <param name="hostedClassesAction">The Action to invoke when that message arrives</param>
        protected override void RegisterSubscriber<TIn>(Action<TIn> hostedClassesAction, bool canBeDistributed)
        {
            //HACK: This is to fix a bug with MT 2.8
            if (canBeDistributed)
            {
                distributedActions.Add(c => c.Handler(hostedClassesAction));
            }
            else
            {
                nonDistributedActions.Add(configurator => configurator.Handler(hostedClassesAction));
            }

        }
    }
}