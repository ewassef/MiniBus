using System.Text;
using System.Threading.Tasks;
using MassTransit.Distributor;

namespace ShortBus.MassTransit.CacheSelectorFactory
{
    public class EuclidSelectorFactory : IWorkerSelectorFactory
    {
        public IWorkerSelector<TMessage> GetSelector<TMessage>() where TMessage : class
        {
            return new EuclidSelector<TMessage>();
        }
    }
}
