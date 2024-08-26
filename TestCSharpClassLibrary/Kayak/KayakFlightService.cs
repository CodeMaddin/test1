using System.Collections.ObjectModel;
using System.Globalization;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

public class KayakFlightSearch
{
    public required string OriginItaCode { get; set; }
    public required string DestinationItaCode { get; set; }
    public required DateOnly DepartureDate { get; set; }

    public decimal? MaxPrice { get; set; }

    public TimeOnly? TakeoffTimeRangeStart { get; set; }
    public TimeOnly? TakeoffTimeRangeEnd { get; set; }
    public TimeOnly? LandingTimeRangeStart { get; set; }
    public TimeOnly? LandingTimeRangeEnd { get; set; }
    public int? MaxStops { get; set; }
    public TimeSpan? MaxFlightDuration { get; set; }
    public TimeSpan? MinLayoverTime { get; set; }
    public TimeSpan? MaxLayoverTime { get; set; }

    public bool ExpandSearchResults { get; set; } = false;
    public int MaximumSearchResultExpansion { get; set; } = 2;
}

public static class KayakService
{
    private const string CacheDirectory = "KayakCache\\Flights";
    private readonly static TimeSpan CacheExpiration = TimeSpan.FromHours(1);

    public static IEnumerable<KayakFlight> GetSearchResults(KayakFlightSearch search)
    {
        string cacheKey = $"{search.OriginItaCode}-{search.DestinationItaCode}-{search.DepartureDate:yyyyMMdd}" +
            $"-{search.MaxPrice ?? 0}" +
            $"-{search.TakeoffTimeRangeStart?.ToString("hhmm") ?? "0000"}-{search.TakeoffTimeRangeEnd?.ToString("hhmm") ?? "2359"}" +
            $"-{search.LandingTimeRangeStart?.ToString("hhmm") ?? "0000"}-{search.LandingTimeRangeEnd?.ToString("hhmm") ?? "2359"}" +
            $"-{search.MaxStops ?? -1}" +
            $"-{(int)(search.MaxFlightDuration?.TotalMinutes ?? -1)}" +
            $"-{(int)(search.MinLayoverTime?.TotalMinutes ?? -1)}-{(int)(search.MaxLayoverTime?.TotalMinutes ?? -1)}";
        var cachedResults = ScrapingService.GetCachedResults<IEnumerable<KayakFlight>>(CacheDirectory, cacheKey, CacheExpiration);

        if (cachedResults != null)
        {
            foreach(var result in cachedResults)
                yield return result;
            yield break;
        }

        var flights = new List<KayakFlight>();

        Console.WriteLine("Scraping search results from Kayak.com...");
        var searchBaseUrl = BuildFlightSearchBasePathUrl(search.OriginItaCode, search.DestinationItaCode, search.DepartureDate);
        var searchUrl = $"{searchBaseUrl}?sort=price_a{BuildSearchParameters(search)}";

        using(WebDriver driver = LoadResultsWebPage(searchUrl))
        {
            ReadOnlyCollection<IWebElement> fareElements = ReadOnlyCollection<IWebElement>.Empty;
            try
            {
                if(search.ExpandSearchResults)
                    ExpandAllSearchResults(driver, search.MaximumSearchResultExpansion);

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
                    var days = flightTimes[1].Contains('+') ? int.Parse(flightTimes[1].Split('+')[1]) : 0;
                    var departureDateTime = search.DepartureDate.ToDateTime(TimeOnly.ParseExact(flightTimes[0].Trim(), "h:mm tt", CultureInfo.InvariantCulture, DateTimeStyles.None));
                    var arrivalDateTime = search.DepartureDate.ToDateTime(TimeOnly.ParseExact(flightTimes[1].Split("+")[0].Trim(), "h:mm tt", CultureInfo.InvariantCulture, DateTimeStyles.None)).AddDays(days);

                    flight = new KayakFlight
                    {
                        Provider = "kayak.com",
                        Uid = resultId,
                        Url = resultUrl,

                        OriginItaCode = search.OriginItaCode,
                        DestinationItaCode = search.DestinationItaCode,
                        DepartureTime = departureDateTime,
                        ArrivalTime = arrivalDateTime,
                        Duration = TimeSpan.ParseExact(flightDurationText, "h\\h\\ m\\m", CultureInfo.InvariantCulture),
                        Days = flightTimesText.Contains('+') ? int.Parse(flightTimesText.Split('+')[1]) : 0,
                        CarrierName = carrierText,
                        TotalPrice = decimal.Parse(ticketPriceText.TrimStart('$')),
                        NumberOfStops = numberOfStops,
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
        ScrapingService.CacheResults(CacheDirectory, cacheKey, flights);
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

    private static void HandleException(WebDriver driver, Exception ex)
    {
        Console.WriteLine("# EXCEPTION #");
        Console.WriteLine(ex);
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
        wait.Until(d => d.FindElements(By.CssSelector(".bE-8-spinner")).Count == 0);
        Console.WriteLine(" done");

        return driver;
    }

    private static void ExpandAllSearchResults(WebDriver driver, int maxtimes)
    {
        int count = 0;
        // Expand the results by clicking the "Show more results button". After clicking the button, the pages loads a few more results, then the button shows up again. The button will not show back up once all the results have loaded
        // Button to expand has this class=ULvh-button show-more-button
        // The buttons parent is a div with class "ULvh" it stays visible the entire time, until all results have been loaded, then the element is removed from the webpage
        Console.Write("Expanding search results...");
        do
        {
            Console.Write($" expantion #{count+1}... ");
            var showMoreResultsButton = driver.FindElements(By.CssSelector(".ULvh-button")).FirstOrDefault();
            if (showMoreResultsButton != null)
            {
                // Scroll into view
                Console.Write(" scrolling... ");
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", showMoreResultsButton);
                Thread.Sleep(0);
                // Click the button using javascript
                Console.Write(" clicking... ");
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", showMoreResultsButton);

                count--;

                // Sleep a random amount of time to avoid bot detection
                Thread.Sleep(new Random((int)DateTime.Now.Ticks).Next(1000, 2000));
            }
            Thread.Sleep(0);

        } while ((count < maxtimes) && driver.FindElements(By.CssSelector(".ULvh")).Count > 0);
        Console.WriteLine(" done");
    }

    // Example URL: https://www.kayak.com/flights/AUS-BOI/2024-09-20?sort=price_a
    private static string BuildFlightSearchBasePathUrl(string originItaCode, string destinationItaCode, DateOnly departureDate)
       => $"https://www.kayak.com/flights/{originItaCode}-{destinationItaCode}/{departureDate:yyyy-MM-dd}";

    private static string BuildSearchParameters(KayakFlightSearch search)
    {
        var parameters = new List<string>();

        if (search.TakeoffTimeRangeStart.HasValue || search.TakeoffTimeRangeEnd.HasValue)
        {
            var takeoffStart = search.TakeoffTimeRangeStart?.ToString("hhmm") ?? "0000";
            var takeoffEnd = search.TakeoffTimeRangeEnd?.ToString("hhmm") ?? "2359";
            parameters.Add($"takeoff={takeoffStart}-{takeoffEnd}");
        }

        // TODO: What if the landing time crosses midnight?
        if (search.LandingTimeRangeStart.HasValue || search.LandingTimeRangeEnd.HasValue)

        {
            var landingStart = search.LandingTimeRangeStart?.ToString("hhmm") ?? "0000";
            var landingEnd = search.LandingTimeRangeEnd?.ToString("hhmm") ?? "2359";
            parameters.Add($"landing={landingStart}-{landingEnd}");
        }

        if (search.MaxPrice.HasValue)
            parameters.Add($"price={search.MaxPrice.Value}");

        if (search.MaxStops.HasValue)
            // examples: "stops=0", "stops=0,1", "stops=0,1,2,3"
            parameters.Add($"stops={string.Join(",", Enumerable.Range(0, search.MaxStops.Value + 1))}");

        if (search.MaxFlightDuration.HasValue)
            parameters.Add($"duration=-{(int)search.MaxFlightDuration.Value.TotalMinutes}");

        if (search.MinLayoverTime.HasValue || search.MaxLayoverTime.HasValue)
        {
            var minLayover = search.MinLayoverTime?.TotalMinutes.ToString("F0") ?? "0";
            var maxLayover = search.MaxLayoverTime?.TotalMinutes.ToString("F0") ?? "1440"; // 24 hours
            parameters.Add($"layoverdur={minLayover}-{maxLayover}");
        }

        return parameters.Count > 0 ? "&fs=" + string.Join(";", parameters) : "";
    }
}