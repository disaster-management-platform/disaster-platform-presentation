using disaster_platform_web.Clients;

namespace disaster_platform_web.Areas.Danos.Clients;

public interface IDanosClient : IPantallaClient { }

public class FakeDanosClient : FakePantallaClient, IDanosClient { }

public class DanosClient(HttpClient http) : HttpPantallaClient(http), IDanosClient { }
