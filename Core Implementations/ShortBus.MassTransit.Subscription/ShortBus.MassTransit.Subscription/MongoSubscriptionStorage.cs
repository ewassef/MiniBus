using System;
using System.Collections.Generic;
using System.Linq;
using MassTransit.Subscriptions.Coordinator;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace ShortBus.MassTransitHelper.Subscription
{
    public class MongoSubscriptionStorage : SubscriptionStorage
    {
        private MongoServerSettings settings;
        private MongoServer server;
        private MongoDatabase database;
        private MongoCollection collection;

        public MongoSubscriptionStorage(string serverName)
        {
            // Create server settings to pass connection string, timeout, etc.
            settings = new MongoServerSettings
            {
                Server = new MongoServerAddress(serverName)
            };
            // Create server object to communicate with our server
            server = new MongoServer(settings);
            // Get our database instance to reach collections and data
            database = server.GetDatabase("MessageDB");
            collection = database.GetCollection<PersistentSubscription>("PersistentSubscriptions");
        }

        public void Dispose()
        {
            server.Disconnect();
        }

        public void Add(PersistentSubscription subscription)
        {
            collection.Insert(subscription);
        }

        public void Remove(PersistentSubscription subscription)
        {
            collection.FindAndRemove(Query.Matches("SubscriptionId", BsonRegularExpression.Create(subscription.SubscriptionId)),SortBy.Ascending());
        }

        public IEnumerable<PersistentSubscription> Load(Uri busUri)
        {
            return collection.FindAllAs<PersistentSubscription>().ToList();
        }
    }
}