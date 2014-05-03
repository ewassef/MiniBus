using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortBus.Hostable.Shared.Specialized
{
    [Serializable]
    public class PerformanceUpdate
    {
        public DateTime UpdateTimeUtc { get; set; }
        public List<Entry> Counters { get; set; }
    }
    [Serializable]
    public class Entry
    {
        public string Context { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
