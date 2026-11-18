using disaster_platform_web.Clients;

namespace disaster_platform_web.Areas.Desastres.Clients;

public interface IDesastresClient : IPantallaClient { }

public class FakeDesastresClient : FakePantallaClient, IDesastresClient { }

public class DesastresClient(HttpClient http) : HttpPantallaClient(http), IDesastresClient { }
