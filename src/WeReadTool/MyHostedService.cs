using WeReadTool.AppService;
using WeReadTool.Configs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.Infrastructure.AutoTask;
using System.ComponentModel;
using System;
using System.Reflection;
using System.Threading;

namespace WeReadTool;

public class MyHostedService : IHostedService
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly ILogger<MyHostedService> _logger;
    private readonly TargetAccountManager<TargetAccountInfo> _accountManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly AutoTaskTypeFactory _autoTaskTypeFactory;
    private readonly List<AccountOptions> _accountOptions;

    public MyHostedService(
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        IHostApplicationLifetime hostApplicationLifetime,
        ILogger<MyHostedService> logger,
        IOptions<List<AccountOptions>> accountOptions,
        TargetAccountManager<TargetAccountInfo> targetAccountManager,
        IServiceProvider serviceProvider,
        AutoTaskTypeFactory autoTaskTypeFactory
    )
    {
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
        _hostApplicationLifetime = hostApplicationLifetime;
        _logger = logger;
        _accountManager = targetAccountManager;
        _serviceProvider = serviceProvider;
        _autoTaskTypeFactory = autoTaskTypeFactory;
        _accountOptions = accountOptions.Value;
        _accountManager.Init(_accountOptions.Select(x => new TargetAccountInfo(x.Email, x.Pwd)).ToList());
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_accountManager.Count <= 0)
        {
            _logger.LogWarning("一个账号没配你运行个卵");
            return;
        }

        for (int i = 0; i < _accountManager.Count; i++)
        {
            _accountManager.Index = i;
            var currentAccount = _accountManager.CurrentTargetAccount;
            _logger.LogInformation("========账号{count}========", i + 1);
            _logger.LogInformation("用户名：{userName}", currentAccount.UserName);

            await DoTaskAsync(cancellationToken);

            _logger.LogInformation("========账号{count}结束========{newLine}", i + 1, Environment.NewLine);

            _logger.LogInformation("·开始推送·{task}·{user}", $"{_configuration["Run"]}任务", currentAccount.UserName);
        }
        _hostApplicationLifetime.StopApplication();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
    }

    private async Task DoTaskAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        //var helloWorldService = scope.ServiceProvider.GetRequiredService<HelloWorldService>();
        //await helloWorldService.SayHelloAsync(cancellationToken);

        var run = _configuration["Run"];

        var autoTaskInfo = _autoTaskTypeFactory.GetByCode(run);

        while (autoTaskInfo == null)
        {
            _logger.LogInformation("未指定目标任务，请选择要运行的任务：");
            _autoTaskTypeFactory.Show(_logger);
            _logger.LogInformation("请输入：");

            var index = System.Console.ReadLine();
            var suc = int.TryParse(index, out int num);
            if (!suc)
            {
                _logger.LogWarning("输入异常，请输入序号");
                continue;
            }

            autoTaskInfo = _autoTaskTypeFactory.GetByIndex(num);
        }

        _logger.LogInformation("目标任务：{run}", autoTaskInfo.ToString());
        var service = (IAutoTaskService)scope.ServiceProvider.GetRequiredService(autoTaskInfo.ImplementType);
        await service.DoAsync(cancellationToken);
    }
}
