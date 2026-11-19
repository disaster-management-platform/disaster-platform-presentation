using disaster_platform_web.Clients;
using disaster_platform_web.Screens;

namespace disaster_platform_web.Areas.Organizaciones.Clients;

public interface IOrganizacionesClient : IIntegrationClient { }

public class FakeOrganizacionesClient : FakePantallaClient, IOrganizacionesClient
{
    public override Task<IReadOnlyList<Row>?> FilasAsync(
        Screen s, Col[] cols, string? rowTo, string? id, int sub, CancellationToken ct)
    {
        if (s.Id == "ORG-01")
            return Task.FromResult<IReadOnlyList<Row>?>(OrganizacionesSeed.Organizations.Select(OrganizationRow).ToArray());

        if (s.Id == "ORG-04")
            return Task.FromResult<IReadOnlyList<Row>?>(OrganizacionesSeed.Tasks.Select(TaskRow).ToArray());

        if (s.Id == "ORG-02")
        {
            var organization = OrganizacionesSeed.FindOrganization(id);
            if (sub == 1 && organization is not null)
                return Task.FromResult<IReadOnlyList<Row>?>(OrganizacionesSeed.Tasks
                    .Where(task => task.OrganizationId.Equals(organization.Id, StringComparison.OrdinalIgnoreCase))
                    .Select(TaskAssignmentRow).ToArray());
            if (sub == 2)
            {
                var integration = organization?.IntegrationDetails;
                IReadOnlyList<Row> rows = integration is null
                    ? []
                    : [new Row(null,
                    [
                        (integration.System, false), (integration.Direction, false), (integration.Mechanism, false),
                        (integration.Information, false), (integration.Modules, false)
                    ])];
                return Task.FromResult<IReadOnlyList<Row>?>(rows);
            }
        }

        return base.FilasAsync(s, cols, rowTo, id, sub, ct);
    }

    public override Task<IReadOnlyDictionary<string, string>?> DetalleAsync(Screen s, string? id, CancellationToken ct)
    {
        if (s.Id == "ORG-02" && OrganizacionesSeed.FindOrganization(id) is { } organization)
            return Task.FromResult<IReadOnlyDictionary<string, string>?>(new Dictionary<string, string>
            {
                ["name"] = organization.Name,
                ["acronym"] = organization.Acronym,
                ["category"] = organization.Category,
                ["scope"] = organization.Scope,
                ["country"] = organization.Country,
                ["location"] = organization.Location,
                ["phone"] = organization.Phone,
                ["email"] = organization.Email,
                ["description"] = organization.Description,
                ["actors"] = organization.Actors,
                ["channel"] = organization.Channel,
            });

        if (s.Id == "ORG-08" && OrganizacionesSeed.FindTask(id) is { } task)
            return Task.FromResult<IReadOnlyDictionary<string, string>?>(new Dictionary<string, string>
            {
                ["name"] = task.Name,
                ["description"] = task.Description,
                ["disaster"] = task.Disaster,
                ["organization"] = task.Organization,
                ["suggestedPriority"] = task.SuggestedPriority,
                ["confirmedPriority"] = task.ConfirmedPriority,
                ["status"] = task.Status,
                ["dueAt"] = task.DueAt,
                ["version"] = task.Version,
            });

        return base.DetalleAsync(s, id, ct);
    }

    static Row OrganizationRow(OrganizationSeed organization) =>
        new(Catalog.Url("ORG-02", organization.Id),
        [
            (organization.Name, false), (organization.Acronym, false), (organization.Category, false),
            (organization.Scope, false), (organization.Role, false), (organization.Integration, false),
            (organization.Status, true)
        ], organization.Id);

    static Row TaskRow(OrganizationTaskSeed task) =>
        new(Catalog.Url("ORG-08", task.Id),
        [
            (task.Name, false), (task.Disaster, false), (task.Organization, false),
            (task.SuggestedPriority, true), (task.ConfirmedPriority, true), (task.Status, true),
            (task.DueAt, false)
        ], task.Id);

    static Row TaskAssignmentRow(OrganizationTaskSeed task) =>
        new(Catalog.Url("ORG-08", task.Id),
        [
            (task.Name, false), (task.ConfirmedPriority, true), (task.Status, true), (task.DueAt, false)
        ], task.Id);
}

public class OrganizacionesClient(HttpClient http) : HttpPantallaClient(http), IOrganizacionesClient { }
