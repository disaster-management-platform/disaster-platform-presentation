namespace disaster_platform_web.Screens;

public static class Etiquetas
{
    static readonly IReadOnlyDictionary<string, string> Traducciones = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Proposed"] = "Propuesta",
        ["Selected"] = "Seleccionada",
        ["Obsolete"] = "Obsoleta",
        ["Draft"] = "Borrador",
        ["Formalized"] = "Formalizado",
        ["Monitoring"] = "En monitoreo",
        ["Closed"] = "Cerrado",
        ["Pending"] = "Pendiente",
        ["InProgress"] = "En ejecución",
        ["Completed"] = "Terminado",
        ["High"] = "Alta",
        ["Low"] = "Baja",
        ["Unassigned"] = "Sin presupuesto",
        ["None"] = "Sin daño",
        ["Minor"] = "Leve",
        ["Moderate"] = "Moderado",
        ["Severe"] = "Grave",
        ["Destroyed"] = "Destrucción total",
        ["Registered"] = "Registrado",
        ["Response"] = "En atención",
        ["Recovery"] = "En recuperación",
    };

    public static string Es(string raw) => Traducciones.GetValueOrDefault(raw, raw);
}
