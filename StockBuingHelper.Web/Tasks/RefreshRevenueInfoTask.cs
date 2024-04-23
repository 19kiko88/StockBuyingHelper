using Coravel.Invocable;
using StockBuyingHelper.Service.Interfaces;

namespace StockBuingHelper.Web.Tasks
{
    public class RefreshRevenueInfoTask: IInvocable
    {
        private readonly ILogger<RefreshRevenueInfoTask> _logger;
        private readonly IAdminService _admin;

        public RefreshRevenueInfoTask(
            ILogger<RefreshRevenueInfoTask> logger,
            IAdminService admin
            )
        {
            _logger = logger;
            _admin = admin;
        }

        public Task Invoke()
        {
            _logger.LogInformation($"Task [RefreshRevenueInfo] running at: {DateTime.Now}");
            try
            {
                _admin.RefreshRevenueInfo();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return Task.CompletedTask;
        }
    }
}
