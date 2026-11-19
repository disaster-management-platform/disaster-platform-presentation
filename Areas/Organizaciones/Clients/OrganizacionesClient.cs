using disaster_platform_web.Clients;

namespace disaster_platform_web.Areas.Organizaciones.Clients;

public interface IOrganizacionesClient : IIntegrationClient { }

public class FakeOrganizacionesClient : FakePantallaClient, IOrganizacionesClient { }

public class OrganizacionesClient(HttpClient http) : HttpPantallaClient(http), IOrganizacionesClient { }
