var departureDate = DateTime.Today.AddDays(25);
var search = new KayakFlightSearch
{
    OriginItaCode = "AUS",
    DestinationItaCode = "BOI",
    DepartureDate = DateOnly.FromDateTime(departureDate),

    MaxStops = 1,
    TakeoffTimeRangeStart = new TimeOnly(8,0),

    MaximumResultPages = 3,
};
foreach (var flight in KayakService.GetSearchResults(search))
{
    KayakService.PrintFlightRow(flight);
}
