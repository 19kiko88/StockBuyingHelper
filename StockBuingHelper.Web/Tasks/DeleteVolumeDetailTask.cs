using Coravel.Invocable;
using StockBuyingHelper.Service.Interfaces;

namespace StockBuingHelper.Web.Tasks
{
    public class DeleteVolumeDetailTask: IInvocable
    {
        private readonly ILogger<DeleteVolumeDetailTask> _logger;
        private readonly IAdminService _admin;

        public DeleteVolumeDetailTask(
            ILogger<DeleteVolumeDetailTask> logger,
            IAdminService admin
            )
        {
            _logger = logger;
            _admin = admin;
        }

        public Task Invoke()
        {
            _logger.LogInformation($"Task [DeleteVolumeDetailTask] running at: {DateTime.Now}");
            try
            {
                _admin.DeleteVolumeDetail();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            
            return Task.CompletedTask;
        }
    }
}
