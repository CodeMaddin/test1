using System.Diagnostics;
using System.Globalization;
using System.Web;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.Extensions;
using OpenQA.Selenium.Support.UI;
  
public static class KayakService
{
    public static IEnumerable<KayakFlight> GetSearchResults(string originItaCode, string destinationItaCode, DateOnly departureDate, bool expandSearchResults = false)
    {
        var flights = new List<KayakFlight>();

        Console.WriteLine("Scraping search results from Kayak.com...");
        var searchBaseUrl = BuildFlightSearchBasePathUrl(originItaCode, destinationItaCode, departureDate);
        var searchUrl = $"{searchBaseUrl}?sort=price_a";

        using(WebDriver driver = LoadResultsWebPage(searchUrl))
        {
            try
            {
                if(expandSearchResults)
                    ExpandAllSearchResults(driver);

                // Find the search result items: div elements of class type: nrc6-mod-pres-multi-fare
                var fareElements = driver.FindElements(By.ClassName("nrc6-mod-pres-multi-fare"));

                // Extract the flight & ticket data for each search result
                Console.WriteLine($"{"Flight Times",-27}  {"Duration",-10}  {"Stops",5}  {"Carrier",-25}  {"Price",-10}");
                foreach (var fareElement in fareElements)
                {
                    var resultId = fareElement.GetAttribute("data-resultid");
                    var resultUrl = $"{searchBaseUrl}/f{resultId}";

                    // eg: "4:00 pm – 5:05 pm", "11:00 pm – 7:41 am\r\n+2"
                    var flightTimesText = fareElement.FindElement(By.XPath(".//div[@class='VY2U']//div[contains(@class, 'vmXl-mod-variant-large')]")).Text;
                    // eg: "10h 41m"
                    var flightDurationText = fareElement.FindElement(By.XPath(".//div[@class='xdW8']//div[contains(@class, 'vmXl-mod-variant-default')]")).Text;
                    // eg: "Frontier"
                    var carrierText = fareElement.FindElement(By.XPath(".//div[@class='VY2U']//div[contains(@class, 'c_cgF-mod-variant-default')]")).Text;
                    // eg: "$100"
                    var ticketPriceText = fareElement.FindElement(By.ClassName("f8F1-price-text")).Text;

                    // eg: "nonstop", "1 stop", "2 stops"
                    var stopsText = fareElement.FindElements(By.ClassName("JWEO-stops-text"));
                    var numberOfStops = stopsText.Count > 0 ? stopsText[0].Text.Contains("nonstop", StringComparison.InvariantCultureIgnoreCase) ? 0 : int.Parse(stopsText[0].Text.Split(' ')[0]) : 0;

                    var flightTimes = flightTimesText.Split('–');
                    var departureDateTime = departureDate.ToDateTime(TimeOnly.ParseExact(flightTimes[0].Trim(), "h:mm tt", CultureInfo.InvariantCulture, DateTimeStyles.None));
                    var arrivalDateTime = departureDate.ToDateTime(TimeOnly.ParseExact(flightTimes[1].Split("+")[0].Trim(), "h:mm tt", CultureInfo.InvariantCulture, DateTimeStyles.None));
                    if (flightTimesText.Contains("+"))
                        arrivalDateTime = arrivalDateTime.AddDays(int.Parse(flightTimesText.Split('+')[1]));

                    var flight = new KayakFlight
                    {
                        OriginItaCode = originItaCode,
                        DestinationItaCode = destinationItaCode,
                        // When should be created from the departureDate, flightTimesText and flightDurationText
                        When = new DateTimeRange(departureDateTime, arrivalDateTime),
                        CarrierName = carrierText,
                        TotalPrice = decimal.Parse(ticketPriceText.TrimStart('$')),
                        NumberOfStops = numberOfStops,
                        Url = resultUrl,
                        Uid = resultId
                    };

                    // Print the result URL
                    //Console.WriteLine($"{resultUrl}");
                    // Write the rest as one line formatted as a table using fixed width columns
                    var flightTimesString = $"{flight.When.Start,8:h:mm tt} - {flight.When.End,8:h:mm tt}"
                        + $"{(flight.When.End.Date > flight.When.Start.Date ? " +" + (flight.When.End.Date - flight.When.Start.Date).Days : "")}";

                    Console.WriteLine($"{flightTimesString,-27}  {flight.When.Duration,-10:h\\h\\ m\\m}  {flight.NumberOfStops,5}  {flight.CarrierName,-25}  ${flight.TotalPrice,-10:F2}");

                    // Segment data requires expanding the fare element and could cause the server to block us from bot detection
                    // ExtractSegmentData(driver, fareElement);

                    flights.Add(flight);
                }
            }
            catch (Exception)
            {
                Console.WriteLine("# EXCEPTION #");
                ScrapingService.SaveErrorFiles(driver, DateTime.Now);
                throw;
            }
            return flights;
        }

        static void ExtractSegmentData(WebDriver driver, IWebElement fareElement)
        {
            // Extract the segment data
            ExpandFareElement(driver, fareElement);
            foreach (var segmentElement in fareElement.FindElements(By.ClassName("nAz5")))
            {
                var flightNumber = segmentElement.GetAttribute("data-flightnumber");
                var carrierInformation = segmentElement.FindElement(By.ClassName("nAz5-carrier-text")).Text;

                // <img src="https://content.r9cdn.net/rimg/provider-logos/airlines/v/UA.png?crop=false&amp;width=108&amp;height=92&amp;fallback=default1.png&amp;_v=0c95e6df5bcf556791991bfcdc6e1763" alt="United Airlines">
                var carrierLogo = segmentElement.FindElement(By.XPath(".//div[@class='nAz5-carrier-icon']//img")).GetAttribute("src");
                var carrierName = segmentElement.FindElement(By.XPath(".//div[@class='nAz5-carrier-icon']//img")).GetAttribute("alt");

                // Get segment flight info
                //<div class="nAz5-segment-body"><div class="g16k"><div class="g16k-time-graph"><span class="g16k-dot"></span><span class="g16k-axis"></span></div><div class="g16k-time-info"><div class="g16k-time-info-text-wrapper"><span class="g16k-time">4:17 pm</span><div class="g16k-location-block"><span class="g16k-station">Austin Bergstrom (AUS)</span></div></div></div></div><div class="nAz5-duration-row"><div class="nAz5-graph-icon"><svg viewBox="0 0 200 200" width="1.25em" height="1.25em" xmlns="http://www.w3.org/2000/svg" class="nAz5-eq-icon" role="presentation"><path></path></svg></div><div class="nAz5-duration-text">1h 06m</div></div><div class="g16k"><div class="g16k-time-graph"><span class="g16k-axis"></span><span class="g16k-dot"></span></div><div class="g16k-time-info g16k-incoming"><div class="g16k-time-info-text-wrapper"><span class="g16k-time">5:23 pm</span><div class="g16k-location-block"><span class="g16k-station">Houston George Bush Intcntl (IAH)</span></div></div></div></div><div class="nAz5-segment-extras-wrapper"></div></div>
                var segmentFlightData = segmentElement.FindElements(By.ClassName("g16k"));
                var departureTime = segmentFlightData[0].FindElement(By.ClassName("g16k-time")).Text;
                var departureLocation = segmentFlightData[0].FindElement(By.ClassName("g16k-location-block")).Text;
                var arrivalTime = segmentFlightData[1].FindElement(By.ClassName("g16k-time")).Text;
                var arrivalLocation = segmentFlightData[1].FindElement(By.ClassName("g16k-location-block")).Text;

                var duration = segmentElement.FindElement(By.ClassName("nAz5-duration-text")).Text;

                // Print all segment info
                Console.WriteLine($" - {carrierName} #{flightNumber} {duration} {departureTime} - {arrivalTime}, from {departureLocation} to {arrivalLocation} ");

                // Get segment layover info
                // <div class="c62AT-layover-info"><span class="c62AT-duration c62AT-mod-variant-default">0h 42m</span><span class="c62AT-separator">•</span><span>Change planes in Houston (IAH)</span></div>
                if (segmentElement.FindElements(By.ClassName("c62AT-layover-info")).Any())
                {
                    var layoverDuration = segmentElement.FindElement(By.ClassName("c62AT-duration")).Text;
                    var layoverLocation = segmentElement.FindElement(By.ClassName("c62AT-separator")).Text;
                    Console.WriteLine($"   Layover: {layoverDuration} at {layoverLocation}");
                }
            }
        }

        static WebDriver LoadResultsWebPage(string searchUrl)
        {
            // Use Selenium to scrape the search results
            var driver = ScrapingService.CreateDefaultWebDriver();
            Console.WriteLine($"Navigating to URL: {searchUrl}");
            driver.Navigate().GoToUrl(searchUrl);

            // Wait for page to load
            Console.Write("Waiting for page load...");
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(25));
            wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));
            Console.WriteLine(" done");

            Console.Write("Waiting for search results to load...");
            Thread.Sleep(TimeSpan.FromSeconds(1));

            Console.Write(" (go away spinner)...");
            wait.Until(d => !d.FindElements(By.ClassName("bE-8-spinner")).Any());
            Console.WriteLine("done");

            return driver;
        }
    }

    private static void ExpandAllSearchResults(WebDriver driver)
    {
        // Expand the results by clicking the "Show more results button". After clicking the button, the pages loads a few more results, then the button shows up again. The button will not show back up once all the results have loaded
        // Button to expand has this class=ULvh-button show-more-button
        // The buttons parent is a div with class "ULvh" it stays visible the entire time, until all results have been loaded, then the element is removed from the webpage
        do
        {
            var showMoreResultsButton = driver.FindElements(By.ClassName("ULvh-button")).FirstOrDefault();
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

        } while (driver.FindElements(By.ClassName("ULvh")).Any());
    }

    private static void ExpandFareElement(WebDriver driver, IWebElement fareElement)
    {
        // Try to click the fare element. If it is intercepted, close the dialogs covering it.
        bool tryAgain = true;
        while (tryAgain) // Keep trying until we can click the fare element (and all dialogs covering it are closed)
        {
            try
            {
                fareElement.Click(); // Open the fare details (to get segment data)
                tryAgain = false;
            }
            catch (OpenQA.Selenium.ElementClickInterceptedException)
            {
                // Click was intercepted. Close the dialogs covering the fare element.
                // ElementClickInterceptedException: Message=element click intercepted: Element is not clickable at point (xxx, yyy). Other element would receive the click
                Console.WriteLine("Element click intercepted. Closing dialogs...");

                // Scroll the fare element into view
                driver.ExecuteScript("arguments[0].scrollIntoView(true);", fareElement);

                // find the dialogs and close them. They will be a dive with an attribute role="dialog"
                var dialogs = driver.FindElements(By.XPath("//div[@role='dialog']"));
                foreach (var dialog in dialogs)
                {
                    // find the close icon class=bBPb-closeIcon and click it
                    var closeIcon = dialog.FindElements(By.ClassName("bBPb-closeIcon")).FirstOrDefault();
                    if (closeIcon is not null)
                    {
                        Console.Write("Dialog found. Closing...");
                        closeIcon.Click();
                        Console.WriteLine(" done");
                        tryAgain = true;
                        // Wait a couple of seconds in case other dialogs open
                        System.Threading.Thread.Sleep(1000);
                    }
                }
            }
        }
    }

    // Example URL: https://www.kayak.com/flights/AUS-BOI/2024-09-20?sort=price_a
    private static string BuildFlightSearchBasePathUrl(string originItaCode, string destinationItaCode, DateOnly departureDate)
       => $"https://www.kayak.com/flights/{originItaCode}-{destinationItaCode}/{departureDate:yyyy-MM-dd}";
}