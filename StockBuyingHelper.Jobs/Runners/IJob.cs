namespace StockBuyingHelper.Jobs.Runners;

public interface IJob
{
    Task RunAsync();
}
