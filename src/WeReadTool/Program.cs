using WeReadTool.Agents;
using WeReadTool.AppService;
using WeReadTool.Configs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ray.Infrastructure.AutoTask;
using Ray.Infrastructure.Http;
using Ray.Serilog.Sinks.PushPlusBatched;
using Ray.Serilog.Sinks.ServerChanBatched;
using Ray.Serilog.Sinks.TelegramBatched;
using Ray.Serilog.Sinks.WorkWeiXinBatched;
using Refit;
using Serilog;
using Serilog.Events;

namespace WeReadTool;

public class Program
{
    private const string EnvPrefix = "WeReadTool_";

    public static async Task<int> Main(string[] args)
    {
        var exitCode = Microsoft.Playwright.Program.Main(new string[] { "install", "--with-deps", "chromium" });
        if (exitCode != 0)
        {
            throw new Exception($"Playwright exited with code {exitCode}");
        }

        Log.Logger = CreateLogger(args);
        try
        {
            Log.Logger.Information("Starting console host.");

            await Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostBuilderContext, configurationBuilder) =>
                {
                    IList<IConfigurationSource> list = configurationBuilder.Sources;
                    list.ReplaceWhile(
                        configurationSource => configurationSource is EnvironmentVariablesConfigurationSource,
                        new EnvironmentVariablesConfigurationSource()
                        {
                            Prefix = EnvPrefix
                        }
                    );
                })
                .ConfigureServices(RegisterServices)
                .ConfigureServices((hostBuilderContext, services) =>
                {
                    var list = services.Where(x => x.ServiceType == typeof(IAutoTaskService))
                        .Select(x=>x.ImplementationType)
                        .ToList();
                    var autoTaskTypeFactory = new AutoTaskTypeFactory(list);
                    services.AddSingleton(autoTaskTypeFactory);
                })
                .UseSerilog()
                .RunConsoleAsync();

            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly!");
            Log.Logger.Fatal("·开始推送·{task}·{user}", "任务异常", "");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static ILogger CreateLogger(string[] args)
    {
        var hb = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostBuilderContext, configurationBuilder) =>
            {
                IList<IConfigurationSource> list = configurationBuilder.Sources;
                list.ReplaceWhile(
                    configurationSource => configurationSource is EnvironmentVariablesConfigurationSource,
                    new EnvironmentVariablesConfigurationSource()
                    {
                        Prefix = EnvPrefix
                    }
                );
            });
        var tempHost = hb.Build();
        var config = tempHost.Services.GetRequiredService<IConfiguration>();

        return new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Async(c =>
            {
                c.File($"Logs/{DateTime.Now.ToString("yyyy-MM-dd")}/{DateTime.Now.ToString("HH-mm-ss")}.txt",
                    restrictedToMinimumLevel: LogEventLevel.Verbose);
            })
            .WriteTo.Console()
            .WriteTo.PushPlusBatched(
                config["Notify:PushPlus:Token"],
                config["Notify:PushPlus:Channel"],
                config["Notify:PushPlus:Topic"],
                config["Notify:PushPlus:Webhook"],
                restrictedToMinimumLevel: LogEventLevel.Information
            )
            .WriteTo.TelegramBatched(
                config["Notify:Telegram:BotToken"],
                config["Notify:Telegram:ChatId"],
                config["Notify:Telegram:Proxy"],
                restrictedToMinimumLevel: LogEventLevel.Information
            )
            .WriteTo.ServerChanBatched(
                "",
                turboScKey: config["Notify:ServerChan:TurboScKey"],
                restrictedToMinimumLevel: LogEventLevel.Information
            )
            .WriteTo.WorkWeiXinBatched(
                config["Notify:WorkWeiXin:WebHookUrl"],
                restrictedToMinimumLevel: LogEventLevel.Information
            )
            .CreateLogger();
    }

    private static void RegisterServices(HostBuilderContext hostBuilderContext, IServiceCollection services)
    {
        var config = (IConfigurationRoot)hostBuilderContext.Configuration;

        services.AddHostedService<MyHostedService>();

        services.AddSingleton(typeof(TargetAccountManager<>));

        #region config
        services.Configure<List<AccountOptions>>(config.GetSection("Accounts"));
        services.Configure<HttpClientCustomOptions>(config.GetSection("HttpCustomConfig"));
        #endregion

        #region Api
        services.AddSingleton<TargetAccountManager<TargetAccountInfo>>();

        services.AddTransient<DelayHttpMessageHandler>();
        services.AddTransient<LogHttpMessageHandler>();
        services.AddTransient<ProxyHttpClientHandler>();
        services.AddTransient<CookieHttpClientHandler<TargetAccountInfo>>();
        services
            .AddRefitClient<IIkuuuApi>()
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri("https://ikuuu.eu");

                var ua = config["UserAgent"];
                if (!string.IsNullOrWhiteSpace(ua))
                    c.DefaultRequestHeaders.UserAgent.ParseAdd(ua);
            })
            .AddHttpMessageHandler<DelayHttpMessageHandler>()
            .AddHttpMessageHandler<LogHttpMessageHandler>()
            .ConfigurePrimaryHttpMessageHandler<ProxyHttpClientHandler>()
            .ConfigurePrimaryHttpMessageHandler<CookieHttpClientHandler<TargetAccountInfo>>()
            ;
        #endregion

        services.Scan(scan => scan
            .FromAssemblyOf<Program>()
            .AddClasses(classes => classes.AssignableTo<IAutoTaskService>())
            .AsImplementedInterfaces()
            .AsSelf()
            .WithTransientLifetime()
        );
    }
}