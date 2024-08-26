using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Web;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.Extensions;
using OpenQA.Selenium.Support.UI;
using System.Text.Json;

public static class KayakService
{
    private const string CacheDirectory = "KayakCache";
    private readonly static TimeSpan CacheExpiration = TimeSpan.FromHours(1);

    public static IEnumerable<KayakFlight> GetSearchResults(string originItaCode, string destinationItaCode, DateOnly departureDate, bool expandSearchResults = false)
    {
        string cacheKey = $"{originItaCode}-{destinationItaCode}-{departureDate:yyyy-MM-dd}";
        var cachedResults = GetCachedResults<IEnumerable<KayakFlight>>(cacheKey);

        if (cachedResults != null)
        {
            foreach(var result in cachedResults)
                yield return result;
            yield break;
        }

        var flights = new List<KayakFlight>();

        Console.WriteLine("Scraping search results from Kayak.com...");
        var searchBaseUrl = BuildFlightSearchBasePathUrl(originItaCode, destinationItaCode, departureDate);
        var searchUrl = $"{searchBaseUrl}?sort=price_a";

        using(WebDriver driver = LoadResultsWebPage(searchUrl))
        {
            ReadOnlyCollection<IWebElement> fareElements = ReadOnlyCollection<IWebElement>.Empty;
            try
            {
                //if (expandSearchResults)
                ExpandAllSearchResults(driver, true);

                // Find the search result items: div elements of class type: nrc6-mod-pres-multi-fare
                fareElements = driver.FindElements(By.CssSelector(".nrc6-mod-pres-multi-fare"));

            }
            catch (Exception ex)
            {
                HandleException(driver, ex);
                throw;
            }

            // Extract the flight & ticket data for each search result
            foreach (var fareElement in fareElements)
            {
                KayakFlight? flight = null;
                try
                {
                    var resultId = fareElement.GetAttribute("data-resultid");
                    var resultUrl = $"{searchBaseUrl}/f{resultId}";

                    // eg: "4:00 pm – 5:05 pm", "11:00 pm – 7:41 am\r\n+2"
                    var flightTimesText = fareElement.FindElement(By.CssSelector("div.VY2U div.vmXl-mod-variant-large")).Text;
                    // eg: "10h 41m"
                    var flightDurationText = fareElement.FindElement(By.CssSelector("div.xdW8 div.vmXl-mod-variant-default")).Text;
                    // eg: "Frontier"
                    var carrierText = fareElement.FindElement(By.CssSelector("div.VY2U div.c_cgF-mod-variant-default")).Text;
                    // eg: "$100"
                    var ticketPriceText = fareElement.FindElement(By.CssSelector(".f8F1-price-text")).Text;
                    // eg: "nonstop", "1 stop", "2 stops"
                    var stopsText = fareElement.FindElements(By.CssSelector(".JWEO-stops-text"));
                    var numberOfStops = stopsText.Count > 0 ? stopsText[0].Text.Contains("nonstop", StringComparison.InvariantCultureIgnoreCase) ? 0 : int.Parse(stopsText[0].Text.Split(' ')[0]) : 0;

                    var flightTimes = flightTimesText.Split('–');
                    var days = flightTimes[1].Contains("+") ? int.Parse(flightTimes[1].Split('+')[1]) : 0;
                    var departureDateTime = departureDate.ToDateTime(TimeOnly.ParseExact(flightTimes[0].Trim(), "h:mm tt", CultureInfo.InvariantCulture, DateTimeStyles.None));
                    var arrivalDateTime = departureDate.ToDateTime(TimeOnly.ParseExact(flightTimes[1].Split("+")[0].Trim(), "h:mm tt", CultureInfo.InvariantCulture, DateTimeStyles.None)).AddDays(days);

                    flight = new KayakFlight
                    {
                        OriginItaCode = originItaCode,
                        DestinationItaCode = destinationItaCode,
                        DepartureTime = departureDateTime,
                        ArrivalTime = arrivalDateTime,
                        Duration = TimeSpan.ParseExact(flightDurationText, "h\\h\\ m\\m", CultureInfo.InvariantCulture),
                        Days = flightTimesText.Contains("+") ? int.Parse(flightTimesText.Split('+')[1]) : 0,
                        CarrierName = carrierText,
                        TotalPrice = decimal.Parse(ticketPriceText.TrimStart('$')),
                        NumberOfStops = numberOfStops,
                        Url = resultUrl,
                        Uid = resultId
                    };
                }
                catch (Exception ex)
                {
                    HandleException(driver, ex);
                    throw;
                }

                flights.Add(flight);    
                yield return flight;
            }
        }

        // After scraping, cache the results
        CacheResults(cacheKey, flights);
    }

