using disaster_platform_web.Clients;

namespace disaster_platform_web.Areas.Reconstruccion.Clients;

public interface IReconstruccionClient : IPantallaClient { }

public class FakeReconstruccionClient : FakePantallaClient, IReconstruccionClient { }

public class ReconstruccionClient(HttpClient http) : HttpPantallaClient(http), IReconstruccionClient { }
