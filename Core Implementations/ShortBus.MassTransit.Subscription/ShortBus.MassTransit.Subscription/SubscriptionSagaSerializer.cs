using System;
using Magnum.StateMachine;
using MassTransit.Services.Subscriptions.Messages;
using MassTransit.Services.Subscriptions.Server;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;

namespace ShortBus.MassTransitHelper.Subscription
{
    internal class SubscriptionSagaSerializer : IBsonSerializer
    {
        public object Deserialize(BsonReader bsonReader, Type nominalType, IBsonSerializationOptions options)
        {
            return Deserialize(bsonReader, nominalType, nominalType, options);
        }

        public object Deserialize(BsonReader bsonReader, Type nominalType, Type actualType, IBsonSerializationOptions options)
        {
            bsonReader.ReadStartDocument();
            var guid = Guid.Parse(bsonReader.ReadString());

            var clientid = Guid.Parse(bsonReader.ReadString());
            var msgId = bsonReader.ReadString();
            var subId = Guid.Parse(bsonReader.ReadString());
            var mname = bsonReader.ReadString();
            var snum = bsonReader.ReadInt64();
            var endpoint = new Uri(bsonReader.ReadString());


            var state = bsonReader.ReadString();
            var toChangeTo = SubscriptionSaga.Initial;
            switch (state)
            {
                case "Active":
                    toChangeTo = SubscriptionSaga.Active;
                    break;

                case "Completed":
                    toChangeTo = SubscriptionSaga.Completed;
                    break;

                default:
                    toChangeTo = SubscriptionSaga.Initial;
                    break;
            }

            bsonReader.ReadEndDocument();
            var result = new SubscriptionSaga(guid);

            result.SubscriptionInfo = new SubscriptionInformation(clientid, subId, mname, msgId, endpoint);
            result.SubscriptionInfo.SequenceNumber = snum;
            //RuntimeHelpers.RunInstanceMethod(typeof(SubscriptionClientSaga), "ChangeCurrentState", result, new object[] { toChangeTo });
            result.SetCurrentState(toChangeTo);
            //result.ChangeCurrentState(state);
            return result;
        }

        public IBsonSerializationOptions GetDefaultSerializationOptions()
        {
            return new DocumentSerializationOptions();
        }

        public void Serialize(BsonWriter bsonWriter, Type nominalType, object value, IBsonSerializationOptions options)
        {
            if (value is SubscriptionSaga)
            {
                var converted = value as SubscriptionSaga;
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteString("_id", converted.CorrelationId.ToString());
                bsonWriter.WriteString("clientId", converted.SubscriptionInfo.ClientId.ToString());
                bsonWriter.WriteString("messageId", converted.SubscriptionInfo.CorrelationId??"");
                bsonWriter.WriteString("subscriptionId", converted.SubscriptionInfo.SubscriptionId.ToString());
                bsonWriter.WriteString("messageName", converted.SubscriptionInfo.MessageName);
                bsonWriter.WriteInt64("sequenceNumber", converted.SubscriptionInfo.SequenceNumber);
                bsonWriter.WriteString("endpoindUri", converted.SubscriptionInfo.EndpointUri.ToString());
                bsonWriter.WriteString("CurrentState", converted.CurrentState.Name);
                bsonWriter.WriteEndDocument();
            }
        }
    }
}