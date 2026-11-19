namespace disaster_platform_web.Screens;

public enum Kind { Auth, Dashboard, List, Detail, Form }
public enum CT { Text, Date, Badge, Money, Number }
public enum FT { Text, TextArea, Number, Date, DateTime, Select, Email, Phone, File, Bool }

public record Col(string Key, string Label, CT Type = CT.Text, string[]? Sample = null);

public record Field
{
    public string Name { get; init; } = "";
    public string Label { get; init; } = "";
    public FT Type { get; init; }
    public bool Required { get; init; }
    public int Max { get; init; }
    public double? Min { get; init; }
    public double? Maximum { get; init; }
    public bool NoFuture { get; init; }
    public string[]? Options { get; init; }
    public string? Hint { get; init; }
}

public record SubTable(string Title, Col[] Cols, string? RowTo);
public record Dialog(string Id, string Label, string Title, Field[] Fields)
{
    public string? ConfirmationText { get; init; }
}
public record Stat(string Label, string Value);
public record Link(string Label, string To, bool Primary = false);

public record Screen
{
    public string Id { get; init; } = "";
    public string Module { get; init; } = "";
    public string Title { get; init; } = "";
    public string Route { get; init; } = "";
    public Kind Kind { get; init; }
    public int? Stage { get; init; }
    public string? Intro { get; init; }
    public Col[] Cols { get; init; } = [];
    public Field[] Fields { get; init; } = [];
    public Field[] Filters { get; init; } = [];
    public Stat[] Stats { get; init; } = [];
    public Link[] Links { get; init; } = [];
    public SubTable[] Tables { get; init; } = [];
    public Dialog[] Dialogs { get; init; } = [];
    public string? TableTitle { get; init; }
    public string? ChartTitle { get; init; }
    public string? RowTo { get; init; }
    public string? SaveTo { get; init; }
    public string? CancelTo { get; init; }
    public bool Map { get; init; }
    public bool Chart { get; init; }
    public bool Selectable { get; init; }
    public bool Optional { get; init; }
    public bool Print { get; init; }
    public char State { get; init; } = 'N';
    public string Api { get; init; } = "";
}

// ViewModels de los parciales
public record TableVm(string? Title, Col[] Cols, string? RowTo, Field[] Filters, bool Search = true)
{
    public IEnumerable<Row>? Rows { get; init; }
    public bool Selectable { get; init; }
}
public record FieldVm(Field F, string? Value, string? Error, string Prefix);
public record Row(string? Href, (string Text, bool Badge)[] Cells, string? Id = null);