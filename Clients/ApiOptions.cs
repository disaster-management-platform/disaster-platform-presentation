namespace disaster_platform_web.Clients;

public class ApiOptions
{
    public string EdenUrl { get; set; } = "http://localhost:8001/eden_default";
    public string ReverseGeocodingUrl { get; set; } = "https://api.bigdatacloud.net/data/reverse-geocode-client";
    public string GatewayUrl { get; set; } = "";       // Desastres, Daños, Reconstrucción
    public bool UseMock { get; set; } = false;
}
