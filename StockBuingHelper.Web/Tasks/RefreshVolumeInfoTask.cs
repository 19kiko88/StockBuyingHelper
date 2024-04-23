using Coravel.Invocable;
using StockBuyingHelper.Service.Interfaces;

namespace StockBuingHelper.Web.Tasks
{
    public class RefreshVolumeInfoTask: IInvocable
    {
        private readonly ILogger<RefreshVolumeInfoTask> _logger;
        private readonly IAdminService _admin;

        public RefreshVolumeInfoTask(
            ILogger<RefreshVolumeInfoTask> logger,
            IAdminService admin
            )
        {
            _logger = logger;
            _admin = admin;
        }

        public Task Invoke()
        {
            _logger.LogInformation($"Task [RefreshVolumeInfo] running at: {DateTime.Now}");
            try
            {
                _admin.RefreshVolumeInfo();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return Task.CompletedTask;
        }
    }
}
