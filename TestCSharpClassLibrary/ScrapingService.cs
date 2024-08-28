using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using System.Diagnostics;
using OpenQA.Selenium.Support.Extensions;
using System.Text.Json;
using HtmlAgilityPack;

internal static class Extensions
{
    public static string BracketText(this HtmlNode node)
        => node?.OuterHtml[..(node.OuterHtml.IndexOf('>') + 1)].Trim() ?? "(null)";

    public static JsonElement EnsureGetProperty(this JsonElement element, string propertyName)
        => ScrapingService.EnsureGetProperty(element, propertyName);

    public static HtmlNode EnsureSelectSingleNode(this HtmlNode node, string featureName, string xpath)
        => ScrapingService.EnsureSelectSingleNode(featureName, node, xpath);
    public static HtmlNodeCollection EnsureSelectNodes(this HtmlNode node, string featureName, string xpath)
        => ScrapingService.EnsureSelectNodes(featureName, node, xpath);
    public static string EnsureGetAttributeValue(this HtmlNode node, string featureName, string arributeName)
        => ScrapingService.EnsureGetAttributeValue(featureName, node, arributeName);
}

internal class ScrapingNodeException(string featureName, HtmlNode? node, string path, string message, Exception? innerException = null) : ScrapingException(featureName, message, innerException)
{
    public HtmlNode? Node { get; } = node;
    public string Path { get; } = path;
    public string NodeFirstLineText => Node?.BracketText() ?? "";
}
internal class ScrapingJsonElementException(string featureName, JsonElement? element, string message, Exception? innerException = null) : ScrapingException(featureName, message, innerException)
{
    public JsonElement? Element { get; } = element;
}
internal class ScrapingException(string featureName, string message, Exception? innerException = null) : Exception(message, innerException)
{
    public string FeatureName { get; } = featureName;
}

internal class ScrapingService
{
    internal static JsonElement EnsureGetProperty(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement property))
        {
            Console.WriteLine($"Property '{propertyName}' not found in JSON element.");
            throw new ScrapingJsonElementException(propertyName, element, $"Property '{propertyName}' not found in JSON element.");
        }
        return property;        
    }

    internal static HtmlNodeCollection EnsureSelectNodes(string featureName, HtmlNode node, string xpath)
    {
        if(node == null)
            throw new ScrapingNodeException(featureName, null, xpath, $"Node is null for feature '{featureName}'", new ArgumentNullException(nameof(node)));

        HtmlNodeCollection? results;
        try
        {
            results = node.SelectNodes(xpath);
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Error, failed to select nodes for feature '{featureName}' using xpath: {xpath} on {node.BracketText()} @ ({node.XPath})");
            throw new ScrapingNodeException(featureName, node, xpath, $"Error, failed to select nodes for feature '{featureName}'", ex);
        }
        if (results == null || results.Count == 0)
        {
            Console.WriteLine($"Error, no nodes found for feature '{featureName}' using xpath: {xpath} on {node.BracketText()} @ ({node.XPath})");
            throw new ScrapingNodeException(featureName, node, xpath, $"No nodes found for feature '{featureName}'");   
        }
        return results;
    }

    internal static HtmlNode EnsureSelectSingleNode(string featureName, HtmlNode node, string xpath)
    {
        if (node == null)
            throw new ScrapingNodeException(featureName, null, xpath, $"Node is null for feature '{featureName}'", new ArgumentNullException(nameof(node)));

        HtmlNode? result;
        try
        {
            result = node.SelectSingleNode(xpath);
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Error, failed to select node for feature '{featureName}' using xpath: {xpath} on {node.BracketText()} @ ({node.XPath})");
            throw new ScrapingNodeException(featureName, node, xpath, $"Failed to select node for feature '{featureName}'", ex);
        }
        if (result == null)
        {
            Console.WriteLine($"Error, no node found for feature '{featureName}' using xpath: {xpath} on {node.BracketText()} @ ({node?.XPath})");
            throw new ScrapingNodeException(featureName, node, xpath, $"No node found for feature '{featureName}'");
        }
        return result;
    }

    internal static string EnsureGetAttributeValue(string featureName, HtmlNode node, string attributeName)
    {
        if (node == null)
            throw new ScrapingNodeException(featureName, null, attributeName, $"Node is null for feature '{featureName}'", new ArgumentNullException(nameof(node)));

        string? attributeValue;
        try
        {
            attributeValue = node.GetAttributeValue(attributeName, null);
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Error, failed to get attribute '{attributeName}' for '{featureName}' on {node.BracketText()} @ ({node.XPath})");
            throw new ScrapingNodeException(featureName, node, attributeName, $"Error, failed to get attribute '{attributeName}' for '{featureName}'", ex);
        }
        if (attributeValue == null)
        {
            Console.WriteLine($"Error, attribute '{attributeName}' not found for '{featureName}' on {node.BracketText()} @ ({node.XPath})");
            throw new ScrapingNodeException(featureName, node, attributeName, $"Error, attribute '{attributeName}' not found for '{featureName}'");
        }
        return attributeValue;
    }

    internal static T? GetCachedResults<T>(string cacheDirectory, string cacheKey, TimeSpan? expiration)
    {
        var cacheFilePath = Path.Combine(cacheDirectory, $"{cacheKey}.json");
        Console.Write($"Checking cache ${cacheKey} @ {cacheFilePath} -- ");
        if (!File.Exists(cacheFilePath))
        {
            Console.WriteLine($"MISS");
            return default;
        }

        var fileInfo = new FileInfo(cacheFilePath);
        if (expiration.HasValue && fileInfo.LastWriteTime.Add(expiration.Value) < DateTime.Now)
        {
            Console.WriteLine($"EXPIRED");
            File.Delete(cacheFilePath);
            return default;
        }

        Console.WriteLine($"HIT");
        var json = File.ReadAllText(cacheFilePath);
        return JsonSerializer.Deserialize<T>(json);
    }

    internal static void CacheResults(string cacheDirectory, string cacheKey, object item)
    {
        Directory.CreateDirectory(cacheDirectory);
        var cacheFilePath = Path.Combine(cacheDirectory, $"{cacheKey}.json");
        Console.Write($"Caching ${cacheKey} @ {cacheFilePath} -- ");
        var json = JsonSerializer.Serialize(item);
        File.WriteAllText(cacheFilePath, json);
        Console.WriteLine($"DONE");
    }

    internal static void SaveErrorFiles(WebDriver driver, DateTime? time = null)
    {
        time ??= DateTime.Now;

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