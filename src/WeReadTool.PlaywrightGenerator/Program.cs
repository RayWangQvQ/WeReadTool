namespace WeReadTool.PlaywrightGenerator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            Microsoft.Playwright.Program.Main(new string[] { "codegen", "weread.qq.com" });
        }
    }
}