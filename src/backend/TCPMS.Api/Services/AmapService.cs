using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using TCPMS.Api.Contracts;

namespace TCPMS.Api.Services;

public sealed class AmapOptions
{
    public string ServerKey { get; set; } = string.Empty;
    public string MiniProgramKey { get; set; } = string.Empty;
    public double DefaultRadiusKm { get; set; } = 50;
}

public sealed class AmapService(HttpClient httpClient, IOptions<AmapOptions> options, ILogger<AmapService> logger)
{
    private readonly AmapOptions _options = options.Value;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ServerKey);

    public async Task<GeoResult?> GeocodeAsync(string address, string? city, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("高德服务端 Key 尚未配置");
        }

        var query = new Dictionary<string, string?>
        {
            ["key"] = _options.ServerKey,
            ["address"] = address,
            ["city"] = city,
            ["output"] = "json"
        };
        var payload = await GetAsync<AmapGeocodeResponse>("v3/geocode/geo", query, cancellationToken);
        var geocode = payload?.Geocodes?.FirstOrDefault();
        if (geocode?.Location is null || !TryParseLocation(geocode.Location, out var longitude, out var latitude))
        {
            return null;
        }

        return new GeoResult(
            longitude,
            latitude,
            geocode.FormattedAddress,
            geocode.Province,
            geocode.City,
            geocode.District,
            geocode.Adcode,
            geocode.Level);
    }

    public async Task<GeoResult?> ReverseGeocodeAsync(decimal longitude, decimal latitude, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("高德服务端 Key 尚未配置");
        }

        var query = new Dictionary<string, string?>
        {
            ["key"] = _options.ServerKey,
            ["location"] = $"{longitude:F6},{latitude:F6}",
            ["extensions"] = "all",
            ["output"] = "json"
        };
        var payload = await GetAsync<AmapReverseGeocodeResponse>("v3/geocode/regeo", query, cancellationToken);
        var component = payload?.Regeocode?.AddressComponent;
        return new GeoResult(
            longitude,
            latitude,
            payload?.Regeocode?.FormattedAddress,
            component?.Province,
            component?.City,
            component?.District,
            component?.Adcode,
            component?.Towncode);
    }

    private async Task<T?> GetAsync<T>(
        string path,
        IReadOnlyDictionary<string, string?> query,
        CancellationToken cancellationToken)
    {
        var queryString = string.Join(
            "&",
            query
                .Where(x => !string.IsNullOrWhiteSpace(x.Value))
                .Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value!)}"));
        var uri = new Uri($"https://restapi.amap.com/{path}?{queryString}");

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                using var response = await httpClient.GetAsync(uri, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"高德接口返回 {(int)response.StatusCode} {response.ReasonPhrase}");
                }

                var payload = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
                return payload;
            }
            catch (Exception ex) when (attempt < 3 && ex is HttpRequestException or TaskCanceledException)
            {
                logger.LogWarning(ex, "高德接口调用失败，将进行第 {Attempt} 次重试", attempt + 1);
                await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt), cancellationToken);
            }
        }

        return default;
    }

    private static bool TryParseLocation(string location, out decimal longitude, out decimal latitude)
    {
        longitude = 0;
        latitude = 0;
        var parts = location.Split(',', StringSplitOptions.TrimEntries);
        return parts.Length == 2
            && decimal.TryParse(parts[0], out longitude)
            && decimal.TryParse(parts[1], out latitude);
    }

    private sealed class AmapGeocodeResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("geocodes")]
        public List<AmapGeocode>? Geocodes { get; set; }
    }

    private sealed class AmapGeocode
    {
        [JsonPropertyName("formatted_address")]
        public string? FormattedAddress { get; set; }

        [JsonPropertyName("province")]
        public string? Province { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("district")]
        public string? District { get; set; }

        [JsonPropertyName("adcode")]
        public string? Adcode { get; set; }

        [JsonPropertyName("location")]
        public string? Location { get; set; }

        [JsonPropertyName("level")]
        public string? Level { get; set; }
    }

    private sealed class AmapReverseGeocodeResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("regeocode")]
        public AmapRegeocode? Regeocode { get; set; }
    }

    private sealed class AmapRegeocode
    {
        [JsonPropertyName("formatted_address")]
        public string? FormattedAddress { get; set; }

        [JsonPropertyName("addressComponent")]
        public AmapAddressComponent? AddressComponent { get; set; }
    }

    private sealed class AmapAddressComponent
    {
        [JsonPropertyName("province")]
        public string? Province { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("district")]
        public string? District { get; set; }

        [JsonPropertyName("adcode")]
        public string? Adcode { get; set; }

        [JsonPropertyName("towncode")]
        public string? Towncode { get; set; }
    }
}
