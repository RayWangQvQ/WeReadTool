using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Ray.Infrastructure.AutoTask;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeReadTool.AppService
{
    [AutoTask("Read", "阅读")]
    public class ReadService : IAutoTaskService
    {
        private readonly ILogger<ReadService> _logger;

        public ReadService(ILogger<ReadService> logger)
        {
            _logger = logger;
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
                StorageStatePath = ".playwright/.auth/state.json"
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
            //await page.Locator("div").Filter(new() { HasText = "关闭取消鲁迅鲁迅：呐喊（中国现代文学经典文库）故乡：鲁迅精读故乡：鲁迅小说集（青春定制版）彷徨（鲁迅作品精选）彷徨：鲁迅作品精选（感悟文学大师经典）野草：鲁迅作" }).Locator("span").Nth(1).ClickAsync();
            //await page.GetByRole(AriaRole.Link, new() { Name = "书籍封面 鲁迅全集（全20卷） 鲁迅 唯一无删改、原汁原味鲁迅的文字，市面上最通俗好读的鲁迅版本！1936年10月，鲁迅先生在上海逝世。鲁迅先生纪念委员会为\"扩大鲁迅精神的影响，以唤醒国魂，争取光明\"编印了鲁迅逝后第一版《鲁迅全集》。《全集》由蔡元培任主席的鲁迅先生纪念委员会负责编校，编辑委员有蔡元培、马裕藻、沈兼士、茅盾、周作人诸先生。《全集》总目以鲁迅亲定的著述目录为基础，增加了译作部分，并力求各册字数大致相当。全书大致分创作、古籍校辑、译作三大部分。各部分内容按时间先后排序。全书总计六百余万字，共分二十卷。于1938年6月正式出版并发行。鲁迅全集（第一卷）-坟、呐喊、野草；鲁迅全集（第二卷）-热风、彷徨、朝花夕拾、故事新编；鲁迅全集（第三卷）-华盖集、华盖集续编、华盖集续编的续编、而已集；鲁迅全集（第四卷）-三闲集、二心集、伪自由书；’鲁迅全集（第五卷）-南腔北调集、准风月谈、花边文学；鲁迅全集（第六卷）-且介亭杂文、且介亭杂文二编、且介亭杂文续编；鲁迅全集（第七卷）-致许广平书信集、集外集、集外集拾遗；鲁迅全集（第八卷）-会稽郡故书杂集、古小说钩沉；鲁迅全集（第九卷）-嵇康集、中国小说史略、后记；鲁迅全集（第十卷）-小说旧闻钞、唐宋传奇集、汉文学史纲要；鲁迅全集（第十一卷）-月界旅行、地底旅行、域外小说集、现代小说译丛、现代日本小说集、工人绥惠略夫；鲁迅全集（第十二卷）-一个青年的梦、爱罗先珂童话集、桃色的云；鲁迅全集（第十三卷）-苦闷的象征、出了象牙之塔、思想·山水·人物；鲁迅全集（第十四卷）-小约翰、表、俄罗斯的童话、药用植物；鲁迅全集（第十五卷）-近代美术史潮论、艺术论；鲁迅全集（第十六卷）-壁下译丛、译丛补；鲁迅全集（第十七卷）-艺术论、现代新兴文学的诸问题、文艺与批评、文艺政策；鲁迅全集（第十八卷）-十月、毁灭、山民牧唱、坏孩子和别的奇闻；鲁迅全集（第十九卷）-竖琴、一天的工作；鲁迅全集（第二十卷）-死魂灵。" }).ClickAsync();

            await page.GetByRole(AriaRole.Link, new PageGetByRoleOptions() { Name = "鲁迅全集（全20卷）" }).First.ClickAsync();
            Thread.Sleep(2000);

            await page.GetByRole(AriaRole.Link, new() { Name = "开始阅读本书" }).ClickAsync();
            Thread.Sleep(2000);

            var maxTry = 2;
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

                var random = new Random().Next(120, 240);
                _logger.LogInformation("阅读{random}秒", random);
                Thread.Sleep(random * 1000);

                _logger.LogInformation("下一章");
                await page.GetByRole(AriaRole.Button, new() { Name = "下一章" }).ClickAsync();
            }
        }
    }
}
