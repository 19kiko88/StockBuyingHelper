using Coravel.Invocable;
using StockBuyingHelper.Service.Interfaces;

namespace StockBuingHelper.Web.Tasks
{
    public class RefreshStockListTask: IInvocable
    {
        private readonly ILogger<RefreshStockListTask> _logger;
        private readonly IAdminService _admin;

        public RefreshStockListTask(
            ILogger<RefreshStockListTask> logger,
            IAdminService admin
            )
        {
            _logger = logger;
            _admin = admin;
        }

        public Task Invoke()
        {
            _logger.LogInformation($"Task [RefreshStockList] running at: {DateTime.Now}");
            try
            {
                _admin.RefreshStockList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return Task.CompletedTask;
        }
    }
}
