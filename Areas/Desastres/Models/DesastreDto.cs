using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace disaster_platform_web.Areas.Desastres.Models;

public record DesastreDto(int Id, string Nombre, string Ubicacion, string Descripcion,
    int Severidad, double Latitud, double Longitud, double? Radio, string Phase, long Version);

public class DesastreInput
{
    [JsonIgnore] public Guid Key { get; set; } = Guid.NewGuid();   // Idempotency-Key estable, va en hidden
    [Required, StringLength(120)] public string Nombre { get; set; } = "";
    [Required, StringLength(200)] public string Ubicacion { get; set; } = "";
    [Required, StringLength(2000)] public string Descripcion { get; set; } = "";
    [Range(1, 5)] public int Severidad { get; set; } = 1;
    [Range(-90, 90)] public double Latitud { get; set; }
    [Range(-180, 180)] public double Longitud { get; set; }
    [Range(0.01, 1_000_000)] public double? Radio { get; set; }
}

public record DesastreListaVm(TableVm Tabla);