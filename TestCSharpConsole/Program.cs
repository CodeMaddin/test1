KayakService.PrintFlightHeaderRow();
foreach (var flight in KayakService.GetSearchResults("AUS", "PHX", DateOnly.FromDateTime(DateTime.Today.AddDays(24))))
    KayakService.PrintFlightRow(flight);
