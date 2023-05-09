using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Ray.Infrastructure.AutoTask;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WeReadTool.Configs;
using System.Security.Principal;
using System.Text.RegularExpressions;

namespace WeReadTool.AppService
{
    [AutoTask("Read", "每日阅读")]
    public class ReadService : IAutoTaskService
    {
        private readonly ILogger<ReadService> _logger;
        private readonly IConfiguration _config;
        private readonly ReadOptions _readOptions;

        public ReadService(
            ILogger<ReadService> logger, 
            IOptions<ReadOptions> readOptions,
            IConfiguration config
            )
        {
            _logger = logger;
            _config = config;
            _readOptions = readOptions.Value;
        }

        public async Task DoAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            var accounts = _config.GetSection("AccountStates").Get<List<string>>();
            for (int i = 0; i < accounts.Count; i++)
            {
                var account = accounts[i];
                await DoForAccountAsync(account, cancellationToken);
            }
        }

        private async Task DoForAccountAsync(string account, CancellationToken cancellationToken)
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
#if DEBUG
                Headless = false,
#else
                Headless = true,
#endif
            });
            var context = await browser.NewContextAsync();

            await context.Tracing.StartAsync(new()
            {
                Screenshots = true,
                Snapshots = true,
                Sources = true
            });

            //加载状态
            var cookies = (JArray)JsonConvert.DeserializeObject<JObject>(account)["cookies"];
            await context.AddCookiesAsync(cookies.ToObject<List<Cookie>>());

            var page = await context.NewPageAsync();

            try
            {
                await ReadAsync(page);
            }
            finally
            {
                await context.Tracing.StopAsync(new()
                {
                    Path = "trace.zip"
                });
            }
        }

        private async Task ReadAsync(IPage page)
        {
            _logger.LogInformation("进入首页");
            await page.GotoAsync("https://weread.qq.com/");

            // 验证登录状态
            var loginButtonLocator = page.GetByRole(AriaRole.Button, new() { Name = "登录" });
            if (await loginButtonLocator.CountAsync() > 0)
            {
                throw new Exception("未登录，请先进行扫码登录任务");
            }

            // 搜索目标书籍
            _logger.LogInformation("开始搜索");
            await page.Locator(".navBar_home_inputText").ClickAsync();
            Thread.Sleep(2000);

            _logger.LogInformation("输入：{bookName}", _readOptions.BookName);
            var searchLocator = page.Locator(".search_input_text");
            await searchLocator.FillAsync(_readOptions.BookName);
            await searchLocator.PressAsync("Enter");
            Thread.Sleep(2000);

            // 从结果集选择第一个匹配项
            _logger.LogInformation("选择搜索结果里的第一个");
            await page.Locator(".search_result_global_bookLink").First.ClickAsync();
            Thread.Sleep(2000);

            //开始阅读
            await page.GetByRole(AriaRole.Button, new() { Name = "开始阅读" }).ClickAsync(); ;
            Thread.Sleep(2000);

            //循环翻页（翻章）
            var maxTry = _readOptions.ChapterCount;
            var currentTry = 0;
            while (currentTry < maxTry)
            {
                currentTry++;
                _logger.LogInformation("阅读第{count}个章节", currentTry);

                var random = new Random().Next(_readOptions.DurationPerChapter - 30, _readOptions.DurationPerChapter + 30);
                _logger.LogInformation("开始阅读{min}分{sec}秒", random / 60, random % 60);
                Thread.Sleep(random * 1000);

                _logger.LogInformation("下一章{newLinew}", Environment.NewLine);
                await page.GetByRole(AriaRole.Button, new() { Name = "下一章" }).ClickAsync();
                //todo:需要考虑没有下一章的情况
            }
        }
    }
}
