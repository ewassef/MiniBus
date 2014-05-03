using System;
using System.Collections.Generic;
using System.Linq;
using MassTransit;
using MassTransit.Exceptions;
using MassTransit.Logging;
using MassTransit.Pipeline;
using MassTransit.Saga;
using MassTransit.Services.Subscriptions.Server;
using MassTransit.Util;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace ShortBus.MassTransitHelper.Subscription
{
    public class MongoSagaStorage<TSaga> : ISagaRepository<TSaga> where TSaga : class,ISaga
    {
        private MongoServerSettings settings;
        private MongoServer server;
        private MongoDatabase database;
        private MongoCollection collection;

        public MongoSagaStorage()
        {
            // Create server settings to pass connection string, timeout, etc.
            settings = new MongoServerSettings
                {
                    Server = new MongoServerAddress("devserver01")
                };
            // Create server object to communicate with our server
            server = new MongoServer(settings);
            // Get our database instance to reach collections and data
            database = server.GetDatabase("SagaDB");
            collection = database.GetCollection<TSaga>(typeof(TSaga).Name);

            if (typeof(TSaga) == typeof(SubscriptionClientSaga))
                BsonSerializer.RegisterSerializer(typeof(SubscriptionClientSaga), new SubscriptionClientSagaSerializer());
            if (typeof(TSaga) == typeof(SubscriptionSaga))
                BsonSerializer.RegisterSerializer(typeof(SubscriptionSaga), new SubscriptionSagaSerializer());
        }

        private static readonly ILog _log = Logger.Get(typeof(MongoSagaStorage<TSaga>).ToFriendlyName());

        public IEnumerable<Action<IConsumeContext<TMessage>>> GetSaga<TMessage>(IConsumeContext<TMessage> context, Guid sagaId, InstanceHandlerSelector<TSaga, TMessage> selector, ISagaPolicy<TSaga, TMessage> policy) where TMessage : class
        {
            if (policy.CanCreateInstance(context))
            {
                yield return x =>
                    {
                        if (_log.IsDebugEnabled)
                            _log.DebugFormat("SAGA: {0} Creating New {1} for {2}",
                                             typeof(TSaga).ToFriendlyName(), sagaId,
                                             typeof(TMessage).ToFriendlyName());

                        try
                        {
                            var instance = policy.CreateInstance(x, sagaId);

                            foreach (var callback in selector(instance, x))
                            {
                                callback(x);
                            }

                            if (!policy.CanRemoveInstance(instance))
                            {
                                collection.Insert(instance);
                            }
                        }
                        catch (Exception ex)
                        {
                            var sex = new SagaException("Create Saga Instance Exception", typeof(TSaga),
                                                        typeof(TMessage), sagaId, ex);
                            if (_log.IsErrorEnabled)
                                _log.Error(sex);

                            throw sex;
                        }
                    };
            }
            else
            {
                if (_log.IsDebugEnabled)
                    _log.DebugFormat("SAGA: {0} Ignoring Missing {1} for {2}", typeof(TSaga).ToFriendlyName(),
                                     sagaId,
                                     typeof(TMessage).ToFriendlyName());
            }

            var canUseExisting = false;
            try
            {
                canUseExisting = policy.CanUseExistingInstance(context);
            }
            catch
            {}

            if (canUseExisting)
            {
                yield return x =>
                {
                    if (_log.IsDebugEnabled)
                        _log.DebugFormat("SAGA: {0} Using Existing {1} for {2}",
                            typeof(TSaga).ToFriendlyName(), sagaId,
                            typeof(TMessage).ToFriendlyName());

                    //get the instance
                    var instance = Where(new SagaFilter<TSaga>(s => s.CorrelationId == sagaId)).FirstOrDefault();
                    if (instance == null)
                        return;

                    try
                    {
                        foreach (var callback in selector(instance, x))
                        {
                            callback(x);
                        }

                        if (policy.CanRemoveInstance(instance))
                        {
                            var query = Query.EQ("_id", instance.CorrelationId.ToString()); 
                            collection.Remove(query);
                        }
                    }
                    catch (Exception ex)
                    {
                        var sex = new SagaException("Existing Saga Instance Exception", typeof(TSaga),
                            typeof(TMessage), sagaId, ex);
                        if (_log.IsErrorEnabled)
                            _log.Error(sex);

                        
                        throw sex;
                    }
                };
            }
            else
            {
                if (_log.IsDebugEnabled)
                    _log.DebugFormat("SAGA: {0} Ignoring Existing {1} for {2}", typeof(TSaga).ToFriendlyName(),
                        sagaId,
                        typeof(TMessage).ToFriendlyName());
            }

        }

        public IEnumerable<Guid> Find(ISagaFilter<TSaga> filter)
        {
            return Where(filter, x => x.CorrelationId);
        }

        public IEnumerable<TSaga> Where(ISagaFilter<TSaga> filter)
        {
            _log.Debug(filter.FilterExpression);
            List<TSaga> result = collection.FindAllAs<TSaga>().ToList()
                                           .Where(filter.FilterExpression.Compile())
                                           .ToList();
            return result;
        }

        public IEnumerable<TResult> Where<TResult>(ISagaFilter<TSaga> filter, Func<TSaga, TResult> transformer)
        {
            return Where(filter).Select(transformer);
        }

        public IEnumerable<TResult> Select<TResult>(Func<TSaga, TResult> transformer)
        {
            List<TResult> result = collection.FindAllAs<TSaga>().ToList()
                                             .Select(transformer)
                                             .ToList();
            return result;
        }
    }
}