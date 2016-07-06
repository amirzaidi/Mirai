using System.Threading.Tasks;

namespace Mirai.Client
{
    interface IClient
    {
        bool Connected { get; }
        string Owner { get; set; }

        Task Connect();
        Task Disconnect();
        Task Send(SendMessage Message);
        Task Edit(SendMessage Message);
        Task Delete(SendMessage Message);
        Task Stream(string Chat, byte[] Sound);
        Task<ClientInformation> Info();
        Task UpdateCache();
    }
}
