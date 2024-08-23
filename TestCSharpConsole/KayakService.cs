using System.Diagnostics;
using System.Web;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

internal class KayakFlight
{
    public string? OriginItaCode { get; set; }
    public string? DestinationItaCode { get; set; }
    public DateTimeRange When { get; set; }
    public string? CarrierName { get; set;}
    public decimal TotalPrice { get; set; }
    public string? Url { get; set; }

    public string Provider => "Kayak.com";
    public string Title => $"Flight from {OriginItaCode} to {DestinationItaCode}";
}
    
internal static class KayakService
{
    public static void GetSearchResults(string originItaCode, string destinationItaCode, DateOnly departureDate)
    {
        Console.WriteLine("Scraping search results from Kayak.com...");
        var url = BuildFlightSearchUrl(originItaCode, destinationItaCode, departureDate);

        // Use Selenium to scrape the search results
        var driver =  CreateDefaultWebDriver();
        Console.WriteLine($"Navigating to URL: {url}");
        driver.Navigate().GoToUrl(url);

        // Save html to file
        Console.WriteLine("Saving search results to search-results.html");  
        File.WriteAllText("search-results.html", driver.PageSource);    


        // Wait for page to load
        Console.Write("Waiting for page load...");
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(25));
        wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));
        Console.WriteLine(" done");
        
        Console.Write("Waiting for search results to load...");
        Thread.Sleep(TimeSpan.FromSeconds(5));
 
        Console.Write(" (go away spinner)...");
        wait.Until(d => !d.FindElements(By.ClassName("bE-8-spinner")).Any());
        Console.WriteLine("done");

        // Save html to file
        Console.WriteLine("Saving search results to search-results.html");  
        var html = driver.PageSource;
        File.WriteAllText("search-results.html", html);    


        //#leftRail > div > div.e_0j-results-count > div > div.bE-8-spinner
        //<div class="bE-8-spinner"><div class="LJld LJld-mod-theme-default" role="progressbar"><svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 100 100" preserveAspectRatio="xMidYMid"><g transform="rotate(0 50 50)"><rect x="45" y="0" rx="4.5" ry="4.5" width="9" height="28" fill="currentColor"><animate attributeName="opacity" values="1;0" keyTimes="0;1" dur="1s" begin="-0.916s" repeatCount="indefinite"></animate></rect></g><g transform="rotate(30 50 50)"><rect x="45" y="0" rx="4.5" ry="4.5" width="9" height="28" fill="currentColor"><animate attributeName="opacity" values="1;0" keyTimes="0;1" dur="1s" begin="-0.83s" repeatCount="indefinite"></animate></rect></g><g transform="rotate(60 50 50)"><rect x="45" y="0" rx="4.5" ry="4.5" width="9" height="28" fill="currentColor"><animate attributeName="opacity" values="1;0" keyTimes="0;1" dur="1s" begin="-0.75s" repeatCount="indefinite"></animate></rect></g><g transform="rotate(90 50 50)"><rect x="45" y="0" rx="4.5" ry="4.5" width="9" height="28" fill="currentColor"><animate attributeName="opacity" values="1;0" keyTimes="0;1" dur="1s" begin="-0.666s" repeatCount="indefinite"></animate></rect></g><g transform="rotate(120 50 50)"><rect x="45" y="0" rx="4.5" ry="4.5" width="9" height="28" fill="currentColor"><animate attributeName="opacity" values="1;0" keyTimes="0;1" dur="1s" begin="-0.583s" repeatCount="indefinite"></animate></rect></g><g transform="rotate(150 50 50)"><rect x="45" y="0" rx="4.5" ry="4.5" width="9" height="28" fill="currentColor"><animate attributeName="opacity" values="1;0" keyTimes="0;1" dur="1s" begin="-0.5s" repeatCount="indefinite"></animate></rect></g><g transform="rotate(180 50 50)"><rect x="45" y="0" rx="4.5" ry="4.5" width="9" height="28" fill="currentColor"><animate attributeName="opacity" values="1;0" keyTimes="0;1" dur="1s" begin="-0.416s" repeatCount="indefinite"></animate></rect></g><g transform="rotate(210 50 50)"><rect x="45" y="0" rx="4.5" ry="4.5" width="9" height="28" fill="currentColor"><animate attributeName="opacity" values="1;0" keyTimes="0;1" dur="1s" begin="-0.333s" repeatCount="indefinite"></animate></rect></g><g transform="rotate(240 50 50)"><rect x="45" y="0" rx="4.5" ry="4.5" width="9" height="28" fill="currentColor"><animate attributeName="opacity" values="1;0" keyTimes="0;1" dur="1s" begin="-0.25s" repeatCount="indefinite"></animate></rect></g><g transform="rotate(270 50 50)"><rect x="45" y="0" rx="4.5" ry="4.5" width="9" height="28" fill="currentColor"><animate attributeName="opacity" values="1;0" keyTimes="0;1" dur="1s" begin="-0.166s" repeatCount="indefinite"></animate></rect></g><g transform="rotate(300 50 50)"><rect x="45" y="0" rx="4.5" ry="4.5" width="9" height="28" fill="currentColor"><animate attributeName="opacity" values="1;0" keyTimes="0;1" dur="1s" begin="-0.083s" repeatCount="indefinite"></animate></rect></g><g transform="rotate(330 50 50)"><rect x="45" y="0" rx="4.5" ry="4.5" width="9" height="28" fill="currentColor"><animate attributeName="opacity" values="1;0" keyTimes="0;1" dur="1s" begin="0s" repeatCount="indefinite"></animate></rect></g></svg></div></div>
        

    }

    private static OpenQA.Selenium.WebDriver CreateDefaultWebDriver()
    {
        // Kill any existing chromedriver processes if they exist
        var chromeDriverProcesses = Process.GetProcesses().Where(p=> p.ProcessName.StartsWith("chrome")).ToList();      
        if( chromeDriverProcesses.Count > 0)
        {
            Console.WriteLine("Found existing chromedriver processes... killing");
            foreach (var process in Process.GetProcesses())
            {
                Console.Write($"{process.StartTime} - #{process.Id} - {process.ProcessName}");
                if(process.ProcessName.StartsWith("chrome"))
                {
                    Console.Write($" -- ## KILLING ##");
                    process.Kill();
                }
                Console.WriteLine();
            }
            Console.WriteLine("Done");
        }
    

        // Set up the Chrome driver options
        var options = new ChromeOptions();
        // Prevet bot detection
        options.AddArgument("user-agent=Mozilla/5.0 (Windows NT 10.0; WOW64; rv:50.0) Gecko/20100101 Firefox/50.0");
        options.AddArgument("javascriptEnabled=true");
        options.AddArgument("loadImages=true");

        options.AddArgument("--no-sandbox"); // This is often necessary for running in environments like containers or VMs
        options.AddArgument("--headless"); // If you don't need the GUI
        options.AddArgument("--disable-dev-shm-usage"); // Overcome limited resource problems in some environments
        options.AddArgument("--disable-gpu"); // Applicable mainly for Windows, but can be useful in other scenarios too
        options.AddArgument("--remote-debugging-port=9222"); // Helps in avoiding some issues related to DevTools

        var driverService =  ChromeDriverService.CreateDefaultService();        
        driverService.HideCommandPromptWindow = true;

        var driver = new ChromeDriver(driverService, options);
        return driver;
    }


    private static string BuildFlightSearchUrl(
        string originItaCode,
        string destinationItaCode,
        DateOnly departureDate)
    {
        // Example URL: https://www.kayak.com/flights/AUS-BOI/2024-09-20?sort=price_a
        var baseUrl = "https://www.kayak.com/flights";

        // Construct the URL path
        string path = $"{originItaCode}-{destinationItaCode}/{departureDate:yyyy-MM-dd}";

       // Create query parameters
        var queryParams = new Dictionary<string, string>
        {
            {"sort", "price_a"},
        };
        // Build the query string
        string queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={HttpUtility.UrlEncode(kvp.Value)}"));

        // Combine the base URL, path, and query string
        return $"{baseUrl}/{path}?{queryString}";
    }
}