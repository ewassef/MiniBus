using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShortBus.Hostable.Shared.Interface;

namespace Language
{
    [Serializable]
    public class FromAlan:ICorrelatedMessage
    {
        public string Message { get; set; }
        public Guid CorrelationId { get; set; }
    }

    [Serializable]
    public class FromSteve : ICorrelatedMessage
    {
        public string Response { get; set; }
        public Guid CorrelationId { get; set; }
    }

    [Serializable]
    public class SteveNotification : ICorrelatedMessage
    {
        public string Message { get; set; }
        public Guid CorrelationId { get; set; }
    }
    [Serializable]
    public class AlanNotification : ICorrelatedMessage
    {
        public string Message { get; set; }
        public Guid CorrelationId { get; set; }
    }
    
}
