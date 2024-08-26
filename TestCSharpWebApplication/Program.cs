using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

// Define the API endpoint
app.MapGet("/flights/{originItaCode}/{destinationItaCode}/{departureDate}", (string originItaCode, string destinationItaCode, string departureDate) =>
{
    if (!DateOnly.TryParseExact(departureDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
    {
        return Results.BadRequest("Invalid date format. Use yyyy-MM-dd.");
    }

    //var flights = KayakService.GetSearchResults(originItaCode, destinationItaCode, parsedDate);
    //return Results.Ok(flights);
    //var text = $"Flights from {originItaCode} to {destinationItaCode} on {parsedDate:ddd MMM dd yyyy}";
    //return Results.Ok($"{Environment.MachineName} - {Environment.UserName} - {Environment.UserDomainName}  : {Environment.GetEnvironmentVariables().ToString()}");
    KayakService.PrintFlightHeaderRow();
    var search = new KayakFlightSearch
    {
        OriginItaCode = originItaCode,
        DestinationItaCode = destinationItaCode,
        DepartureDate = parsedDate,
    };
    return Results.Ok(KayakService.GetSearchResults(search));

})
.WithName("GetFlights");
//.WithOpenApi();

app.Run();