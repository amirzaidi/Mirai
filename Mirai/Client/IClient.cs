using System.Threading.Tasks;

namespace Mirai.Client
{
    interface IClient
    {
        bool Connected { get; }

        Task Connect();
        Task Disconnect();
        Task<ClientInformation> Info();
    }
}
