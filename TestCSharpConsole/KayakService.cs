using System.Diagnostics;
using System.Globalization;
using System.Web;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.Extensions;
using OpenQA.Selenium.Support.UI;

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
    
internal static class KayakService
{
    public static void GetSearchResults(string originItaCode, string destinationItaCode, DateOnly departureDate, bool expandSearchResults = false)
    {
        Console.WriteLine("Scraping search results from Kayak.com...");
        var searchBaseUrl = BuildFlightSearchBasePathUrl(originItaCode, destinationItaCode, departureDate);
        var searchUrl = $"{searchBaseUrl}?sort=price_a";

        using(WebDriver driver = LoadResultsWebPage(searchUrl))
        {
            try
            {
                if(expandSearchResults)
                    ExpandAllSearchResults(driver);

                // Flight Result Item HTML
                //<div data-resultid="3c7de670f0b983d56a8448737fbcf231" class="nrc6 nrc6-mod-pres-multi-fare"><div class="yuAt yuAt-pres-rounded" role="group" tabindex="-1"><div class="nrc6-wrapper"><div class="nrc6-inner"><div class="nrc6-content-section"><div class="nrc6-large-header"><div class="btf6"><div class="btf6-container"><div class="btf6-labels"><div class="ZGc- ZGc--mod-margin-left-none ZGc--mod-theme-default ZGc--mod-variant-special ZGc--mod-layout-inline ZGc--mod-padding-default ZGc--mod-size-default ZGc--mod-bold-text ZGc--mod-nowrap">New partner</div></div><div class="btf6-actions-wrapper"><div><div role="button" tabindex="0" class="AFFP AFFP-body AFFP-s AFFP-res AFFP-emphasis Jav1" aria-label="Share"><div class="Jav1-icon"><svg viewBox="0 0 200 200" width="1.25em" height="1.25em" xmlns="http://www.w3.org/2000/svg" role="presentation"><path d="M182 109.42l-10.75 10.75c0 .08-.08.17-.17.25L127.58 164l-10.92 11H99.99v-40.92c-44.87 2.76-65.8 16.52-75.43 27.33c-3.08 3.46-8.81 1.09-8.68-3.55c.79-28.41 11.87-82.89 84.1-90.12V25h16.67l10.92 11l43.5 43.58c.08.08.17.17.17.25l10.75 10.75c5.25 5.17 5.25 13.67 0 18.83z"></path></svg></div><div class="Jav1-content">Share</div></div><div class="c20A_-sr-only" aria-live="polite" aria-atomic="false"></div></div></div></div></div></div><div class="nrc6-main"><div class="hJSA"><ol class="hJSA-list"><li class="hJSA-item"><div class="c3J0r"><div class="c3J0r-container"><div class="tdCx-mod-spaced tdCx-mod-stacked"><div class="tdCx-leg-carrier"><div class="c5iUd c5iUd-mod-variant-medium"><div class="c5iUd-leg-carrier"><img src="https://content.r9cdn.net/rimg/provider-logos/airlines/v/WN.png?crop=false&amp;width=108&amp;height=92&amp;fallback=default1.png&amp;_v=041327b45b90cad810524142c9e37950" alt="Southwest" aria-hidden="true"></div></div></div></div><div class="VY2U"><div class="vmXl vmXl-mod-variant-large"><span>8:35 pm</span><span class="aOlM"> � </span><span>12:35 am<sup class="VY2U-adendum" title="Flight lands the next day">+1</sup></span></div><div class="c_cgF c_cgF-mod-variant-default" dir="auto">Southwest</div></div><div class="JWEO"><div class="vmXl vmXl-mod-variant-default"><span class="JWEO-stops-text">1 stop</span></div><div class="c_cgF c_cgF-mod-variant-default"><span><span title="0h 50m layover, ,[object Object]">DEN</span></span></div></div><div class="xdW8"><div class="vmXl vmXl-mod-variant-default">5h 00m</div><div class="c_cgF c_cgF-mod-variant-default"><div class="EFvI"><div class="c_cgF c_cgF-mod-variant-default" title="Austin Bergstrom"><span class="jLhY-airport-info" dir="auto"><span>AUS</span></span></div><span class="aOlM">-</span><div class="c_cgF c_cgF-mod-variant-default" title="Boise Air Term. Gowen Fld"><span class="jLhY-airport-info" dir="auto"><span>BOI</span></span></div></div></div></div></div></div></li></ol></div></div><div class="nrc6-default-footer"></div></div><div class="nrc6-price-section nrc6-mod-multi-fare"><div class="Oihj Oihj-mod-pres-multi-fare"><div class="Oihj-full-height"><div class="zx8F"><div class="zx8F-price-tile"><div class="zx8F-price-section"><div class="M_JD M_JD-mod-pres-multi-fare"><div class="M_JD-large-display"><div class="oVHK"><a class="oVHK-fclink" href="/book/flight?code=mhDiJ9iGD5.Mg-fM-A4AlU.32898.3c7de670f0b983d56a8448737fbcf231&amp;h=c91e9d43db78&amp;sub=F2177003753393840060E0e6e333133d&amp;pageOrigin=F..RP.FE.M1" target="_blank"><div class="f8F1"><div class="f8F1-above"><div class="f8F1-price-text-container"><div class="f8F1-price-text">$329</div></div></div></div></a></div><div><div class="aC3z"><div class="aC3z-links"><div class="DOum"><div class="oVHK"><a class="oVHK-fclink" href="/book/flight?code=mhDiJ9iGD5.Mg-fM-A4AlU.32898.3c7de670f0b983d56a8448737fbcf231&amp;h=c91e9d43db78&amp;sub=F2177003753393840060E0e6e333133d&amp;pageOrigin=F..RP.FE.M1" target="_blank"><div class="DOum-option"><div class="DOum-name DOum-mod-ellipsis" title="Wanna Get Away">Wanna Get Away</div></div></a></div></div></div></div></div><div class="oVHK"><a class="oVHK-fclink" href="/book/flight?code=mhDiJ9iGD5.Mg-fM-A4AlU.32898.3c7de670f0b983d56a8448737fbcf231&amp;h=c91e9d43db78&amp;sub=F2177003753393840060E0e6e333133d&amp;pageOrigin=F..RP.FE.M1" target="_blank"><div class="M_JD-provider-name">Southwest</div></a></div></div><div class="M_JD-booking-btn"><div role="listbox" class="dOAU"><div role="button" tabindex="-1" class="dOAU-best"><div class="dOAU-main-btn-wrap"><div class="oVHK"><a role="link" href="/book/flight?code=mhDiJ9iGD5.Mg-fM-A4AlU.32898.3c7de670f0b983d56a8448737fbcf231&amp;h=c91e9d43db78&amp;sub=F2177003753393840060E0e6e333133d&amp;pageOrigin=F..RP.FE.M1" target="_blank" class="Iqt3 Iqt3-mod-stretch Iqt3-mod-bold Button-No-Standard-Style Iqt3-mod-variant-solid Iqt3-mod-theme-progress-legacy Iqt3-mod-shape-rounded-small Iqt3-mod-shape-mod-default Iqt3-mod-spacing-default Iqt3-mod-size-small" tabindex="0" aria-disabled="false"><div class="Iqt3-button-container"><div class="Iqt3-button-content"><span class="dOAU-booking-text">View Deal</span></div></div><div class="Iqt3-button-focus-outline"></div></a></div></div></div></div></div></div></div><div class="zx8F-amenity-dropdown"><div role="button" tabindex="0" class="c7_PC"><div class="c7_PC-amenity-container"><div class="ss46-icon-group-wrapper ss46-mod-gap-size-default"><div role="button" tabindex="-1" class="MGW--wrapper" aria-describedby="tooltip-2e779a1a31158" aria-labelledby="tooltip-2e779a1a31158"><div class="aN1Z-icon-group"><div class="aN1Z-amenity-icon-wrapper"><svg width="200" height="200" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 200 200" role="presentation"><path d="M130 185a15 15 0 01-14.14-10H84.14a15 15 0 01-28.28 0H55a15 15 0 01-15-15v-60a15 15 0 0115-15h15V27.12C70 22.6 73.9 15 100 15c11.2 0 30 1.58 30 12.12V85h15a15 15 0 0115 15v60a15 15 0 01-15 15h-.86A15 15 0 01130 185zm-50-20h40a5 5 0 015 5a5 5 0 0010 0a5 5 0 015-5h5a5 5 0 005-5v-60a5 5 0 00-5-5H55a5 5 0 00-5 5v60a5 5 0 005 5h5a5 5 0 015 5a5 5 0 0010 0a5 5 0 015-5zm0-80h40V28.1c-2-1.23-8.93-3.1-20-3.1s-18 1.87-20 3.1zm45 60a5 5 0 01-5-5v-20a5 5 0 0110 0v20a5 5 0 01-5 5zm-50 0a5 5 0 01-5-5v-20a5 5 0 0110 0v20a5 5 0 01-5 5z"></path></svg></div><div class="aN1Z-restriction-icon-wrapper aN1Z-mod-size-medium"><div class="hk_J-mod-theme-INCLUDED hk_J-mod-size-medium"><svg viewBox="0 0 200 200" width="1.25em" height="1.25em" xmlns="http://www.w3.org/2000/svg" role="presentation"><path d="M100 20c-44.183 0-80 35.817-80 80s35.817 80 80 80s80-35.817 80-80s-35.817-80-80-80zm-8.403 114.801c-8.222 8.896-16.39-1.147-38.097-17.752l12.132-15.697l17.483 13.375L131.85 62l14.65 13.401l-54.903 59.4z"></path></svg></div></div></div></div><div role="button" tabindex="-1" class="MGW--wrapper" aria-describedby="tooltip-da3b67a19f4b4" aria-labelledby="tooltip-da3b67a19f4b4"><div class="aN1Z-icon-group"><div class="aN1Z-amenity-icon-wrapper"><svg width="200" height="200" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 200 200" role="presentation"><path d="M130 185a15 15 0 01-14.14-10H84.14a15 15 0 01-28.28 0H55a15 15 0 01-15-15V60a15 15 0 0115-15h15V27.12C70 22.6 73.9 15 100 15c11.2 0 30 1.58 30 12.12V45h15a15 15 0 0115 15v100a15 15 0 01-15 15h-.86A15 15 0 01130 185zm-50-20h40a5 5 0 015 5a5 5 0 0010 0a5 5 0 015-5h5a5 5 0 005-5V60a5 5 0 00-5-5H80v12.93l8.54 8.53A5 5 0 0190 80v30a5 5 0 01-5 5h-5v25a5 5 0 01-10 0v-25h-5a5 5 0 01-5-5V80a5 5 0 011.46-3.54L70 67.93V55H55a5 5 0 00-5 5v100a5 5 0 005 5h5a5 5 0 015 5a5 5 0 0010 0a5 5 0 015-5zm-10-60h10V82.07l-5-5l-5 5zm10-60h40V28.1c-2-1.23-8.93-3.1-20-3.1s-18 1.87-20 3.1zm45 100a5 5 0 01-5-5V80a5 5 0 0110 0v60a5 5 0 01-5 5z"></path></svg></div><div class="aN1Z-restriction-icon-wrapper aN1Z-mod-size-medium"><div class="hk_J-mod-theme-INCLUDED hk_J-mod-size-medium"><svg width="18" height="18" viewBox="0 0 18 18" fill="none" xmlns="http://www.w3.org/2000/svg" role="presentation"> <circle cx="9" cy="8.99999" r="7.2" fill="#14884C"></circle> <path d="M6.1 13V11.308L9.532 8.212C9.684 8.076 9.796 7.944 9.868 7.816C9.948 7.68 9.988 7.504 9.988 7.288C9.988 7.12 9.944 6.964 9.856 6.82C9.776 6.668 9.66 6.552 9.508 6.472C9.356 6.384 9.184 6.34 8.992 6.34C8.792 6.34 8.608 6.392 8.44 6.496C8.272 6.592 8.14 6.732 8.044 6.916C7.948 7.092 7.9 7.288 7.9 7.504H5.98C5.98 6.912 6.104 6.388 6.352 5.932C6.608 5.468 6.964 5.112 7.42 4.864C7.876 4.608 8.4 4.48 8.992 4.48C9.584 4.48 10.1 4.6 10.54 4.84C10.988 5.08 11.332 5.412 11.572 5.836C11.82 6.252 11.944 6.724 11.944 7.252C11.944 7.58 11.884 7.88 11.764 8.152C11.652 8.416 11.5 8.656 11.308 8.872C11.124 9.088 10.876 9.336 10.564 9.616L8.92 11.128V11.188H11.992V13H6.1Z" fill="white"></path>  </svg></div></div></div></div><div role="button" tabindex="-1" class="MGW--wrapper" aria-describedby="tooltip-8410aee0b8529" aria-labelledby="tooltip-8410aee0b8529"><div class="aN1Z-icon-group"><div class="aN1Z-amenity-icon-wrapper"><svg width="200" height="200" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 200 200" role="presentation"><path d="M135 180H75a5 5 0 010-10h25v-20H75a5 5 0 01-4.85-3.79l-10-39.89l-.06-.21l-10-39.9A4.89 4.89 0 0150 65V35a15 15 0 0130 0v29.38L88.9 100H145a5 5 0 010 10H91.4l2.5 10H135a15 15 0 0115 15v10a5 5 0 01-5 5h-35v20h25a5 5 0 010 10zm-30-40h35v-5a5 5 0 00-5-5H90a5 5 0 01-4.85-3.79L81.1 110h-9.7l7.5 30zm-36.1-40h9.7l-8.45-33.79A4.89 4.89 0 0170 65V35a5 5 0 00-10 0v29.38z"></path></svg></div><div class="aN1Z-restriction-icon-wrapper aN1Z-mod-size-medium"><div class="hk_J-mod-theme-FLEXIBLE hk_J-mod-size-medium"><svg viewBox="0 0 200 200" width="1.25em" height="1.25em" xmlns="http://www.w3.org/2000/svg" role="presentation"><path d="M100 20c-44.16 0-80 35.84-80 80s35.84 80 80 80s80-35.84 80-80s-35.84-80-80-80zm0 128c-12.32 0-23.52-4.64-32-12.32V148H52v-27.12c.08-2.64.8-4.96 2.08-6.96c.4-.72.96-1.36 1.52-2c.64-.72 1.36-1.28 2.16-1.84c.48-.32 1.12-.64 1.68-.88c.16-.08.4-.16.64-.24c1.44-.64 3.12-.96 4.8-.96H92v16H78.8c5.68 4.96 13.12 8 21.2 8c14.88 0 27.44-10.16 30.96-24h4.16c4.48 0 8.8-.96 12.64-2.88C145.12 129.2 124.72 148 100 148zm48-68.88c-.08 2.64-.8 4.96-2.08 6.96c-.4.72-.96 1.36-1.52 2c-1.28 1.36-2.8 2.4-4.48 2.96c-1.44.64-3.12.96-4.8.96H108V76h13.2c-5.68-4.96-13.12-8-21.2-8c-14.88 0-27.44 10.16-30.96 24h-4.16c-4.48 0-8.8.96-12.64 2.88C54.88 70.8 75.28 52 100 52c12.32 0 23.52 4.64 32 12.32V52h16v27.12z"></path></svg></div></div></div></div></div></div><svg viewBox="0 0 200 200" width="1.25em" height="1.25em" xmlns="http://www.w3.org/2000/svg" class="c7_PC-chevron" role="presentation"><path d="M100 132.5c-3.873 0 .136 2.376-64.801-51.738l9.603-11.523L100 115.237l55.199-45.999l9.603 11.523C99.806 134.924 103.855 132.5 100 132.5z"></path></svg></div></div></div><div class="zx8F-price-tile"><div class="zx8F-price-section"><div class="M_JD M_JD-mod-pres-multi-fare"><div class="M_JD-large-display"><div class="oVHK"><a class="oVHK-fclink" href="/book/flight?code=mhDiJ9iGD5.Mg-fM-A4AlU.34898.3c7de670f0b983d56a8448737fbcf231&amp;h=58105b494537&amp;sub=F2177003755407400553E039f0ca4018&amp;pageOrigin=F..RP.FE.M1" target="_blank"><div class="f8F1"><div class="f8F1-above"><div class="f8F1-price-text-container"><div class="f8F1-price-text">$349</div></div></div></div></a></div><div><div class="aC3z"><div class="aC3z-links"><div class="DOum"><div class="oVHK"><a class="oVHK-fclink" href="/book/flight?code=mhDiJ9iGD5.Mg-fM-A4AlU.34898.3c7de670f0b983d56a8448737fbcf231&amp;h=58105b494537&amp;sub=F2177003755407400553E039f0ca4018&amp;pageOrigin=F..RP.FE.M1" target="_blank"><div class="DOum-option"><div class="DOum-name DOum-mod-ellipsis" title="Wanna Get Away Plus">Wanna Get Away Plus</div></div></a></div></div></div></div></div><div class="oVHK"><a class="oVHK-fclink" href="/book/flight?code=mhDiJ9iGD5.Mg-fM-A4AlU.34898.3c7de670f0b983d56a8448737fbcf231&amp;h=58105b494537&amp;sub=F2177003755407400553E039f0ca4018&amp;pageOrigin=F..RP.FE.M1" target="_blank"><div class="M_JD-provider-name">Southwest</div></a></div></div><div class="M_JD-booking-btn"><div role="listbox" class="dOAU"><div role="button" tabindex="-1" class="dOAU-best"><div class="dOAU-main-btn-wrap"><div class="oVHK"><a role="link" href="/book/flight?code=mhDiJ9iGD5.Mg-fM-A4AlU.34898.3c7de670f0b983d56a8448737fbcf231&amp;h=58105b494537&amp;sub=F2177003755407400553E039f0ca4018&amp;pageOrigin=F..RP.FE.M1" target="_blank" class="Iqt3 Iqt3-mod-stretch Iqt3-mod-bold Button-No-Standard-Style Iqt3-mod-variant-solid Iqt3-mod-theme-progress-legacy Iqt3-mod-shape-rounded-small Iqt3-mod-shape-mod-default Iqt3-mod-spacing-default Iqt3-mod-size-small" tabindex="0" aria-disabled="false"><div class="Iqt3-button-container"><div class="Iqt3-button-content"><span class="dOAU-booking-text">View Deal</span></div></div><div class="Iqt3-button-focus-outline"></div></a></div></div></div></div></div></div></div><div class="zx8F-amenity-dropdown"><div role="button" tabindex="0" class="c7_PC"><div class="c7_PC-amenity-container"><div class="ss46-icon-group-wrapper ss46-mod-gap-size-default"><div role="button" tabindex="-1" class="MGW--wrapper" aria-describedby="tooltip-458ce2374e088" aria-labelledby="tooltip-458ce2374e088"><div class="aN1Z-icon-group"><div class="aN1Z-amenity-icon-wrapper"><svg width="200" height="200" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 200 200" role="presentation"><path d="M130 185a15 15 0 01-14.14-10H84.14a15 15 0 01-28.28 0H55a15 15 0 01-15-15v-60a15 15 0 0115-15h15V27.12C70 22.6 73.9 15 100 15c11.2 0 30 1.58 30 12.12V85h15a15 15 0 0115 15v60a15 15 0 01-15 15h-.86A15 15 0 01130 185zm-50-20h40a5 5 0 015 5a5 5 0 0010 0a5 5 0 015-5h5a5 5 0 005-5v-60a5 5 0 00-5-5H55a5 5 0 00-5 5v60a5 5 0 005 5h5a5 5 0 015 5a5 5 0 0010 0a5 5 0 015-5zm0-80h40V28.1c-2-1.23-8.93-3.1-20-3.1s-18 1.87-20 3.1zm45 60a5 5 0 01-5-5v-20a5 5 0 0110 0v20a5 5 0 01-5 5zm-50 0a5 5 0 01-5-5v-20a5 5 0 0110 0v20a5 5 0 01-5 5z"></path></svg></div><div class="aN1Z-restriction-icon-wrapper aN1Z-mod-size-medium"><div class="hk_J-mod-theme-INCLUDED hk_J-mod-size-medium"><svg viewBox="0 0 200 200" width="1.25em" height="1.25em" xmlns="http://www.w3.org/2000/svg" role="presentation"><path d="M100 20c-44.183 0-80 35.817-80 80s35.817 80 80 80s80-35.817 80-80s-35.817-80-80-80zm-8.403 114.801c-8.222 8.896-16.39-1.147-38.097-17.752l12.132-15.697l17.483 13.375L131.85 62l14.65 13.401l-54.903 59.4z"></path></svg></div></div></div></div><div role="button" tabindex="-1" class="MGW--wrapper" aria-describedby="tooltip-72671f67f666d" aria-labelledby="tooltip-72671f67f666d"><div class="aN1Z-icon-group"><div class="aN1Z-amenity-icon-wrapper"><svg width="200" height="200" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 200 200" role="presentation"><path d="M130 185a15 15 0 01-14.14-10H84.14a15 15 0 01-28.28 0H55a15 15 0 01-15-15V60a15 15 0 0115-15h15V27.12C70 22.6 73.9 15 100 15c11.2 0 30 1.58 30 12.12V45h15a15 15 0 0115 15v100a15 15 0 01-15 15h-.86A15 15 0 01130 185zm-50-20h40a5 5 0 015 5a5 5 0 0010 0a5 5 0 015-5h5a5 5 0 005-5V60a5 5 0 00-5-5H80v12.93l8.54 8.53A5 5 0 0190 80v30a5 5 0 01-5 5h-5v25a5 5 0 01-10 0v-25h-5a5 5 0 01-5-5V80a5 5 0 011.46-3.54L70 67.93V55H55a5 5 0 00-5 5v100a5 5 0 005 5h5a5 5 0 015 5a5 5 0 0010 0a5 5 0 015-5zm-10-60h10V82.07l-5-5l-5 5zm10-60h40V28.1c-2-1.23-8.93-3.1-20-3.1s-18 1.87-20 3.1zm45 100a5 5 0 01-5-5V80a5 5 0 0110 0v60a5 5 0 01-5 5z"></path></svg></div><div class="aN1Z-restriction-icon-wrapper aN1Z-mod-size-medium"><div class="hk_J-mod-theme-INCLUDED hk_J-mod-size-medium"><svg width="18" height="18" viewBox="0 0 18 18" fill="none" xmlns="http://www.w3.org/2000/svg" role="presentation"> <circle cx="9" cy="8.99999" r="7.2" fill="#14884C"></circle> <path d="M6.1 13V11.308L9.532 8.212C9.684 8.076 9.796 7.944 9.868 7.816C9.948 7.68 9.988 7.504 9.988 7.288C9.988 7.12 9.944 6.964 9.856 6.82C9.776 6.668 9.66 6.552 9.508 6.472C9.356 6.384 9.184 6.34 8.992 6.34C8.792 6.34 8.608 6.392 8.44 6.496C8.272 6.592 8.14 6.732 8.044 6.916C7.948 7.092 7.9 7.288 7.9 7.504H5.98C5.98 6.912 6.104 6.388 6.352 5.932C6.608 5.468 6.964 5.112 7.42 4.864C7.876 4.608 8.4 4.48 8.992 4.48C9.584 4.48 10.1 4.6 10.54 4.84C10.988 5.08 11.332 5.412 11.572 5.836C11.82 6.252 11.944 6.724 11.944 7.252C11.944 7.58 11.884 7.88 11.764 8.152C11.652 8.416 11.5 8.656 11.308 8.872C11.124 9.088 10.876 9.336 10.564 9.616L8.92 11.128V11.188H11.992V13H6.1Z" fill="white"></path>  </svg></div></div></div></div><div role="button" tabindex="-1" class="MGW--wrapper" aria-describedby="tooltip-31d6dc0c7a756" aria-labelledby="tooltip-31d6dc0c7a756"><div class="aN1Z-icon-group"><div class="aN1Z-amenity-icon-wrapper"><svg width="200" height="200" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 200 200" role="presentation"><path d="M135 180H75a5 5 0 010-10h25v-20H75a5 5 0 01-4.85-3.79l-10-39.89l-.06-.21l-10-39.9A4.89 4.89 0 0150 65V35a15 15 0 0130 0v29.38L88.9 100H145a5 5 0 010 10H91.4l2.5 10H135a15 15 0 0115 15v10a5 5 0 01-5 5h-35v20h25a5 5 0 010 10zm-30-40h35v-5a5 5 0 00-5-5H90a5 5 0 01-4.85-3.79L81.1 110h-9.7l7.5 30zm-36.1-40h9.7l-8.45-33.79A4.89 4.89 0 0170 65V35a5 5 0 00-10 0v29.38z"></path></svg></div><div class="aN1Z-restriction-icon-wrapper aN1Z-mod-size-medium"><div class="hk_J-mod-theme-FLEXIBLE hk_J-mod-size-medium"><svg viewBox="0 0 200 200" width="1.25em" height="1.25em" xmlns="http://www.w3.org/2000/svg" role="presentation"><path d="M100 20c-44.16 0-80 35.84-80 80s35.84 80 80 80s80-35.84 80-80s-35.84-80-80-80zm0 128c-12.32 0-23.52-4.64-32-12.32V148H52v-27.12c.08-2.64.8-4.96 2.08-6.96c.4-.72.96-1.36 1.52-2c.64-.72 1.36-1.28 2.16-1.84c.48-.32 1.12-.64 1.68-.88c.16-.08.4-.16.64-.24c1.44-.64 3.12-.96 4.8-.96H92v16H78.8c5.68 4.96 13.12 8 21.2 8c14.88 0 27.44-10.16 30.96-24h4.16c4.48 0 8.8-.96 12.64-2.88C145.12 129.2 124.72 148 100 148zm48-68.88c-.08 2.64-.8 4.96-2.08 6.96c-.4.72-.96 1.36-1.52 2c-1.28 1.36-2.8 2.4-4.48 2.96c-1.44.64-3.12.96-4.8.96H108V76h13.2c-5.68-4.96-13.12-8-21.2-8c-14.88 0-27.44 10.16-30.96 24h-4.16c-4.48 0-8.8.96-12.64 2.88C54.88 70.8 75.28 52 100 52c12.32 0 23.52 4.64 32 12.32V52h16v27.12z"></path></svg></div></div></div></div></div></div><svg viewBox="0 0 200 200" width="1.25em" height="1.25em" xmlns="http://www.w3.org/2000/svg" class="c7_PC-chevron" role="presentation"><path d="M100 132.5c-3.873 0 .136 2.376-64.801-51.738l9.603-11.523L100 115.237l55.199-45.999l9.603 11.523C99.806 134.924 103.855 132.5 100 132.5z"></path></svg></div></div></div></div></div></div></div></div></div></div></div>

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
                }
            }
            catch (Exception ex)
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                Console.WriteLine("# EXCEPTION #");
                // Save html to file
                Console.WriteLine($"Saving copy of web page html to: \"err_webpage_{timestamp}.html\"");
                File.WriteAllText($"err_webpage_{timestamp}.html", driver.PageSource);                
                // Take a screenshot of the page
                Console.WriteLine($"Saving screenshot of web page to: \"err_screenshot_{timestamp}.png\"");
                driver.TakeScreenshot().SaveAsFile($"err_screenshot_{timestamp}.png");
                throw;
            }
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
            var driver = CreateDefaultWebDriver();
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
            // Spinner HTML
            //<div class="bE-8-spinner"><div class="LJld LJld-mod-theme-default" role="progressbar"><svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 100 100" preserveAspectRatio="xMidYMid"><g transform="rotate(0 50 50)"><rect x="45" y="0" rx="4.5" ry="4.5" width="9" height="28" fill="currentColor"><animate attributeName="opacity" values="1;0" keyTimes="0;1" dur="1s" begin="-0.916s" repeatCount="indefinite"></animate></rect></g><g transform="rotate(30 50 50)"><rect x="45" y="0" rx="4.5" ry="4.5" width="9" height="28" fill="currentColor"><animate attributeName="opacity" values="1;0" keyTimes="0;1" dur="1s" begin="-0.83s" repeatCount="indefinite"></animate></rect></g><g transform="rotate(60 50 50)"><rect x="45" y="0" rx="4.5" ry="4.5" width="9" height="28" fill="currentColor"><animate attributeName="opacity" values="1;0" keyTimes="0;1" dur="1s" begin="-0.75s" repeatCount="indefinite"></animate></rect></g><g transform="rotate(90 50 50)"><rect x="45" y="0" rx="4.5" ry="4.5" width="9" height="28" fill="currentColor"><animate attributeName="opacity" values="1;0" keyTimes="0;1" dur="1s" begin="-0.666s" repeatCount="indefinite"></animate></rect></g><g transform="rotate(120 50 50)"><rect x="45" y="0" rx="4.5" ry="4.5" width="9" height="28" fill="currentColor"><animate attributeName="opacity" values="1;0" keyTimes="0;1" dur="1s" begin="-0.583s" repeatCount="indefinite"></animate></rect></g><g transform="rotate(150 50 50)"><rect x="45" y="0" rx="4.5" ry="4.5" width="9" height="28" fill="currentColor"><animate attributeName="opacity" values="1;0" keyTimes="0;1" dur="1s" begin="-0.5s" repeatCount="indefinite"></animate></rect></g><g transform="rotate(180 50 50)"><rect x="45" y="0" rx="4.5" ry="4.5" width="9" height="28" fill="currentColor"><animate attributeName="opacity" values="1;0" keyTimes="0;1" dur="1s" begin="-0.416s" repeatCount="indefinite"></animate></rect></g><g transform="rotate(210 50 50)"><rect x="45" y="0" rx="4.5" ry="4.5" width="9" height="28" fill="currentColor"><animate attributeName="opacity" values="1;0" keyTimes="0;1" dur="1s" begin="-0.333s" repeatCount="indefinite"></animate></rect></g><g transform="rotate(240 50 50)"><rect x="45" y="0" rx="4.5" ry="4.5" width="9" height="28" fill="currentColor"><animate attributeName="opacity" values="1;0" keyTimes="0;1" dur="1s" begin="-0.25s" repeatCount="indefinite"></animate></rect></g><g transform="rotate(270 50 50)"><rect x="45" y="0" rx="4.5" ry="4.5" width="9" height="28" fill="currentColor"><animate attributeName="opacity" values="1;0" keyTimes="0;1" dur="1s" begin="-0.166s" repeatCount="indefinite"></animate></rect></g><g transform="rotate(300 50 50)"><rect x="45" y="0" rx="4.5" ry="4.5" width="9" height="28" fill="currentColor"><animate attributeName="opacity" values="1;0" keyTimes="0;1" dur="1s" begin="-0.083s" repeatCount="indefinite"></animate></rect></g><g transform="rotate(330 50 50)"><rect x="45" y="0" rx="4.5" ry="4.5" width="9" height="28" fill="currentColor"><animate attributeName="opacity" values="1;0" keyTimes="0;1" dur="1s" begin="0s" repeatCount="indefinite"></animate></rect></g></svg></div></div>
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

