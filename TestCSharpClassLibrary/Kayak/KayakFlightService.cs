using System.Globalization;
using System.Text.Json;
using HtmlAgilityPack;

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
    public int MaximumResultPages { get; set; } = 5;
}

public static class KayakService
{
    private const string CacheDirectory = "KayakCache\\Flights";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromHours(1);

    public static IEnumerable<KayakFlight> GetSearchResults(KayakFlightSearch search)
    {
        var cacheKey = GetCacheKey(search);
        var cachedResults = ScrapingService.GetCachedResults<IEnumerable<KayakFlight>>(CacheDirectory, cacheKey, CacheExpiration);
        // TEMP: Don't return cached flight results so that we can test out the cached html
        if (false) 
        //if (cachedResults != null)
        {            
            foreach(var result in cachedResults)
                yield return result;
        }

        var flights = new List<KayakFlight>();
        int pageNumber = 1;
        bool morePages;

        do
        {
            var doc = GetHtmlDoc(search, pageNumber, cacheKey);
            var scriptNode = doc.DocumentNode.EnsureSelectSingleNode("Json Data", "//script[@id='__R9_HYDRATE_DATA__']");
            var flightData = JsonSerializer.Deserialize<JsonElement>(scriptNode.InnerHtml);

            var resultsList = flightData.EnsureGetProperty("serverData").EnsureGetProperty("FlightResultsList");
            var pageSize = resultsList.EnsureGetProperty("pageSize").GetInt32();
            var filteredCount = resultsList.EnsureGetProperty("filteredCount").GetInt32();
            var returnedPageNumber = resultsList.EnsureGetProperty("pageNumber").GetInt32();

            if (returnedPageNumber != pageNumber)
            {
                Console.WriteLine($"Returned page number {returnedPageNumber} does not match requested page number {pageNumber}");
                throw new Exception($"Returned page number {returnedPageNumber} does not match requested page number {pageNumber}");
            }

            morePages = (pageNumber * pageSize) < filteredCount;

            var resultIds = resultsList.EnsureGetProperty("resultIds").EnumerateArray();
            var results = resultsList.EnsureGetProperty("results");

            foreach (var resultId in resultIds)
            {
                if (resultId.GetString().Length == "b13d7cc5b0dea093bc96f5e40b3f13a9".Length)
                {
                    var result = results.EnsureGetProperty(resultId.GetString());
                    var flight = ParseFlightFromJson(result);
                    flights.Add(flight);
                    yield return flight;
                }
            }

            pageNumber++;
        } 
        while (pageNumber <= search.MaximumResultPages && morePages);

        ScrapingService.CacheResults(CacheDirectory, cacheKey, flights);
    }

    private static HtmlDocument GetHtmlDoc(KayakFlightSearch search, int pageNumber, string cacheKey)
    {
        var htmlCacheKey = $"{cacheKey}_p{pageNumber}_html";
        var html = ScrapingService.GetCachedResults<string>(CacheDirectory, htmlCacheKey, CacheExpiration);
        if (html == null)
        {
            var searchBaseUrl = BuildFlightSearchBasePathUrl(search.OriginItaCode, search.DestinationItaCode, search.DepartureDate);
            var searchUrl = $"{searchBaseUrl}?sort=price_a{BuildSearchParameters(search)}&pageNumber={pageNumber}";

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36");

            Console.WriteLine($"Downloading HTML from: {searchUrl}");
            html = httpClient.GetStringAsync(searchUrl).Result;
            ScrapingService.CacheResults(CacheDirectory, htmlCacheKey, html);
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return doc;
    }

    private static KayakFlight ParseFlightFromJson(JsonElement result)
    {
        var flight = new KayakFlight
        {
            Provider = "kayak.com",
            Uid = result.EnsureGetProperty("resultId").GetString()
        };

        var leg = result.EnsureGetProperty("legs")[0];
        flight.Duration = TimeSpan.FromMinutes(leg.EnsureGetProperty("legDurationMinutes").GetInt32());

        var segments = leg.EnsureGetProperty("segments");
        var firstSegment = segments[0];
        var lastSegment = segments.EnumerateArray().Last();

        var departure = firstSegment.EnsureGetProperty("departure");
        flight.OriginItaCode = departure.EnsureGetProperty("airport").EnsureGetProperty("code").GetString();
        flight.DepartureTime = DateTime.Parse(departure.EnsureGetProperty("isoDateTimeLocal").GetString());

        var arrival = lastSegment.EnsureGetProperty("arrival");
        flight.DestinationItaCode = arrival.EnsureGetProperty("airport").EnsureGetProperty("code").GetString();
        flight.ArrivalTime = DateTime.Parse(arrival.EnsureGetProperty("isoDateTimeLocal").GetString());

        flight.NumberOfStops = segments.GetArrayLength() - 1;
        flight.CarrierName = leg.EnsureGetProperty("displayAirline").EnsureGetProperty("name").GetString();
        flight.TotalPrice = result.EnsureGetProperty("trackingDataLayer").EnsureGetProperty("tagLayerPrice").GetDecimal();
        flight.Url = $"https://www.kayak.com/flights/{flight.OriginItaCode}-{flight.DestinationItaCode}/f{flight.Uid}";
        flight.Days = (flight.ArrivalTime.Date - flight.DepartureTime.Date).Days;

        return flight;
    }

    public static string GetCacheKey(KayakFlightSearch search) =>
        $"{search.OriginItaCode}-{search.DestinationItaCode}-{search.DepartureDate:yyyyMMdd}" +
        $"-{search.MaxPrice ?? 0}" +
        $"-{search.TakeoffTimeRangeStart?.ToString("hhmm") ?? "0000"}-{search.TakeoffTimeRangeEnd?.ToString("hhmm") ?? "2359"}" +
        $"-{search.LandingTimeRangeStart?.ToString("hhmm") ?? "0000"}-{search.LandingTimeRangeEnd?.ToString("hhmm") ?? "2359"}" +
        $"-{search.MaxStops ?? -1}" +
        $"-{(int)(search.MaxFlightDuration?.TotalMinutes ?? -1)}" +
        $"-{(int)(search.MinLayoverTime?.TotalMinutes ?? -1)}-{(int)(search.MaxLayoverTime?.TotalMinutes ?? -1)}";

    public static void PrintFlightRow(KayakFlight flight)
    {
        var flightTimesString = $"{flight.DepartureTime,8:h:mm tt} - {flight.ArrivalTime,8:h:mm tt}"
            + $"{(flight.ArrivalTime.Date > flight.DepartureTime.Date ? " +" + (flight.ArrivalTime.Date - flight.DepartureTime.Date).Days : "")}";

        Console.WriteLine($"{flightTimesString,-27}  {flight.Duration,-10:h\\h\\ m\\m}  {flight.NumberOfStops,5}  {flight.CarrierName,-25}  ${flight.TotalPrice,-10:F2}");
    }

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