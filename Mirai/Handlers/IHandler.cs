using System.Threading.Tasks;

namespace Mirai.Handlers
{
    interface IHandler
    {
        Task Tick();
        Task Save();
    }
}
