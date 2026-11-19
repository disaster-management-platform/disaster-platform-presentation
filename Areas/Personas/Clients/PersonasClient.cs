using disaster_platform_web.Clients;

namespace disaster_platform_web.Areas.Personas.Clients;

public interface IPersonasClient : IIntegrationClient { }

public class FakePersonasClient : FakePantallaClient, IPersonasClient { }

public class PersonasClient(HttpClient http) : HttpPantallaClient(http), IPersonasClient { }
