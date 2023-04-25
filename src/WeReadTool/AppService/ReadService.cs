using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Ray.Infrastructure.AutoTask;
using Microsoft.Extensions.Options;
using WeReadTool.Configs;

namespace WeReadTool.AppService
{
    [AutoTask("Read", "每日阅读")]
    public class ReadService : IAutoTaskService
    {
        private readonly ILogger<ReadService> _logger;
        private readonly ReadOptions _readOptions;

        public ReadService(ILogger<ReadService> logger, IOptions<ReadOptions> readOptions)
        {
            _logger = logger;
            _readOptions = readOptions.Value;
        }

        public async Task DoAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
            });
            var context = await browser.NewContextAsync(new()
            {
                StorageStatePath = File.Exists(".playwright/.auth/state.json") ? ".playwright/.auth/state.json" : null
            });

            var page = await context.NewPageAsync();

            await page.GotoAsync("https://weread.qq.com/");

            await page.GetByRole(AriaRole.Link, new() { Name = "查看我的书架" }).ClickAsync();
            Thread.Sleep(2000);

            await page.GetByPlaceholder("搜索").ClickAsync();
            Thread.Sleep(2000);

            await page.Locator("div").Filter(new() { HasText = "关闭取消" }).GetByPlaceholder("搜索").FillAsync("鲁迅全集");
            Thread.Sleep(2000);

            await page.GetByText("鲁迅全集（全20卷）").ClickAsync();
            Thread.Sleep(2000);

            await page.GetByRole(AriaRole.Link, new PageGetByRoleOptions() { Name = "鲁迅全集（全20卷）" }).First.ClickAsync();
            Thread.Sleep(2000);

            await page.GetByRole(AriaRole.Link, new() { Name = "开始阅读本书" }).ClickAsync();
            Thread.Sleep(2000);

            var maxTry = _readOptions.ChapterCount;
            var currentTry = 0;
            while (currentTry < maxTry)
            {
                currentTry++;
                _logger.LogInformation("阅读第{count}个章节", currentTry);

                //_logger.LogInformation("读60秒");
                //Thread.Sleep(60 * 1000);

                //_logger.LogInformation("看笔记30秒");
                //await page.GetByRole(AriaRole.Button, new() { Name = "笔记" }).ClickAsync();
                //Thread.Sleep(30 * 1000);
                //await page.GetByRole(AriaRole.Button, new() { Name = "笔记" }).ClickAsync();

                var random = new Random().Next(_readOptions.DurationPerChapter - 30, _readOptions.DurationPerChapter + 30);
                _logger.LogInformation("阅读{random}秒", random);
                Thread.Sleep(random * 1000);

                _logger.LogInformation("下一章{newLinew}", Environment.NewLine);
                await page.GetByRole(AriaRole.Button, new() { Name = "下一章" }).ClickAsync();
            }
        }
    }
}
