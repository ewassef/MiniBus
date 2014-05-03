using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.Diagnostics.Introspection;
using MassTransit.Distributor;
using MassTransit.Distributor.DistributorConfigurators;
using MassTransit.Distributor.WorkerConfigurators;
using MassTransit.Events;
using MassTransit.Log4NetIntegration;
using MassTransit.Monitoring;
using MassTransit.Saga;
using MassTransit.Services.HealthMonitoring;
using MassTransit.Services.HealthMonitoring.Messages;
using MassTransit.Services.HealthMonitoring.Server;
using MassTransit.Services.Subscriptions.Messages;
using MassTransit.SubscriptionConfigurators;
using MassTransit.Transports.ZeroMQ;
using ShortBus.Hostable.Shared.Interface;
using ShortBus.Hostable.Shared.Specialized;
using ShortBus.ServiceBusHost;
using log4net;
using ILog = MassTransit.Logging.ILog;

namespace ShortBus.ZeroMqHost
{
    /// <summary>
    /// Implements the Base Service Bus Host and allows us to generically link external classes
    /// to the bus to send and receive messages
    /// </summary>
    public class ZeroMqTransitHost : ServiceBusHost.ServiceBusHost
    {

        private IServiceBus _bus;
        private readonly List<UnsubscribeAction> _unsubscribeActions = new List<UnsubscribeAction>();
        static readonly log4net.ILog Log = LogManager.GetLogger(typeof(ZeroMqTransitHost));
        private HealthService _healthService;
        private readonly List<Action<WorkerBusServiceConfigurator>> distributedActions = new List<Action<WorkerBusServiceConfigurator>>();
        private readonly List<Action<SubscriptionBusServiceConfigurator>> nonDistributedActions = new List<Action<SubscriptionBusServiceConfigurator>>();
        private readonly List<Action<DistributorBusServiceConfigurator>> distributerSettings = new List<Action<DistributorBusServiceConfigurator>>();
        protected new readonly AutoResetEvent heartbeat = new AutoResetEvent(false);
        protected Dictionary<string, PerformanceCounter> _counters;
        protected bool _shuttingDown;
        /// <summary>
        /// Registers this instance with a particular bus. Every instance with the same name
        /// will end up on the same bus
        /// </summary>
        protected override void Register(HostSettings settings)
        {

            _bus = ServiceBusFactory.New(x =>
                {
                    var port = FindFreePort();
                    ZeroMqAddress.RegisterLocalPort(port);
                    x.ReceiveFrom(string.Format("tcp://{2}:{0}/{1}/", port, Process.GetCurrentProcess().ProcessName, Environment.MachineName));
                    Log.InfoFormat("Configured to recieve from port {0} on local machine", port);
                    x.SetPurgeOnStartup(true);
                    x.SetNetwork(settings.Network);
                    x.UseBsonSerializer();
                    x.SetShutdownTimeout(TimeSpan.FromSeconds(15));
                    x.UseZeroMq(configurator =>
                    {
                        if (!string.IsNullOrWhiteSpace(settings.SubscriberOnMachine))
                        {
                            configurator.UseSubscriptionService(string.Format("tcp://{0}:50000/", settings.SubscriberOnMachine));
                            Log.InfoFormat("Configured to subscribe to service at tcp://{0}:50000/", settings.SubscriberOnMachine);
                        }
                    });

                    x.UseControlBus();
                    x.UseLog4Net();
                    x.EnableMessageTracing();
                    x.EnableRemoteIntrospection();
                    x.UseHealthMonitoring(30);

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

            var repo = new InMemorySagaRepository<HealthSaga>();

            _healthService = new HealthService(_bus, repo);
            _healthService.Start();

            var counters = ServiceBusPerformanceCounters.Instance;
            string instanceName = string.Format("{0}_{1}{2}",
                    _bus.Endpoint.Address.Uri.Scheme, _bus.Endpoint.Address.Uri.Host, _bus.Endpoint.Address.Uri.AbsolutePath.Replace("/", "_"));
            //go through each counter and get the value
            var perf = counters.GetType().GetProperties().Where(p => p.PropertyType == typeof(RuntimePerformanceCounter)).ToList();
            _counters = perf.Select(x =>
            {
                var counter = x.GetValue(counters) as RuntimePerformanceCounter;
                var c = new PerformanceCounter("MassTransit", counter.Name, instanceName, true);
                return new { counter.Name, Counter = c };
            }).ToDictionary(x => x.Name, y => y.Counter);

            ThreadPool.RegisterWaitForSingleObject(heartbeat, (state, @out) => SendCustomUpdate(), null,
                                                   TimeSpan.FromSeconds(30), false);
        }

        private int FindFreePort()
        {
            var randomizer = new Random(DateTime.Now.Millisecond);
            var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            var tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();
            //try up to 1000 ports
            var attempts = 0;
            while (attempts < 1000)
            {
                var port = randomizer.Next(10000, 20000);
                if (tcpConnInfoArray.All(x => x.LocalEndPoint.Port != port))
                    return port;
                attempts++;
            }
            throw new SystemException("Couldnt find a free port to use");
        }

        protected override void RegisterDistributedFor<T>()
        {
            distributerSettings.Add((c) =>
                c.Handler<T>()
                .UseWorkerSelector<LeastBusyWorkerSelectorFactory>()
                .Transient());
        }

        public override void Cleanup()
        {
            _shuttingDown = true;
            try
            {
                heartbeat.Set();
                _healthService.Stop();
                _unsubscribeActions.ForEach(ua =>
                {
                    if (ua != null)
                        ua.Invoke();
                });

                if (_bus != null)
                    _bus.Dispose();

                _counters.ToList().ForEach(x =>
                    {
                        try
                        {
                            x.Value.Dispose();
                        }
                        catch
                        {

                        }
                    });

            }
            catch (Exception)
            {

            }
        }

        public override object Bus { get { return _bus; } protected set { } }

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
                    Log.Error(fault.FailedMessage);
                    handlerComplete.Set();
                });
            });
            handlerComplete.WaitOne();
            if (result != null)
            {
                result.CorrelationId = msg.CorrelationId;
                Log.InfoFormat("Recieved a reponse for msg with correlation ID {0}", msg.CorrelationId);
            }
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
            if (canBeDistributed)
            {
                Action<WorkerBusServiceConfigurator> x = (c) =>
                    {

                        var handler = c.Handler<TIn>(msg =>
                            {
                                var output = hostedClassesFunc.Invoke(msg);
                                if (output != null)
                                    output.CorrelationId = msg.CorrelationId;
                                var context = _bus.MessageContext<TIn>();
                                context.Respond(output ?? new TOut() { CorrelationId = msg.CorrelationId });
                            }).Transient();

                    };

                distributedActions.Add(x);
            }
            else
            {
                nonDistributedActions.Add(configurator => configurator.Handler<TIn>(msg =>
                {
                    var output = hostedClassesFunc.Invoke(msg);
                    if (output != null)
                        output.CorrelationId = msg.CorrelationId;
                    var context = _bus.MessageContext<TIn>();
                    context.Respond(output ?? new TOut() { CorrelationId = msg.CorrelationId });
                }).Transient());
            }
        }

        /// <summary>
        /// Abstract method to implement on the bus for allow for Listener only calls
        /// </summary>
        /// <typeparam name="TIn">The message to listen for.</typeparam>
        /// <param name="hostedClassesAction">The Action to invoke when that message arrives</param>
        protected override void RegisterSubscriber<TIn>(Action<TIn> hostedClassesAction, bool canBeDistributed)
        {

            if (canBeDistributed)
            {
                distributedActions.Add(c => c.Handler(hostedClassesAction).Transient());
            }
            else
            {
                nonDistributedActions.Add(configurator => configurator.Handler(hostedClassesAction).Transient());
            }

        }

        void SendCustomUpdate()
        {
            if (_shuttingDown)
                return;
            Log.DebugFormat("Broadcasting out a health and stats update...");
            _healthService.Consume(new HealthUpdateRequest());
            var perfUpdate = new PerformanceUpdate()
                {
                    UpdateTimeUtc = DateTime.UtcNow,
                    Counters = new List<Entry>()
                };

            var probe = new InMemoryDiagnosticsProbe();
            _bus.Inspect(probe);

            perfUpdate.Counters.AddRange(probe.Entries.Select(x => new Entry
                {
                    Context = x.Context,
                    Key = x.Key,
                    Value = x.Value
                }));

            foreach (var counter in _counters)
            {
                try
                {
                    perfUpdate.Counters.Add(new Entry
                                {
                                    Context = Environment.MachineName,
                                    Key = counter.Key,
                                    Value = counter.Value.NextSample().RawValue.ToString()
                                });
                }
                catch (Exception ex)
                {
                    Log.Error("Encountered error getting perf value:", ex);
                }
            }

            _bus.Publish(perfUpdate);
        }
    }
}
