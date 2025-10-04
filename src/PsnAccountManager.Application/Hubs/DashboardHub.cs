using Microsoft.AspNetCore.SignalR;
using PsnAccountManager.Shared.ViewModels;
using System.Threading.Tasks;

namespace PsnAccountManager.Application.Hubs 
{
    public class DashboardHub : Hub
    {
        public async Task SendStatusUpdate(WorkerStatusViewModel status)
        {
            if (Clients != null)
            {
                await Clients.All.SendAsync("ReceiveStatusUpdate", status);
            }
        }
    }
}