using System;

namespace Language
{
    [Serializable]
    public class FromAlan 
    {
        public string Message { get; set; }
        public Guid CorrelationId { get; set; }
    }

    [Serializable]
    public class FromSteve  
    {
        public string Response { get; set; }
        public Guid CorrelationId { get; set; }
    }

    [Serializable]
    public class SteveNotification  
    {
        public string Message { get; set; }
        public Guid CorrelationId { get; set; }
    }
    [Serializable]
    public class AlanNotification  
    {
        public string Message { get; set; }
        public Guid CorrelationId { get; set; }
    }
    
}
