using System.Threading.Tasks;

namespace Ldv.Scrappy.Bll
{
    public interface INotifier
    {
        Task Send(Rule rule, string oldData, string newData);
    }
}