namespace disaster_platform_web.Clients;

public class ApiOptions
{
    public string GatewayUrl { get; set; } = "";       // Desastres, Daños, Reconstrucción
    public string IntegrationUrl { get; set; } = "";   // Personas, Organizaciones (Integration Service -> Eden)
    public bool UseMock { get; set; } = true;
}
