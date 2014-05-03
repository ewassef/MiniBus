// -----------------------------------------------------------------------
// <copyright file="ZmqMessage.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace MassTransit.Transports.ZeroMQ
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Text;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    [Serializable]
    public class ZmqMessage
    {
        public string ContentType { get; set; }
        public string OriginalMessageId { get; set; }
        public byte[] Body { get; set; }

        public byte[] Serialize()
        {
            try
            {
                using (var stream = new MemoryStream())
                {
                    var serializer = new BinaryFormatter();
                    serializer.Serialize(stream,this);
                    return stream.ToArray();
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                return new byte[0];
            }
        }

        public static ZmqMessage Deserialize(byte[] bites)
        {
            if (bites.Length < 20)
                return null;
            try
            {
                using (var stream = new MemoryStream(bites))
                {
                    var serializer = new BinaryFormatter();
                    return serializer.Deserialize(stream) as ZmqMessage;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
