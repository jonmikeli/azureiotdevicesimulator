using System.Threading.Tasks;

namespace IoT.Simulator.Services
{
    public interface IMessageService
    {
        Task<string> GetMessageAsync();

        Task<string> GetMessageAsync(string deviceId, string moduleId);

        Task<string> GetRandomizedMessageAsync(string deviceId, string moduleId);
    }
}