    private static T? GetCachedResults<T>(string cacheKey)
    {
        var cacheFilePath = Path.Combine(CacheDirectory, $"{cacheKey}.json");
        Console.Write($"Checking cache ${cacheKey} @ {cacheFilePath} -- ");
        if (!File.Exists(cacheFilePath))
        {
            Console.WriteLine($"MISS");
            return default(T);
        }

        var fileInfo = new FileInfo(cacheFilePath);
        if (fileInfo.LastWriteTime.Add(CacheExpiration) < DateTime.Now)
        {
            Console.WriteLine($"EXPIRED");
            File.Delete(cacheFilePath);
            return default(T);
        }

        Console.WriteLine($"HIT");
        var json = File.ReadAllText(cacheFilePath);
        return JsonSerializer.Deserialize<T>(json);
    }

    private static void CacheResults(string cacheKey, object item)
    {
        Directory.CreateDirectory(CacheDirectory);
        var cacheFilePath = Path.Combine(CacheDirectory, $"{cacheKey}.json");
        Console.Write($"Caching ${cacheKey} @ {cacheFilePath} -- ");
        var json = JsonSerializer.Serialize(item);
        File.WriteAllText(cacheFilePath, json);
        Console.WriteLine($"DONE");
    }

    public static void PrintFlightHeaderRow()
    {
        Console.WriteLine($"{"Flight Times",-27}  {"Duration",-10}  {"Stops",5}  {"Carrier",-25}  {"Price",-10}");
    }

    public static void PrintFlightRow(KayakFlight flight)
    {
        //Console.WriteLine($"{resultUrl}");
        var flightTimesString = $"{flight.DepartureTime,8:h:mm tt} - {flight.ArrivalTime,8:h:mm tt}"
            + $"{(flight.ArrivalTime.Date > flight.DepartureTime.Date ? " +" + (flight.ArrivalTime.Date - flight.DepartureTime.Date).Days : "")}";

        Console.WriteLine($"{flightTimesString,-27}  {flight.Duration,-10:h\\h\\ m\\m}  {flight.NumberOfStops,5}  {flight.CarrierName,-25}  ${flight.TotalPrice,-10:F2}");
    }

    private static void HandleException(WebDriver driver, Exception? ex = null)
    {
        Console.WriteLine("# EXCEPTION #");
        ScrapingService.SaveErrorFiles(driver, DateTime.Now);
    }

    private static WebDriver LoadResultsWebPage(string searchUrl)
    {
        // Use Selenium to scrape the search results
        Console.WriteLine($"Loading web browser...");
        var driver = ScrapingService.CreateDefaultWebDriver();
        Console.WriteLine($"Navigating to URL: {searchUrl}");
        driver.Navigate().GoToUrl(searchUrl);

        // Wait for page to load
        Console.Write("Waiting for... page load...");
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(25));
        wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));

        Console.Write(" search results...");
        Thread.Sleep(TimeSpan.FromSeconds(1));

        Console.Write(" [ go away spinner ]...");
        wait.Until(d => !d.FindElements(By.CssSelector(".bE-8-spinner")).Any());
        Console.WriteLine(" done");

        return driver;
    }

    private static void ExpandAllSearchResults(WebDriver driver, bool onlyOnce = false)
    {
        // Expand the results by clicking the "Show more results button". After clicking the button, the pages loads a few more results, then the button shows up again. The button will not show back up once all the results have loaded
        // Button to expand has this class=ULvh-button show-more-button
        // The buttons parent is a div with class "ULvh" it stays visible the entire time, until all results have been loaded, then the element is removed from the webpage
        do
        {
            var showMoreResultsButton = driver.FindElements(By.CssSelector(".ULvh-button")).FirstOrDefault();
            if (showMoreResultsButton != null)
            {
                // Scroll into view
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", showMoreResultsButton);
                Thread.Sleep(0);
                // Click the button using javascript
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", showMoreResultsButton);

                // Sleep a random amount of time to avoid bot detection
                Thread.Sleep(new Random((int)DateTime.Now.Ticks).Next(1000, 3000));
            }
            Thread.Sleep(0);

        } while (!onlyOnce && driver.FindElements(By.CssSelector(".ULvh")).Any());
    }

    // Example URL: https://www.kayak.com/flights/AUS-BOI/2024-09-20?sort=price_a
    private static string BuildFlightSearchBasePathUrl(string originItaCode, string destinationItaCode, DateOnly departureDate)
       => $"https://www.kayak.com/flights/{originItaCode}-{destinationItaCode}/{departureDate:yyyy-MM-dd}";
}