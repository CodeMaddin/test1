using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using System.Diagnostics;
using OpenQA.Selenium.Support.Extensions;

internal class ScrapingService
{
    internal static void SaveErrorFiles(WebDriver driver, DateTime? time = null)
    {
        if (time is null)
            time = DateTime.Now;

        // Save html to file
        if (Directory.Exists("log") == false)
            Directory.CreateDirectory("log");

        var timestamp = time.Value.ToString("yyyy-MM-dd_HH-mm-ss");

        Console.WriteLine($"Saving copy of web page html to: \"err_webpage_{timestamp}.html\"");
        File.WriteAllText($"log/err_webpage_{timestamp}.html", driver.PageSource);

        // Take a screenshot of the page
        Console.WriteLine($"Saving screenshot of web page to: \"err_screenshot_{timestamp}.png\"");
        driver.TakeScreenshot().SaveAsFile($"log/err_screenshot_{timestamp}.png");
    }

    internal static WebDriver CreateDefaultWebDriver()
    {
        //If we are running on the server, there shouldn't be any chrome processes running
        bool isServer = Environment.GetEnvironmentVariable("OS") is not string s || !s.Contains("Windows", StringComparison.OrdinalIgnoreCase);

        if (isServer)
        {
            // Kill any existing chromedriver processes on the server, if they exist
            Console.WriteLine("Running on server, checking for existing chromedriver processes");
            var chromeDriverProcesses = Process.GetProcesses().Where(p => p.ProcessName.StartsWith("chrome")).ToList();
            if (chromeDriverProcesses.Count > 0)
            {
                Console.WriteLine("Found existing chromedriver processes... killing");
                foreach (var process in Process.GetProcesses())
                {
                    Console.Write($"{process.StartTime} - #{process.Id} - {process.ProcessName}");
                    if (process.ProcessName.StartsWith("chrome"))
                    {
                        Console.Write($" -- ## KILLING ##");
                        process.Kill();
                    }
                    Console.WriteLine();
                }
                Console.WriteLine("Done");
            }
        }

        // Set up the Chrome driver options
        var options = new ChromeOptions();
        // Prevet bot detection
        options.AddArgument("user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36");

        if (isServer)
        {
            options.AddArgument("loadImages=false");
            options.AddArgument("--headless"); // If you don't need the GUI

            options.AddArgument("--no-sandbox"); // This is often necessary for running in environments like containers or VMs
            options.AddArgument("--disable-dev-shm-usage"); // Overcome limited resource problems in some environments
            options.AddArgument("--disable-gpu"); // Applicable mainly for Windows, but can be useful in other scenarios too
            options.AddArgument("--remote-debugging-port=9222"); // Helps in avoiding some issues related to DevTools
        }

        var driverService = ChromeDriverService.CreateDefaultService();
        driverService.HideCommandPromptWindow = true;

        var driver = new ChromeDriver(driverService, options);
        return driver;
    }

}