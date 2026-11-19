using disaster_platform_web.Clients;

namespace disaster_platform_web.Areas.Recursos.Clients;

public interface IRecursosClient : IIntegrationClient { }

public class FakeRecursosClient : FakePantallaClient, IRecursosClient { }

public class RecursosClient(HttpClient http) : HttpPantallaClient(http), IRecursosClient { }
