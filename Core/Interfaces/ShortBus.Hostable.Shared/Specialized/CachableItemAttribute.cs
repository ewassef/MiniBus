using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortBus.Hostable.Shared.Specialized
{
    [AttributeUsage(AttributeTargets.Class,
                   AllowMultiple = false,
                   Inherited = true)]
    public class CachableItemAttribute :Attribute
    {
        
    }
}
