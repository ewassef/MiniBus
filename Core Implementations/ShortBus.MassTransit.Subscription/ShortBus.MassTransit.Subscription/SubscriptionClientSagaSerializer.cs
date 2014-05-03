using System;
using Magnum.StateMachine;
using MassTransit.Services.Subscriptions.Server;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;

namespace ShortBus.MassTransitHelper.Subscription
{
    internal class SubscriptionClientSagaSerializer : IBsonSerializer
    {


        public object Deserialize(BsonReader bsonReader, Type nominalType, IBsonSerializationOptions options)
        {
            return Deserialize(bsonReader, nominalType, nominalType, options);
        }

        public object Deserialize(BsonReader bsonReader, Type nominalType, Type actualType, IBsonSerializationOptions options)
        {
            bsonReader.ReadStartDocument();
            var guid = Guid.Parse(bsonReader.ReadString());
            var control = new Uri(bsonReader.ReadString());
            var data = new Uri(bsonReader.ReadString());
            var state = bsonReader.ReadString();
            State toChangeTo;
            switch (state)
            {
                case "Active":
                    toChangeTo = SubscriptionClientSaga.Active;
                    break;

                case "Completed":
                    toChangeTo = SubscriptionClientSaga.Completed;
                    break;

                default:
                    toChangeTo = SubscriptionClientSaga.Initial;
                    break;
            }

            bsonReader.ReadEndDocument();
            var result = new SubscriptionClientSaga(guid);

            result.ControlUri = control;
            result.DataUri = data;
            result.SetCurrentState(toChangeTo);
            //RuntimeHelpers.RunInstanceMethod(typeof(SubscriptionClientSaga), "ChangeCurrentState", result, new object[] { toChangeTo });
            //result.ChangeCurrentState(state);
            return result;
        }

        public IBsonSerializationOptions GetDefaultSerializationOptions()
        {
            return new DocumentSerializationOptions();
        }

        public void Serialize(BsonWriter bsonWriter, Type nominalType, object value, IBsonSerializationOptions options)
        {
            if (value is SubscriptionClientSaga)
            {
                var converted = value as SubscriptionClientSaga;
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteString("_id", converted.CorrelationId.ToString());
                bsonWriter.WriteString("DataUri", converted.DataUri.ToString());
                bsonWriter.WriteString("ControlUri", converted.ControlUri.ToString());
                bsonWriter.WriteString("CurrentState", converted.CurrentState.Name);
                bsonWriter.WriteEndDocument();
            }
        }
    }
}