using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using StockBuyingHelper.Jobs.Clients;

namespace StockBuyingHelper.Jobs.Runners;

public sealed class Get0050ListRunner : IJob
{
    private const string List0050Endpoint = "Save0050List";

    private readonly VpsUploader _uploader;

    public Get0050ListRunner(VpsUploader uploader)
    {
        _uploader = uploader;
    }

    public async Task RunAsync()
    {
        var options = new ChromeOptions();

        // 不顯示瀏覽器視窗
        options.AddArgument("--headless=new");

        // 視窗最大化
        options.AddArgument("--start-maximized");

        using var driver = new ChromeDriver(options);
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(60));

        // 開啟網站
        driver.Navigate().GoToUrl("https://www.pocket.tw/etf/tw/0050/fundholding");
        wait.Until(ExpectedConditions.ElementExists(By.ClassName("fundholding")));

        var stockIds = driver
            .FindElements(By.CssSelector(".fundholding div.cm-table tbody tr"))
            .Select(row => row.FindElement(By.CssSelector("td")).Text)
            .ToList();

        await _uploader.UploadAsync(List0050Endpoint, stockIds);
    }
}
