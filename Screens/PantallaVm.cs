namespace disaster_platform_web.Screens;

// ViewModel único de las pantallas del catálogo. Lo arman los controllers de cada área.
public class PantallaVm
{
    public Screen Screen { get; set; } = default!;
    public string? RouteId { get; set; }
    public Screen? Parent { get; set; }
    public string? ProjectStatus { get; set; }
    public bool CanFinalize { get; set; }
    public int? ActivitiesWithoutReports { get; set; }
    public Guid Key { get; set; } = Guid.NewGuid();   // Idempotency-Key del envío

    public Dictionary<string, string?> Values { get; set; } = new();
    public Dictionary<string, string> Errors { get; } = new();
    public string? Flash { get; set; }
    public string? FlashError { get; set; }

    // Datos que entrega el cliente (null = sin datos, la vista usa los de ejemplo)
    public IReadOnlyList<Row>? Filas { get; set; }
    public Dictionary<int, IReadOnlyList<Row>> FilasSub { get; } = new();
    public IReadOnlyDictionary<string, string>? Detalle { get; set; }
}
