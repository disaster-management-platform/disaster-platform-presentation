namespace disaster_platform_web.Clients;

// Marca para los clientes que hablan con el Integration Service (Eden vive detrás de él, el front nunca llama a Eden).
public interface IIntegrationClient : IPantallaClient { }
