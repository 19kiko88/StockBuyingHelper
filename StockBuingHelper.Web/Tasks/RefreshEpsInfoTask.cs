using Coravel.Invocable;
using Microsoft.Extensions.Options;
using StockBuyingHelper.Models;
using StockBuyingHelper.Service.Interfaces;

namespace StockBuingHelper.Web.Tasks
{
    public class RefreshEpsInfoTask: IInvocable
    {
        private readonly ILogger<RefreshEpsInfoTask> _logger;
        private readonly IAdminService _admin;
        private readonly AppSettings.CustomizeSettings _appCustSettings;

        public RefreshEpsInfoTask(
            ILogger<RefreshEpsInfoTask> logger,
            IAdminService admin,
            IOptions<AppSettings.CustomizeSettings> appCustSettings
            )
        {
            _logger = logger;
            _admin = admin;
            _appCustSettings = appCustSettings.Value;
        }

        public Task Invoke()
        {
            _logger.LogInformation($"Task [RefreshEpsInfo] running at: {DateTime.Now}");
            try
            {
                _admin.RefreshEpsInfo(_appCustSettings.OperationSystem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return Task.CompletedTask;
        }
    }
}
