using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortBus.Hostable.Shared.Interface
{


    /// <summary>
    /// Use this interface on your messages if you want to correlate between requests and replies
    /// </summary>
    public interface ICorrelatedMessage
    {
        Guid CorrelationId { get; set; }
    }
}
