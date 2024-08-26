KayakService.PrintFlightHeaderRow();
var departureDate = DateTime.Today.AddDays(25);
var search = new KayakFlightSearch
{
    OriginItaCode = "AUS",
    DestinationItaCode = "PHX",
    DepartureDate = DateOnly.FromDateTime(departureDate),

    MaxStops = 0,
    TakeoffTimeRangeStart = new TimeOnly(8,0),
    LandingTimeRangeEnd = new TimeOnly(12+6,0),

    ExpandSearchResults = true,
    MaximumSearchResultExpansion = 2
};
foreach (var flight in KayakService.GetSearchResults(search))
{
    KayakService.PrintFlightRow(flight);
}
