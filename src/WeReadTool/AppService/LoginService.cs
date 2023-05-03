using Microsoft.Extensions.Logging;
using Ray.Infrastructure.AutoTask;
using Volo.Abp.DependencyInjection;
using Microsoft.Playwright;
using Newtonsoft.Json;
using Ray.Infrastructure.QingLong;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace WeReadTool.AppService;

[AutoTask("Login", "扫码登录")]
public class LoginService : ITransientDependency, IAutoTaskService
{
    private readonly ILogger<LoginService> _logger;
    private readonly IQingLongApi _qingLongApi;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IConfiguration _config;

    public LoginService(
        ILogger<LoginService> logger,
        IQingLongApi qingLongApi,
        IHostEnvironment hostEnvironment,
        IConfiguration config
        )
    {
        _logger = logger;
        _qingLongApi = qingLongApi;
        _hostEnvironment = hostEnvironment;
        _config = config;
    }


    public async Task DoAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("开始扫码登录");
        using var playwright = await Playwright.CreateAsync();

        _logger.LogInformation("打开浏览器");
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
#if DEBUG
            Headless = false,
#else
            Headless = true,
#endif
        });

        // Create a new context with the saved storage state.
        _logger.LogInformation("加载上下文");
        IBrowserContext context = await browser.NewContextAsync();

        _logger.LogInformation("初始化页面");
        var page = await context.NewPageAsync();

        _logger.LogInformation("打开微信读书首页");
        await page.GotoAsync("https://weread.qq.com/");

        var loginButtonLocator = page.GetByRole(AriaRole.Button, new() { Name = "登录" });

        if (await loginButtonLocator.CountAsync() > 0)
        {
            _logger.LogInformation("检测到未登录，或登录过期，开始扫码登录");
            await loginButtonLocator.ClickAsync();
            var qrCodeLocator = page.GetByAltText("扫码登录");

            //识别二维码
            var picBase64 = await qrCodeLocator.GetAttributeAsync("src");
            //_logger.LogInformation(picBase64);
            //_logger.LogInformation("请复制以上base64字符串到浏览器地址栏，查看并扫描二维码");
            ShowQrCode(picBase64);


            //扫描
            var maxTry = 5;
            var currentTry = 0;
            var loginSuccess = false;
            while (currentTry < maxTry)
            {
                currentTry++;
                _logger.LogInformation("[{time}]等待扫码...", currentTry);

                Thread.Sleep(20 * 1000);

                var refreshLocator = page.GetByRole(AriaRole.Button, new() { Name = "点击刷新二维码" });
                if (await refreshLocator.CountAsync() > 0)
                {
                    await refreshLocator.ClickAsync();

                    qrCodeLocator = page.GetByAltText("扫码登录");
                    picBase64 = await qrCodeLocator.GetAttributeAsync("src");
                    _logger.LogInformation("二维码已刷新：");
                    //_logger.LogInformation(picBase64);
                    ShowQrCode(picBase64);

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
        var state = await context.StorageStateAsync();

        if (_config["Platform"]?.ToLower() == "qinglong")
        {
            var wr_gid = GetWrgid(state);
            await QingLongHelper.SaveCookieListItemToQinLongAsync(_qingLongApi, "WeReadTool_AccountStates", state, wr_gid, _logger, cancellationToken);
        }
        else
        {
            SaveCookieToJsonFile(state);
        }
    }

    private void ShowQrCode(string base64Str)
    {
        var text = Ray.Infrastructure.BarCode.BarCodeHelper
            .DecodeByBase64Str(base64Str)
            .ToString();
        var img = Ray.Infrastructure.BarCode.BarCodeHelper.EncodeByImageSharp(text,optionsAction: op =>
        {
            op.Width = 20;
            op.Height = 20;
        });//重新生成，压缩下

        //打印二维码
        if (_config["Platform"]?.ToLower() == "qinglong")
        {
            Ray.Infrastructure.BarCode.BarCodeHelper.PrintSmallQrCode(img,
                onRowPrintProcess: s => _logger.LogInformation(s));
        }
        else
        {
            Ray.Infrastructure.BarCode.BarCodeHelper.PrintQrCode(img,
                onRowPrintProcess: s => _logger.LogInformation(s));
        }
        img.Dispose();
        _logger.LogInformation("若显示异常，请访问在线版扫描：{qrcode}", GetOnlinePic(text));
    }

    private void SaveCookieToJsonFile(string stateJson)
    {
        //读取wr_gid
        var wr_gid = GetWrgid(stateJson);

        var pl = _hostEnvironment.ContentRootPath.Split("bin").ToList();
        pl.RemoveAt(pl.Count - 1);
        var path = Path.Combine(string.Join("bin", pl), "account.json");

        if (!File.Exists(path))
        {
            File.Create(path);
            File.WriteAllText(path, "{\"AccountStates\":[]}");
        }

        var jsonStr = File.ReadAllText(path);

        dynamic jsonObj = JsonConvert.DeserializeObject(jsonStr);
        var accounts = (JArray)jsonObj["AccountStates"];

        int index = accounts.IndexOf(accounts.FirstOrDefault(x => x.ToString().Contains(wr_gid)));

        if (index >= 0)
        {
            jsonObj["AccountStates"][index] = stateJson;
        }
        else
        {
            jsonObj["AccountStates"].Add(stateJson);
        }

        string output = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
        File.WriteAllText(path, output);
    }

    private string GetWrgid(string stateJson)
    {
        dynamic stateObj = JsonConvert.DeserializeObject(stateJson);
        var ckList = (JArray)stateObj["cookies"];
        var ck = ckList.FirstOrDefault(x => x["name"].ToString() == "wr_gid");
        var wr_gid = ck["value"].ToString();
        return wr_gid;
    }

    private string GetOnlinePic(string str)
    {
        var encode = System.Web.HttpUtility.UrlEncode(str); ;
        return $"https://tool.lu/qrcode/basic.html?text={encode}";
    }
}
