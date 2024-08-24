internal class KayakFlight
{
    public string? OriginItaCode { get; set; }
    public string? DestinationItaCode { get; set; }
    public DateTimeRange When { get; set; }
    public string? CarrierName { get; set;}
    public decimal TotalPrice { get; set; }
    public string? Url { get; set; }
    public string? Uid { get; set; }
    public int NumberOfStops { get; set; }

    public string Provider => "Kayak.com";
    public string Title => $"Flight from {OriginItaCode} to {DestinationItaCode}";
}