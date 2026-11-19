using disaster_platform_web.Clients;

namespace disaster_platform_web.Areas.Sistema.Clients;

public interface ISistemaClient : IPantallaClient { }

public class FakeSistemaClient : FakePantallaClient, ISistemaClient { }

public class SistemaClient(HttpClient http) : HttpPantallaClient(http), ISistemaClient { }
