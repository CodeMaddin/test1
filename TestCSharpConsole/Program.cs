var departureDate = DateTime.Today.AddDays(25);
var search = new KayakFlightSearch
{
    OriginItaCode = "AUS",
    DestinationItaCode = "BOI",
    DepartureDate = DateOnly.FromDateTime(departureDate),

    MaxStops = 1,
    TakeoffTimeRangeStart = new TimeOnly(8,0),

    ExpandSearchResults = true,
    MaximumSearchResultExpansion = 0,
};
foreach (var flight in KayakService.GetSearchResults(search))
{
    KayakService.PrintFlightRow(flight);
}
