using System.Text.Json;
using WeReadTool.Agents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ray.Infrastructure.AutoTask;
using Volo.Abp.DependencyInjection;
using Microsoft.Playwright;

namespace WeReadTool.AppService;

[AutoTask("Login", "扫码登录")]
public class LoginService : ITransientDependency, IAutoTaskService
{
    private readonly IConfiguration _configuration;
    private readonly IIkuuuApi _hostlocApi;
    private readonly ILogger<LoginService> _logger;
    private readonly TargetAccountInfo _targetAccount;

    public LoginService(
        IConfiguration configuration,
        IIkuuuApi hostlocApi,
        TargetAccountManager<TargetAccountInfo> targetAccountManager,
        ILogger<LoginService> logger
        )
    {
        _configuration = configuration;
        _hostlocApi = hostlocApi;
        _logger = logger;
        _targetAccount = targetAccountManager.CurrentTargetAccount;
    }


    public async Task DoAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("开始扫码登录");
        using var playwright = await Playwright.CreateAsync();

        _logger.LogInformation("打开浏览器");
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
        });

        // Create a new context with the saved storage state.
        _logger.LogInformation("加载上下文");
        var context = await browser.NewContextAsync(new()
        {
            StorageStatePath = ".playwright/.auth/state.json"
        });

        _logger.LogInformation("初始化页面");
        var page = await context.NewPageAsync();

        _logger.LogInformation("打开微信读书首页");
        await page.GotoAsync("https://weread.qq.com/");

        var loginButtonLocator = page.GetByRole(AriaRole.Button, new() { Name = "登录" });

        if (await loginButtonLocator.CountAsync() > 0)
        {
            _logger.LogInformation("检测到未登录，或登录过期，开始扫码登录");

            await loginButtonLocator.ClickAsync();

            await page.ScreenshotAsync(new()
            {
                Path = "screenshots/unlogin.png",
            });

            var qrCodeLocator = page.GetByAltText("扫码登录");
            var picBase64 = await qrCodeLocator.GetAttributeAsync("src");
            _logger.LogInformation(picBase64);
            _logger.LogInformation("请复制以上base64字符串到浏览器地址栏，查看并扫描二维码");

            var maxTry = 3;
            var currentTry = 0;
            var loginSuccess = false;
            while (currentTry < maxTry)
            {
                currentTry++;
                _logger.LogInformation("[{time}]等待扫码...", currentTry);

                Thread.Sleep(10000);

                var refreshLocator = page.GetByRole(AriaRole.Button, new() { Name = "点击刷新二维码" });
                if (await refreshLocator.CountAsync() > 0)
                {
                    await refreshLocator.ClickAsync();

                    qrCodeLocator = page.GetByAltText("扫码登录");
                    picBase64 = await qrCodeLocator.GetAttributeAsync("src");
                    _logger.LogInformation("二维码已刷新：");
                    _logger.LogInformation(picBase64);

                    continue;
                }

                if (await page.GetByAltText("扫码登录").CountAsync() == 0
                    && await page.Locator(".wr_avatar_img").CountAsync() > 0)
                {
                    loginSuccess = true;
                    _logger.LogInformation("扫码登录成功！");
                    await page.ScreenshotAsync(new()
                    {
                        Path = "screenshots/already_login.png",
                    });
                    break;
                };
            }

            if (!loginSuccess)
            {
                throw new Exception("登录失败！");
            }
        }

        // Save storage state into the file.
        await context.StorageStateAsync(new()
        {
            Path = ".playwright/.auth/state.json"
        });
    }
}