    private static WebDriver CreateDefaultWebDriver()
    {

        //If we are running on the server, there shouldn't be any chrome processes running
        bool isServer = Environment.GetEnvironmentVariable("OS") is not string s || !s.Contains("Windows", StringComparison.OrdinalIgnoreCase);

        if (isServer)
        {
            // Kill any existing chromedriver processes on the server, if they exist
            Console.WriteLine("Running on server, checking for existing chromedriver processes");
            var chromeDriverProcesses = Process.GetProcesses().Where(p => p.ProcessName.StartsWith("chrome")).ToList();
            if (chromeDriverProcesses.Count > 0)
            {
                Console.WriteLine("Found existing chromedriver processes... killing");
                foreach (var process in Process.GetProcesses())
                {
                    Console.Write($"{process.StartTime} - #{process.Id} - {process.ProcessName}");
                    if (process.ProcessName.StartsWith("chrome"))
                    {
                        Console.Write($" -- ## KILLING ##");
                        process.Kill();
                    }
                    Console.WriteLine();
                }
                Console.WriteLine("Done");
            }
        }
    

        // Set up the Chrome driver options
        var options = new ChromeOptions();
    
        // Prevet bot detection
        options.AddArgument("user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36");
        // Other user agents:
        // "user-agent=Mozilla/5.0 (Windows NT 10.0; WOW64; rv:50.0) Gecko/20100101 Firefox/50.0");
        // "user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");
        // "user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36 Edge/16.16299");
        // "user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36 OPR/45.0.2552.898");

        if (isServer)
        {
            options.AddArgument("loadImages=false");
            options.AddArgument("--headless"); // If you don't need the GUI

            options.AddArgument("--no-sandbox"); // This is often necessary for running in environments like containers or VMs
            options.AddArgument("--disable-dev-shm-usage"); // Overcome limited resource problems in some environments
            options.AddArgument("--disable-gpu"); // Applicable mainly for Windows, but can be useful in other scenarios too
            options.AddArgument("--remote-debugging-port=9222"); // Helps in avoiding some issues related to DevTools
        }

        var driverService =  ChromeDriverService.CreateDefaultService();
        driverService.HideCommandPromptWindow = true;
      
        var driver = new ChromeDriver(driverService, options);
        return driver;
    }

    // Example URL: https://www.kayak.com/flights/AUS-BOI/2024-09-20?sort=price_a
    private static string BuildFlightSearchBasePathUrl(string originItaCode, string destinationItaCode, DateOnly departureDate)
       => $"https://www.kayak.com/flights/{originItaCode}-{destinationItaCode}/{departureDate:yyyy-MM-dd}";
}