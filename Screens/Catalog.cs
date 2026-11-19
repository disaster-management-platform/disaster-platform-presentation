using System.Text.RegularExpressions;

namespace disaster_platform_web.Screens;

public static class Catalog
{
    static readonly Regex ParamRx = new(@"^\{id(?::(\w+))?\}$");
    public static readonly IReadOnlyList<Screen> All = Build();
    static readonly Dictionary<string, Screen> ById = All.ToDictionary(s => s.Id);
    static readonly List<(Screen S, Regex Rx)> Matchers = All
        .Select(s => (S: s, Rx: Compile(s.Route)))
        .OrderBy(x => x.S.Route.Count(ch => ch == '{'))
        .ThenByDescending(x => x.S.Route.Length)
        .ToList();

    // ---------- Rutas ----------
    static Regex Compile(string route)
    {
        var parts = route.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries).Select(seg =>
        {
            var m = ParamRx.Match(seg);
            if (!m.Success) return Regex.Escape(seg);
            return m.Groups[1].Value switch
            {
                "int" => @"(?<id>\d+)",
                "guid" => @"(?<id>[0-9a-fA-F]{8}(?:-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12})",
                _ => @"(?<id>[^/]+)"
            };
        });
        return new Regex("^" + string.Join("/", parts) + "$", RegexOptions.IgnoreCase);
    }

    public static (Screen Screen, string? Id)? Match(string path)
    {
        var p = path.Trim('/');
        foreach (var (s, rx) in Matchers)
        {
            var m = rx.Match(p);
            if (m.Success) return (s, m.Groups["id"].Success ? m.Groups["id"].Value : null);
        }
        return null;
    }

    public static Screen? ParentOf(string path)
    {
        var segs = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segs.Length == 0) return null;
        return Match(string.Join('/', segs[..^1]))?.Screen;
    }

    public static Screen Get(string id) => ById[id];
    public static string? DashboardId(string module) =>
        All.FirstOrDefault(s => s.Module == module && !s.Route.Contains('/'))?.Id;
    public static string Url(string to, string? current = null) => Build(to, current, 0);
    public static string RowUrl(string to, int i) => Build(to, null, i);

    static bool Fits(string c, string v) => c switch
    {
        "int" => v.All(char.IsDigit),
        "guid" => Guid.TryParse(v, out _),
        _ => true
    };

    static string Sample(string c, int i) => c switch
    {
        "int" => (i + 1).ToString(),
        "guid" => new Guid(i + 1, 0, 0, new byte[8]).ToString(),
        _ => $"p-{i + 1:0000}"
    };

    static string Build(string to, string? current, int i)
    {
        var s = ById[to];
        var segs = new List<string>();
        var hasParam = false;
        foreach (var seg in s.Route.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            var m = ParamRx.Match(seg);
            if (!m.Success) { segs.Add(seg); continue; }
            hasParam = true;
            var c = m.Groups[1].Value;
            segs.Add(current is not null && Fits(c, current) ? current : Sample(c, i));
        }
        var url = "/" + string.Join('/', segs);
        // Mantiene el contexto de desastre al navegar a pantallas sin id propio
        if (!hasParam && current is not null && current.All(char.IsDigit)) url += "?disasterId=" + current;
        return url;
    }

    // ---------- Constructores cortos ----------
    static Col C(string k, string l, CT t = CT.Text, string? s = null) => new(k, l, t, s?.Split('|'));

    static Field F(string n, string l, FT t = FT.Text, bool req = false, int max = 0, string? hint = null,
                   string? o = null, double? min = null, double? hi = null, bool noFuture = false) =>
        new() { Name = n, Label = l, Type = t, Required = req, Max = max, Hint = hint,
                Options = o?.Split('|'), Min = min, Maximum = hi, NoFuture = noFuture };

    static Link L(string label, string to, bool primary = false) => new(label, to, primary);
    static Dialog D(string id, string label, string title, params Field[] f) => new(id, label, title, f);
    static SubTable T(string title, string? rowTo, params Col[] c) => new(title, c, rowTo);
    static Stat S(string l, string v) => new(l, v);

    // ---------- Catálogo de las 56 pantallas ----------
    static List<Screen> Build()
    {
        const string AUT = "Autenticación", HOM = "Inicio", DIS = "Gestión de desastres", DAM = "Evaluación de daños",
                     PEO = "Personas afectadas", ORG = "Coordinación de organizaciones", REC = "Reconstrucción",
                     RES = "Gestión de recursos", ADM = "Administración", AUD = "Auditoría", INT = "Integraciones";

        Screen M(string id, string mod, string title, string route, Kind k, char st, string api) =>
            new()
            {
                Id = id, Module = mod, Title = title, Route = route, Kind = k, State = st, Api = api,
                Intro = k switch
                {
                    Kind.Dashboard => $"Consulta la información principal de {mod.ToLowerInvariant()} y accede a sus herramientas.",
                    Kind.List => "Consulta los registros disponibles y realiza las acciones de esta sección.",
                    Kind.Detail => "Consulta la información y el historial de este registro.",
                    Kind.Form => "Completa la información solicitada para continuar.",
                    _ => null
                }
            };

        // Opciones compartidas
        const string sev = "1|2|3|4|5";
        const string fases = "Registered|Response|Recovery|Closed";
        const string tipos = "Terremoto|Inundación|Incendio|Deslizamiento|Huracán|Erupción volcánica|Otro";
        const string orgs = "Bomberos|Policía|Defensa Civil|Servicios médicos|Cruz Roja|Autoridad local|Autoridad regional";
        const string danio = "None|Minor|Moderate|Severe|Destroyed";
        const string activos = "Vivienda|Edificio público|Hospital|Escuela|Puente|Carretera|Sistema de agua|Red eléctrica|Telecomunicaciones|Instalación industrial";
        const string desastres = "Sismo Armenia|Inundación Cali|Deslizamiento Manizales|Incendio Quibdó";
        const string prios = "Alta|Media|Baja";
        const string estTarea = "Pendiente|En curso|Completada";
        const string estProy = "Draft|Formalized|Monitoring|Closed";
        const string canales = "SMS|Correo|Push";
        const string categoriasRecursos = "Alimentos|Agua|Medicamentos|Vehículos|Maquinaria|Equipos de rescate|Personal|Refugios|Donaciones";
        const string unidadesRecursos = "litros|kits|cajas|unidades|personas";
        const string estadosRecursos = "Disponible|Asignado parcialmente|Agotado|En tránsito";
        const string bodegas = "Bodega Armenia|Bodega Cali|Bodega Manizales|Bodega Quibdó";
        const string nivelesAtencion = "Nivel_1 — Bajo|Nivel_2 — Medio|Nivel_3 — Alto|Nivel_4 — Crítico";

        // Columnas reutilizadas
        var disCols = new[]
        {
            C("nombre", "Nombre", s: desastres),
            C("tipo", "Tipo", s: "Terremoto|Inundación|Deslizamiento|Incendio"),
            C("ubicacion", "Ubicación", s: "Armenia, Quindío|Cali, Valle|Manizales, Caldas|Quibdó, Chocó"),
            C("inicio", "Inicio", CT.Date),
            C("severidad", "Severidad", CT.Badge, "4|3|3|2"),
            C("phase", "Fase", CT.Badge, "Response|Registered|Recovery|Closed"),
        };
        var evalCols = new[]
        {
            C("assetId", "Activo", s: "PUENTE-014|ESC-203|HOSP-007|VIV-1180|VIA-032"),
            C("assetType", "Tipo", s: "Puente|Escuela|Hospital|Vivienda|Carretera"),
            C("location", "Zona", s: "Armenia|Calarcá|Cali|Manizales"),
            C("evaluatedAt", "Fecha", CT.Date),
            C("evaluator", "Evaluador", s: "L. Ramírez|M. Torres|J. Pardo"),
            C("damageLevel", "Daño", CT.Badge, "Destroyed|Severe|Moderate|Minor|None"),
            C("revision", "Rev.", CT.Number),
            C("status", "Estado", CT.Badge, "Validated|Draft|Rejected|Validated"),
        };
        var taskCols = new[]
        {
            C("name", "Tarea", s: "Despejar vía principal|Instalar refugio|Inventario de agua|Evaluar puente|Censo de familias"),
            C("priorityId", "Prioridad", CT.Badge, prios),
            C("assignee", "Responsable", s: "Defensa Civil|Cruz Roja|Bomberos|J. Pardo"),
            C("statusId", "Estado", CT.Badge, estTarea),
            C("dueAt", "Vence", CT.Date),
            C("timeEstimated", "H. estimadas", CT.Number),
            C("timeActual", "H. reales", CT.Number),
        };
        var alertCols = new[]
        {
            C("subject", "Asunto", s: "Réplica de magnitud 4.8|Evacuación zona norte|Cierre vía Armenia-Calarcá|Apertura de refugio"),
            C("recipientIds", "Destinatarios", s: "Bomberos, Cruz Roja|Todos los organismos|Defensa Civil"),
            C("channel", "Canal", CT.Badge, canales),
            C("occurredAt", "Fecha", CT.Date),
            C("status", "Estado", CT.Badge, "Entregada|Pendiente|Fallida|Entregada"),
        };
        var solicitudesRecursosCols = new[]
        {
            C("codigo", "Solicitud", s: "SOL-0101|SOL-0102|SOL-0103"),
            C("desastre", "Desastre", s: desastres),
            C("solicitante", "Solicitante", s: orgs),
            C("recurso", "Recurso", s: "Agua potable|Carpas|Medicamentos|Camión cisterna"),
            C("solicitada", "Solicitada", CT.Number), C("asignada", "Asignada", CT.Number),
            C("prioridad", "Prioridad", CT.Badge, prios),
            C("estado", "Estado", CT.Badge, "Pendiente|Aprobada|Asignada|Entregada|Rechazada"),
            C("fecha", "Fecha", CT.Date),
        };
        var activityDlg = new[]
        {
            F("name", "Nombre", req: true, max: 200),
            F("statusId", "Estado", FT.Select, o: estTarea),
            F("startsAt", "Inicio", FT.DateTime),
            F("endsAt", "Fin", FT.DateTime),
            F("comments", "Observaciones", FT.TextArea),
        };
        var projActCols = new[]
        {
            C("name", "Actividad", s: "Demolición|Cimentación|Estructura|Acabados|Entrega"),
            C("responsible", "Responsable", s: "Ing. Rojas|Ing. Díaz"),
            C("startsAt", "Inicio", CT.Date),
            C("dueAt", "Vence", CT.Date),
            C("status", "Estado", CT.Badge, "Completed|InProgress|Pending"),
            C("budget", "Presupuesto", CT.Money),
        };

        return new List<Screen>
        {
            // ===== 1. Autenticación =====
            M("AUTH", AUT, "Acceso a la plataforma", "Cuenta/Acceder", Kind.Auth, 'N',
              "OIDC Authorization Code + PKCE, callback /signin-oidc (Keycloak)") with
            {
                Intro = "Accede a la plataforma para consultar información y coordinar la atención de emergencias.",
                Links = [L("Ingresar con Keycloak", "HOME", true)],
            },

            // ===== 2. Principal =====
            M("HOME", HOM, "Centro de operaciones", "", Kind.Dashboard, 'A',
              "GET /api/desastres; GET /api/reconstruccion/proyectos. Daños, personas y alertas son N/E") with
            {
                Intro = "Consulta de un vistazo la situación de las emergencias y las alertas recientes.",
                Stats = [S("Desastres activos", "3"), S("Evaluaciones", "128"), S("Personas afectadas", "1.240"), S("Proyectos", "12")],
                Map = true,
                TableTitle = "Alertas recientes",
                Cols = alertCols,
            },

            // ===== 3. Gestión de desastres =====
            M("DIS-01", DIS, "Resumen de desastres", "Desastres", Kind.Dashboard, 'A', "GET /api/desastres (array). N geometria") with
            {
                Intro = "Consulta los desastres activos, su ubicación y las alertas de cada emergencia.",
                Stats = [S("Activos", "3"), S("Críticos", "1")],
                Map = true,
                TableTitle = "Desastres recientes",
                Cols = disCols,
                RowTo = "DIS-03",
                Filters = [F("tipo", "Tipo", o: "Terremoto|Inundación|Deslizamiento|Incendio"),
                           F("phase", "Fase", o: fases), F("ubicacion", "Zona", o: "Armenia|Cali|Manizales|Quibdó")],
                Links = [L("Registrar desastre", "DIS-04", true), L("Listado", "DIS-02"),
                         L("Fuentes", "DIS-05"), L("Organismos", "DIS-06"), L("Alertas", "DIS-07"), L("Evolución", "DIS-08")],
            },
            M("DIS-02", DIS, "Listado de desastres", "Desastres/Lista", Kind.List, 'A', "GET /api/desastres. No enviar query params no soportados") with
            {
                Cols = disCols,
                RowTo = "DIS-03",
                Filters = [F("tipo", "Tipo", o: "Terremoto|Inundación|Deslizamiento|Incendio"),
                           F("phase", "Fase", o: fases), F("severidad", "Severidad", o: sev)],
                Links = [L("Registrar desastre", "DIS-04", true)],
            },
            M("DIS-03", DIS, "Detalle del desastre", "Desastres/{id:int}", Kind.Detail, 'A',
              "GET /api/desastres/{id}; PUT /api/desastres/{id}/fase. N severidad, geometria, historial") with
            {
                Map = true,
                Cols =
                [
                    C("nombre", "Nombre", s: "Sismo Armenia"), C("tipoId", "Tipo", s: "Terremoto"),
                    C("ubicacion", "Ubicación", s: "Armenia, Quindío"),
                    C("descripcion", "Descripción", s: "Sismo de magnitud 6.1 con múltiples réplicas en el Eje Cafetero."),
                    C("severidad", "Severidad", CT.Badge, "4"), C("phase", "Fase", CT.Badge, "Response"),
                    C("version", "Versión", CT.Number), C("latitud", "Latitud", s: "4.5339"), C("longitud", "Longitud", s: "-75.6811"),
                    C("radio", "Radio (m)", s: "25000"), C("inicio", "Inicio", CT.Date), C("actualizadoAt", "Actualizado", CT.Date),
                    C("responsableId", "Responsable", s: "Defensa Civil"),
                ],
                Tables = [T("Historial de cambios", null,
                    C("occurredAt", "Fecha", CT.Date), C("eventType", "Evento", s: "Fase|Severidad|Área"),
                    C("actor", "Actor", s: "j.pardo|m.torres"), C("before", "Antes", s: "Registered|3|3 barrios"),
                    C("after", "Después", s: "Response|4|7 barrios"), C("reason", "Motivo", s: "Nueva evaluación"))],
                Dialogs =
                [
                    D("fase", "Cambiar fase", "Cambiar fase", F("phase", "Nueva fase", FT.Select, req: true, o: fases),
                      F("reason", "Motivo", FT.TextArea, req: true, max: 1000)),
                    D("sev", "Cambiar severidad", "Cambiar severidad", F("severidad", "Severidad", FT.Select, req: true, o: sev),
                      F("reason", "Motivo", FT.TextArea, req: true, max: 1000)),
                    D("geo", "Actualizar geometría", "Actualizar geometría",
                      F("geometria", "GeoJSON (Polygon o MultiPolygon)", FT.TextArea, req: true, hint: "Coordenadas [longitud, latitud]")),
                ],
                Links = [L("Daños", "DAM-01"), L("Personas", "PEO-01"), L("Reconstrucción", "REC-01"), L("Evolución", "DIS-08"),
                         L("Fuentes", "DIS-05"), L("Organismos", "DIS-06"), L("Alertas", "DIS-07")],
            },
            M("DIS-04", DIS, "Registro de desastre", "Desastres/Crear", Kind.Form, 'A',
              "POST /api/desastres con Idempotency-Key UUID (201/200/409). N extensión del body y GET /api/desastres/tipos") with
            {
                SaveTo = "DIS-03", CancelTo = "DIS-02",
                Fields =
                [
                    F("nombre", "Nombre", req: true, max: 120),
                    F("tipoId", "Tipo de desastre", FT.Select, req: true, o: tipos),
                    F("inicio", "Fecha y hora de inicio", FT.DateTime, req: true),
                    F("severidad", "Severidad", FT.Select, req: true, hint: "Escala actual 1 a 5", o: sev),
                    F("ubicacion", "Municipio o zona", req: true, max: 200),
                    F("latitud", "Latitud", FT.Number, req: true, min: -90, hi: 90),
                    F("longitud", "Longitud", FT.Number, req: true, min: -180, hi: 180),
                    F("radio", "Radio (m)", FT.Number, min: 1, hi: 1000000, hint: "Mayor que 0 y hasta 1.000.000"),
                    F("descripcion", "Descripción", FT.TextArea, req: true, max: 2000),
                    F("geometria", "Polígono GeoJSON (opcional)", FT.TextArea, hint: "El dibujo en mapa queda pendiente"),
                    F("fuenteId", "Fuente de información", FT.Select, o: "Sensor sísmico|Estación meteorológica|Ciudadano|Organismo"),
                    F("organizacionId", "Organismo que registra", FT.Select, req: true, o: orgs),
                    F("responsableId", "Responsable de la emergencia", req: true),
                ],
            },
            M("DIS-05", DIS, "Fuentes y reportes", "Desastres/{id:int}/Fuentes", Kind.List, 'N', "GET/POST /api/desastres/{id}/fuentes") with
            {
                Cols = [C("receivedAt", "Fecha", CT.Date), C("source", "Origen", s: "Sensor sísmico|Estación meteorológica|Bomberos|Ciudadano"),
                        C("reference", "Referencia", s: "REF-0001|REF-0002|REF-0003"), C("description", "Descripción", s: "Reporte de daños en zona norte|Lectura de magnitud 6.1"),
                        C("status", "Estado", CT.Badge, "Consolidado|Pendiente|Duplicado")],
                Dialogs = [D("asociar", "Asociar reporte", "Asociar reporte", F("source", "Origen", req: true), F("reference", "Referencia", req: true),
                                F("description", "Descripción", FT.TextArea, req: true))],
            },
            M("DIS-06", DIS, "Organismos participantes", "Desastres/{id:int}/Organismos", Kind.List, 'E',
              "E GET/POST /api/desastres/{id}/organizaciones; PUT .../{associationId}") with
            {
                RowTo = "ORG-02",
                Cols = [C("name", "Organización", s: orgs), C("responsibility", "Responsabilidad", s: "Rescate|Logística|Seguridad|Atención médica|Albergue"),
                        C("statusId", "Estado", CT.Badge, "Activo|En espera|Activo")],
                Dialogs = [D("asociar", "Asociar organismo", "Asociar organismo", F("organizationId", "Organismo", FT.Select, req: true, o: orgs),
                                F("responsibility", "Responsabilidad", req: true), F("statusId", "Estado", FT.Select, o: "Activo|En espera"))],
            },
            M("DIS-07", DIS, "Alertas y actividad", "Desastres/{id:int}/Alertas", Kind.List, 'E',
              "N GET /api/desastres/{id}/historial; E GET/POST /api/coordinacion/alertas?disasterId={id}") with
            {
                TableTitle = "Alertas",
                Cols = alertCols,
                Tables = [T("Línea de tiempo", null, C("occurredAt", "Fecha", CT.Date), C("eventType", "Evento", s: "Fase|Severidad|Área"),
                            C("actor", "Actor", s: "j.pardo|m.torres"), C("before", "Antes", s: "Registered|3"), C("after", "Después", s: "Response|4"))],
                Dialogs = [D("nueva", "Nueva alerta", "Nueva alerta", F("subject", "Asunto", req: true), F("message", "Mensaje", FT.TextArea, req: true),
                                F("recipientIds", "Destinatarios", req: true, hint: "Separados por coma"))],
            },
            M("DIS-08", DIS, "Evolución del evento", "Desastres/{id:int}/Evolucion", Kind.Dashboard, 'N',
              "N GET /api/desastres/{id}/evolucion; POST .../area; PUT .../nivel") with
            {
                Intro = "Cómo ha cambiado el área afectada, el nivel de atención y el estado del evento.",
                Stats =
                [
                    S("Nivel de atención actual", "Nivel_3 — Alto"), S("Estado", "En atención"),
                    S("Zonas afectadas", "7 barrios"), S("Versiones del área", "4"),
                ],
                Map = true,
                TableTitle = "Historial del área afectada",
                Cols =
                [
                    C("registradoAt", "Fecha y hora", CT.Date), C("zonas", "Zonas afectadas", s: "3 barrios|5 barrios|7 barrios"),
                    C("fuente", "Fuente", s: "Sistema GIS|Evaluación de campo|Organismo"),
                    C("registradoPor", "Registrado por", s: "j.pardo|m.torres"),
                    C("motivo", "Motivo", s: "Estimación inicial|Nueva evaluación|Réplica"),
                ],
                Tables =
                [
                    T("Línea de tiempo", null, C("occurredAt", "Fecha", CT.Date),
                      C("evento", "Evento", s: "Estado|Nivel de atención|Área afectada"),
                      C("antes", "Antes", s: "Registrado|Nivel_1|3 barrios"),
                      C("despues", "Después", s: "En atención|Nivel_3|7 barrios"),
                      C("actor", "Responsable", s: "j.pardo|m.torres"),
                      C("motivo", "Motivo", s: "Nueva evaluación|Réplica|Ampliación de área")),
                ],
                Dialogs =
                [
                    D("area", "Actualizar área afectada", "Actualizar área afectada",
                      F("descripcion", "Descripción de zonas", req: true),
                      F("cantidad", "Cantidad de zonas", FT.Number, min: 0),
                      F("geometria", "Geometría", FT.TextArea, hint: "Opcional"),
                      F("fuente", "Fuente", FT.Select, o: "Sistema GIS|Evaluación de campo|Organismo"),
                      F("motivo", "Motivo", FT.TextArea, req: true)),
                    D("nivel", "Cambiar nivel de atención", "Cambiar nivel de atención",
                      F("nivel", "Nivel de atención", FT.Select, req: true, o: nivelesAtencion),
                      F("motivo", "Motivo", FT.TextArea, req: true)),
                ],
                Links = [L("Detalle del desastre", "DIS-03"), L("Alertas", "DIS-07")],
            },

            // ===== 4. Evaluación de daños =====
            M("DAM-01", DAM, "Resumen de daños", "Danos", Kind.Dashboard, 'N', "N GET /api/evaluaciones/mapa?disasterId={id}; GET /api/evaluaciones?disasterId={id}") with
            {
                Intro = "Consulta las evaluaciones de daños y el estado de los bienes afectados.",
                Stats = [S("Evaluaciones", "128"), S("Daño grave o total", "23"), S("Por validar", "17")],
                Map = true,
                TableTitle = "Activos por nivel de daño",
                Cols = [C("disaster", "Desastre", s: desastres), C("assetId", "Activo", s: "PUENTE-014|ESC-203|HOSP-007|VIV-1180"),
                        C("assetType", "Tipo", s: "Puente|Escuela|Hospital|Vivienda"), C("damageLevel", "Daño", CT.Badge, "Destroyed|Severe|Moderate|Minor"),
                        C("status", "Estado", CT.Badge, "Validated|Draft|Rejected|Validated")],
                RowTo = "DAM-03",
                Filters = [F("disaster", "Desastre", o: desastres), F("assetType", "Tipo", o: "Puente|Escuela|Hospital|Vivienda"),
                           F("status", "Estado", o: "Validated|Draft|Rejected")],
                Links = [L("Nueva evaluación", "DAM-04", true), L("Listado", "DAM-02"), L("Sincronización", "DAM-05"),
                         L("Validación", "DAM-06"), L("Priorización", "DAM-07")],
            },
            M("DAM-02", DAM, "Listado de evaluaciones", "Danos/Lista", Kind.List, 'N',
              "GET /api/evaluaciones?disasterId={id}. Respuesta {schemaVersion:1, disasterId, evaluations[]}") with
            {
                Cols = evalCols, RowTo = "DAM-03",
                Filters = [F("assetType", "Tipo", o: "Puente|Escuela|Hospital|Vivienda|Carretera"), F("damageLevel", "Daño", o: danio),
                           F("status", "Estado", o: "Validated|Draft|Rejected")],
                Links = [L("Nueva evaluación", "DAM-04", true)],
            },
            M("DAM-03", DAM, "Detalle de evaluación", "Danos/{id:guid}", Kind.Detail, 'N',
              "GET /api/evaluaciones/{id} y /revisiones; PUT /api/evaluaciones/{id}; POST /api/evaluaciones/{id}/validaciones") with
            {
                Map = true,
                Cols =
                [
                    C("assetId", "Activo", s: "PUENTE-014"), C("assetType", "Tipo", s: "Puente"), C("location", "Zona", s: "Calarcá, Quindío"),
                    C("evaluatedAt", "Fecha", CT.Date), C("evaluator", "Evaluador", s: "L. Ramírez"), C("organization", "Organización", s: "Defensa Civil"),
                    C("damageLevel", "Nivel de daño", CT.Badge, "Severe"), C("status", "Estado", CT.Badge, "Draft"), C("revision", "Revisión", CT.Number),
                    C("budget", "Presupuesto", CT.Money), C("description", "Descripción", s: "Fisuras en la pila central y asentamiento del estribo norte."),
                ],
                Tables =
                [
                    T("Evidencias", null, C("evidence", "Archivo", s: "foto-001.jpg|foto-002.jpg|informe.pdf"), C("evaluatedAt", "Cargado", CT.Date)),
                    T("Revisiones", null, C("revision", "Rev.", CT.Number), C("evaluatedAt", "Fecha", CT.Date), C("evaluator", "Evaluador", s: "L. Ramírez|M. Torres"),
                      C("damageLevel", "Daño", CT.Badge, "Severe|Moderate"), C("status", "Estado", CT.Badge, "Draft|Validated")),
                ],
                Dialogs =
                [
                    D("rev", "Editar (nueva revisión)", "Nueva revisión", F("damageLevel", "Nivel de daño", FT.Select, req: true, o: danio),
                      F("description", "Descripción", FT.TextArea, req: true), F("budget", "Presupuesto", FT.Number, min: 0)),
                    D("aprobar", "Aprobar", "Aprobar evaluación", F("reason", "Motivo", FT.TextArea, req: true, max: 1000)),
                    D("rechazar", "Rechazar", "Rechazar evaluación", F("reason", "Motivo", FT.TextArea, req: true, max: 1000)),
                ],
                Links = [L("Listado", "DAM-02"), L("Reconstrucción (solo validadas)", "REC-02")],
            },
            M("DAM-04", DAM, "Nueva evaluación", "Danos/Crear", Kind.Form, 'N',
              "POST /api/evaluaciones; POST .../{id}/evidencias (multipart); GET /api/evaluaciones/catalogos. Idempotency-Key UUID") with
            {
                SaveTo = "DAM-03", CancelTo = "DAM-02",
                Fields =
                [
                    F("disasterId", "Desastre", FT.Select, hint: "Vacío = evaluación preliminar", o: desastres),
                    F("assetId", "Activo (identificador)", req: true), F("assetTypeId", "Tipo de activo", FT.Select, req: true, o: activos),
                    F("damageLevel", "Nivel de daño", FT.Select, req: true, o: danio),
                    F("evaluatedAt", "Fecha y hora", FT.DateTime, req: true, noFuture: true),
                    F("evaluator", "Evaluador", req: true), F("organization", "Organización", req: true),
                    F("location", "Ubicación (zona)", req: true),
                    F("latitude", "Latitud", FT.Number, min: -90, hi: 90), F("longitude", "Longitud", FT.Number, min: -180, hi: 180),
                    F("budget", "Costo estimado", FT.Number, min: 0),
                    F("description", "Descripción", FT.TextArea, req: true),
                    F("evidence", "Evidencias (fotos o documentos)", FT.File),
                ],
            },
            M("DAM-05", DAM, "Evaluaciones recibidas del trabajo en campo", "Danos/Sincronizacion", Kind.List, 'N', "GET /api/evaluaciones/sincronizacion?disasterId={id}") with
            {
                Intro = "Consulta las evaluaciones recibidas y revisa los conflictos que requieren atención.",
                RowTo = "DAM-03",
                Cols = [C("clientRequestId", "Operación", s: "a1f3…9c|b72e…01|c9d4…7a"), C("evaluationId", "Evaluación", s: "3f2b…11|8a90…42"),
                        C("receivedAt", "Recibida", CT.Date), C("status", "Estado", CT.Badge, "Applied|Conflict|Error|Applied"),
                        C("error", "Error", s: "|Revisión desactualizada|Activo sin identificador|")],
                Filters = [F("status", "Estado", o: "Applied|Conflict|Error")],
                Dialogs = [D("resolver", "Resolver conflicto", "Resolver conflicto", F("reason", "Motivo de la resolución", FT.TextArea, req: true))],
            },
            M("DAM-06", DAM, "Validación de evaluaciones", "Danos/Validacion", Kind.List, 'N',
              "N GET /api/evaluaciones?estado=Draft; PUT /api/evaluaciones/{id}/validacion") with
            {
                Intro = "Revisa evaluaciones pendientes, valida, rechaza o fusiona duplicados.",
                Cols =
                [
                    evalCols.Single(c => c.Key == "assetId"), evalCols.Single(c => c.Key == "assetType"),
                    evalCols.Single(c => c.Key == "location"), evalCols.Single(c => c.Key == "evaluator"),
                    evalCols.Single(c => c.Key == "damageLevel"),
                    C("duplicate", "Posible duplicado", CT.Badge, "Sí|No|No"),
                    C("status", "Estado", CT.Badge, "Pendiente de validación|Validada|Rechazada"),
                    evalCols.Single(c => c.Key == "evaluatedAt"),
                ],
                RowTo = "DAM-03",
                Filters =
                [
                    F("status", "Estado", o: "Pendiente de validación|Validada|Rechazada"),
                    F("duplicate", "Posible duplicado", o: "Sí|No"),
                ],
                Dialogs =
                [
                    D("validar", "Validar evaluación", "Validar evaluación",
                      F("assetId", "Activo", req: true), F("comentario", "Comentario", FT.TextArea)),
                    D("rechazar", "Rechazar evaluación", "Rechazar evaluación",
                      F("assetId", "Activo", req: true), F("motivo", "Motivo", FT.TextArea, req: true)),
                    D("duplicado", "Marcar como duplicado", "Marcar como duplicado",
                      F("evaluacionDuplicada", "Evaluación duplicada", req: true),
                      F("evaluacionOriginal", "Evaluación original", req: true),
                      F("motivo", "Motivo", FT.TextArea, req: true)),
                ],
            },
            M("DAM-07", DAM, "Priorización e impacto", "Danos/Priorizacion", Kind.Dashboard, 'N',
              "N GET /api/evaluaciones/priorizacion?disasterId={id}") with
            {
                Intro = "Zonas y activos que requieren atención primero, con el costo estimado de recuperación.",
                Stats =
                [
                    S("Activos destruidos", "42"), S("Daño grave", "118"),
                    S("Costo estimado", "$ 18.400 millones"), S("Zonas críticas", "5"),
                ],
                Map = true, Chart = true, ChartTitle = "Costo estimado por zona (millones COP)",
                TableTitle = "Zonas priorizadas",
                Cols =
                [
                    C("zona", "Zona", s: "Armenia|Calarcá|Cali|Manizales"),
                    C("activos", "Activos afectados", CT.Number), C("graves", "Graves o destruidos", CT.Number),
                    C("personas", "Personas afectadas", CT.Number), C("costo", "Costo estimado", CT.Money),
                    C("prioridad", "Prioridad", CT.Badge, prios),
                ],
                Links = [L("Crear propuesta de reconstrucción", "REC-02", true), L("Listado de evaluaciones", "DAM-02")],
            },

            // ===== 5. Personas afectadas =====
            M("PEO-01", PEO, "Resumen de personas afectadas", "Personas", Kind.Dashboard, 'E', "E GET /api/personas-afectadas/resumen?disasterId={id}") with
            {
                Intro = "Consulta las cifras de personas y familias afectadas y las necesidades de atención.",
                Stats = [S("Personas", "1.240"), S("Familias", "312"), S("Expedientes abiertos", "87"), S("Alojados", "540")],
                Chart = true, ChartTitle = "Personas por zona",
                Links = [L("Registrar persona", "PEO-03", true), L("Directorio", "PEO-02"), L("Detalle de ejemplo", "PEO-04"),
                         L("Familias", "PEO-05"), L("Refugios", "PEO-07"), L("Solicitudes de ayuda", "PEO-09")],
            },
            M("PEO-02", PEO, "Directorio de personas", "Personas/Lista", Kind.List, 'E', "E GET /api/personas-afectadas?disasterId={id}&search=...") with
            {
                RowTo = "PEO-04",
                Cols = [C("identificationRef", "Referencia", s: "CC-1010|CC-2044|TI-9931|CC-3377"),
                        C("nombre", "Nombre", s: "María Gómez|Juan Pérez|Ana Rojas|Carlos Díaz|Luisa Torres"),
                        C("locationId", "Ubicación", s: "Armenia|Calarcá|Cali"), C("organizationId", "Organización", s: "Cruz Roja|Defensa Civil"),
                        C("caseStatusId", "Caso", CT.Badge, "Abierto|En atención|Cerrado")],
                Filters = [F("caseStatusId", "Estado de caso", o: "Abierto|En atención|Cerrado")],
                Links = [L("Registrar persona", "PEO-03", true)],
            },
            M("PEO-03", PEO, "Registro de personas", "Personas/Crear", Kind.Form, 'E', "E POST /api/personas-afectadas") with
            {
                Intro = "Información protegida. El acceso se controla por rol y organización.",
                SaveTo = "PEO-04", CancelTo = "PEO-02",
                Fields =
                [
                    F("firstName", "Nombres", req: true), F("lastName", "Apellidos"), F("identificationRef", "Identificación"),
                    F("birthDate", "Fecha de nacimiento", FT.Date, noFuture: true), F("phone", "Teléfono", FT.Phone), F("email", "Correo", FT.Email),
                    F("locationId", "Ubicación"), F("organizationId", "Organización", FT.Select, o: orgs), F("disasterId", "Desastre", FT.Select, o: desastres),
                ],
            },
            M("PEO-04", PEO, "Detalle de persona", "Personas/{id}", Kind.Detail, 'E', "E GET/PUT /api/personas-afectadas/{id}") with
            {
                Cols = [C("firstName", "Nombres", s: "María"), C("lastName", "Apellidos", s: "Gómez"), C("identificationRef", "Identificación", s: "CC-1010"),
                        C("birthDate", "Nacimiento", s: "1988-04-12"), C("phone", "Teléfono", s: "300 123 4567"), C("email", "Correo", s: "maria@correo.co"),
                        C("locationId", "Ubicación", s: "Armenia"), C("organizationId", "Organización", s: "Cruz Roja"),
                        C("familyId", "Familia", s: "Familia Gómez"), C("shelterId", "Refugio", s: "Coliseo Armenia")],
                Tables = [T("Expedientes", null, C("caseId", "Caso", s: "DVR-001|DVR-014"), C("status", "Estado", CT.Badge, "Abierto|Cerrado"), C("prioridad", "Prioridad", s: "Alta|Media")),
                          T("Necesidades y atención", null, C("tipo", "Necesidad", s: "Alimentos|Medicamentos|Alojamiento"), C("estado", "Atención", CT.Badge, "Pendiente|Completada|En curso"), C("fecha", "Fecha", CT.Date))],
                Dialogs = [D("editar", "Editar", "Editar persona", F("firstName", "Nombres", req: true), F("lastName", "Apellidos"), F("phone", "Teléfono", FT.Phone), F("email", "Correo", FT.Email))],
                Links = [L("Familia", "PEO-06")],
            },
            M("PEO-05", PEO, "Gestión de familias", "Personas/Familias", Kind.List, 'E', "E GET/POST /api/familias; POST /api/familias/{id}/integrantes") with
            {
                Optional = true, RowTo = "PEO-06",
                Cols = [C("name", "Familia", s: "Familia Gómez|Familia Rojas|Familia Díaz"), C("groupTypeId", "Tipo", s: "Familiar|Extendida"),
                        C("integrantes", "Integrantes", CT.Number), C("jefe", "Jefe de grupo", s: "María Gómez|Ana Rojas|Carlos Díaz")],
                Dialogs = [D("crear", "Crear familia", "Crear familia", F("name", "Nombre", req: true), F("groupTypeId", "Tipo", FT.Select, o: "Familiar|Extendida"))],
            },
            M("PEO-06", PEO, "Detalle de familia", "Personas/Familias/{id}", Kind.Detail, 'E', "E GET /api/familias/{id}; POST /api/familias/{id}/integrantes") with
            {
                Optional = true,
                Cols = [C("name", "Familia", s: "Familia Gómez"), C("locationId", "Ubicación", s: "Armenia"), C("caseIds", "Casos", s: "DVR-001, DVR-014")],
                Tables = [T("Miembros", "PEO-04", C("personId", "Persona", s: "María Gómez|Luis Gómez|Sara Gómez"), C("groupHead", "Jefe de grupo", CT.Badge, "Sí|No|No"))],
                Dialogs = [D("asoc", "Editar asociación", "Editar asociación", F("personId", "Persona", req: true), F("groupHead", "Jefe de grupo", FT.Bool))],
            },
            M("PEO-07", PEO, "Refugios", "Personas/Refugios", Kind.List, 'E', "E GET/POST /api/refugios?disasterId={id}") with
            {
                Intro = "Capacidad y ocupación de los refugios habilitados.",
                Cols =
                [
                    C("nombre", "Refugio", s: "Coliseo Armenia|Colegio Normal Calarcá|Polideportivo Cali Sur|Coliseo Manizales"),
                    C("ubicacion", "Ubicación", s: "Armenia|Calarcá|Cali|Manizales"), C("capacidad", "Capacidad", CT.Number),
                    C("ocupacion", "Ocupación", CT.Number), C("responsable", "Responsable", s: "Cruz Roja|Defensa Civil|Autoridad local"),
                    C("estado", "Estado", CT.Badge, "Abierto|Lleno|Cerrado"),
                ],
                RowTo = "PEO-08",
                Filters = [F("estado", "Estado", o: "Abierto|Lleno|Cerrado")],
                Dialogs =
                [
                    D("crear", "Registrar refugio", "Registrar refugio", F("nombre", "Nombre", req: true),
                      F("ubicacion", "Ubicación", req: true),
                      F("capacidad", "Capacidad", FT.Number, req: true, min: 1),
                      F("responsable", "Responsable", FT.Select, req: true, o: orgs),
                      F("latitud", "Latitud", FT.Number, min: -90, hi: 90),
                      F("longitud", "Longitud", FT.Number, min: -180, hi: 180)),
                ],
                Links = [L("Registrar solicitud de ayuda", "PEO-09", true), L("Directorio", "PEO-02")],
            },
            M("PEO-08", PEO, "Detalle de refugio", "Personas/Refugios/{id:guid}", Kind.Detail, 'E',
              "E GET /api/refugios/{id}; PUT /api/refugios/{id}/ocupacion") with
            {
                Cols =
                [
                    C("nombre", "Refugio", s: "Coliseo Armenia"), C("ubicacion", "Ubicación", s: "Armenia"),
                    C("capacidad", "Capacidad", CT.Number), C("ocupacion", "Ocupación", CT.Number),
                    C("responsable", "Responsable", s: "Cruz Roja"), C("estado", "Estado", CT.Badge, "Abierto"),
                    C("contacto", "Contacto", s: "300 555 0142"),
                ],
                Tables =
                [
                    T("Personas alojadas", "PEO-04", C("persona", "Persona", s: "María Gómez|Juan Pérez|Ana Rojas"),
                      C("familia", "Familia", s: "Familia Gómez|Familia Pérez"), C("fechaIngreso", "Ingreso", CT.Date)),
                    T("Recursos asignados", null, C("recurso", "Recurso", s: "Kit de alimentos|Agua potable|Colchonetas"),
                      C("cantidad", "Cantidad", CT.Number), C("fecha", "Fecha", CT.Date),
                      C("estado", "Estado", CT.Badge, "Asignado|Entregado|Pendiente")),
                ],
                Dialogs =
                [
                    D("ocupacion", "Actualizar ocupación", "Actualizar ocupación",
                      F("ocupacion", "Ocupación", FT.Number, req: true, min: 0),
                      F("observaciones", "Observaciones", FT.TextArea)),
                ],
            },
            M("PEO-09", PEO, "Solicitudes de ayuda y atención", "Personas/Solicitudes", Kind.List, 'E',
              "E GET/POST /api/personas-afectadas/solicitudes-ayuda; POST .../atenciones") with
            {
                Intro = "Información protegida. El acceso se controla por rol y organización.",
                Cols =
                [
                    C("codigo", "Solicitud", s: "AYU-0201|AYU-0202|AYU-0203"),
                    C("solicitante", "Persona o familia", s: "María Gómez|Familia Rojas|Carlos Díaz"),
                    C("necesidad", "Necesidad", s: "Alimentos|Agua|Medicamentos|Alojamiento|Transporte|Atención psicosocial"),
                    C("ubicacion", "Ubicación", s: "Armenia|Calarcá|Cali|Manizales"),
                    C("prioridad", "Prioridad", CT.Badge, prios), C("organismo", "Organismo asignado", s: orgs),
                    C("estado", "Estado", CT.Badge, "Pendiente|En atención|Atendida"), C("fecha", "Fecha", CT.Date),
                ],
                Filters =
                [
                    F("necesidad", "Necesidad", o: "Alimentos|Agua|Medicamentos|Alojamiento|Transporte|Atención psicosocial"),
                    F("prioridad", "Prioridad", o: prios), F("estado", "Estado", o: "Pendiente|En atención|Atendida"),
                ],
                Dialogs =
                [
                    D("nueva", "Registrar solicitud de ayuda", "Registrar solicitud de ayuda",
                      F("solicitante", "Persona o familia", req: true),
                      F("necesidad", "Necesidad", FT.Select, req: true, o: "Alimentos|Agua|Medicamentos|Alojamiento|Transporte|Atención psicosocial"),
                      F("prioridad", "Prioridad", FT.Select, req: true, o: prios),
                      F("ubicacion", "Ubicación"),
                      F("descripcion", "Descripción", FT.TextArea)),
                    D("atencion", "Registrar atención", "Registrar atención",
                      F("codigo", "Código de solicitud", req: true),
                      F("tipo", "Tipo de atención", FT.Select, req: true, o: "Entrega de ayuda|Alojamiento|Atención médica|Acompañamiento"),
                      F("organismo", "Organismo", FT.Select, req: true, o: orgs),
                      F("fecha", "Fecha y hora", FT.DateTime),
                      F("observaciones", "Observaciones", FT.TextArea)),
                ],
            },

            // ===== 6. Coordinación de organizaciones =====
            M("ORG-01", ORG, "Directorio de organizaciones", "Organizaciones", Kind.List, 'E', "E GET /api/organizaciones; GET /api/organizaciones/tipos") with
            {
                Intro = "Consulta las organizaciones registradas y coordina sus tareas y comunicaciones.",
                RowTo = "ORG-02",
                Cols = [C("name", "Nombre", s: "Cruz Roja Colombiana|Defensa Civil|Bomberos Bogotá|Policía Nacional|Secretaría de Salud"),
                        C("acronym", "Sigla", s: "CRC|DCC|BB|PN|SDS"), C("typeId", "Tipo", s: "ONG|Gubernamental|Seguridad|Seguridad|Salud"),
                        C("locationId", "Zona", s: "Nacional|Bogotá|Quindío"), C("email", "Contacto", s: "contacto@org.co")],
                Filters = [F("typeId", "Tipo", o: "ONG|Gubernamental|Seguridad|Salud")],
                Links = [L("Registrar organización", "ORG-03", true), L("Tareas", "ORG-04"), L("Actividades", "ORG-05"), L("Alertas", "ORG-06")],
            },
            M("ORG-02", ORG, "Detalle de organización", "Organizaciones/{id}", Kind.Detail, 'E', "E GET/PUT /api/organizaciones/{id}") with
            {
                Cols = [C("name", "Nombre", s: "Cruz Roja Colombiana"), C("acronym", "Sigla", s: "CRC"), C("typeId", "Tipo", s: "ONG"),
                        C("country", "País", s: "Colombia"), C("locationId", "Ubicación", s: "Bogotá"), C("phone", "Teléfono", s: "601 555 0100"),
                        C("email", "Correo", s: "contacto@cruzroja.org.co"), C("description", "Descripción", s: "Atención humanitaria y primeros auxilios.")],
                Tables = [T("Personal", null, C("name", "Nombre", s: "A. Vargas|R. Castro"), C("rol", "Rol", s: "Coordinador|Paramédico")),
                          T("Instalaciones", null, C("name", "Instalación", s: "Sede Armenia|Bodega central"), C("ubicacion", "Ubicación", s: "Armenia|Bogotá")),
                          T("Desastres asociados", "DIS-03", C("nombre", "Desastre", s: desastres), C("phase", "Fase", CT.Badge, "Response|Registered"))],
                Dialogs = [D("editar", "Editar", "Editar organización", F("name", "Nombre", req: true), F("acronym", "Sigla"), F("phone", "Teléfono", FT.Phone), F("email", "Correo", FT.Email))],
                Links = [L("Tareas", "ORG-04"), L("Actividades", "ORG-05")],
            },
            M("ORG-03", ORG, "Registro de organizaciones", "Organizaciones/Crear", Kind.Form, 'E', "E POST /api/organizaciones; GET /api/organizaciones/tipos") with
            {
                SaveTo = "ORG-02", CancelTo = "ORG-01",
                Fields = [F("name", "Nombre", req: true), F("acronym", "Sigla"), F("typeId", "Tipo", FT.Select, req: true, o: "ONG|Gubernamental|Seguridad|Salud|Internacional"),
                          F("country", "País"), F("locationId", "Zona"), F("phone", "Teléfono", FT.Phone), F("email", "Correo", FT.Email),
                          F("description", "Descripción", FT.TextArea)],
            },
            M("ORG-04", ORG, "Coordinación de tareas", "Organizaciones/Tareas", Kind.List, 'E', "E GET/POST /api/coordinacion/tareas; PUT /api/coordinacion/tareas/{id}") with
            {
                Cols = taskCols,
                Filters = [F("priorityId", "Prioridad", o: prios), F("statusId", "Estado", o: estTarea)],
                Dialogs = [D("nueva", "Nueva tarea", "Nueva tarea", F("name", "Nombre", req: true), F("description", "Descripción", FT.TextArea),
                                F("priorityId", "Prioridad", FT.Select, req: true, o: prios), F("assignee", "Responsable"), F("dueAt", "Vencimiento", FT.DateTime),
                                F("disasterId", "Desastre", FT.Select, o: desastres))],
            },
            M("ORG-05", ORG, "Seguimiento de actividades", "Organizaciones/Actividades", Kind.List, 'E', "E GET/POST /api/coordinacion/actividades; PUT .../{id}") with
            {
                Cols = [C("name", "Actividad", s: "Reparto de alimentos|Censo en refugio|Ruta de evacuación"), C("statusId", "Estado", CT.Badge, estTarea),
                        C("contactPersonId", "Contacto", s: "A. Vargas|R. Castro"), C("startsAt", "Inicio", CT.Date), C("endsAt", "Fin", CT.Date),
                        C("comments", "Observaciones", s: "Avance del 40 %|Sin novedades")],
                Filters = [F("statusId", "Estado", o: estTarea)],
                Dialogs = [D("nueva", "Nueva actividad", "Nueva actividad", activityDlg)],
            },
            M("ORG-06", ORG, "Historial de alertas y comunicaciones", "Organizaciones/Alertas", Kind.List, 'E', "E GET/POST /api/coordinacion/alertas") with
            {
                Cols = alertCols,
                Filters = [F("channel", "Canal", o: canales), F("status", "Estado", o: "Entregada|Pendiente|Fallida")],
                Dialogs = [D("nueva", "Nueva alerta", "Nueva alerta", F("subject", "Asunto", req: true), F("message", "Mensaje", FT.TextArea, req: true),
                                F("channel", "Canal", FT.Select, o: canales), F("recipientIds", "Destinatarios", req: true))],
                Links = [L("Directorio", "ORG-01")],
            },

            // ===== 7. Reconstrucción =====
            M("REC-01", REC, "Resumen de reconstrucción", "Reconstruccion", Kind.Dashboard, 'A',
              "GET /api/reconstruccion/propuestas y /proyectos?disasterId={id}. N mapa DAM-01") with
            {
                Intro = "Consulta las propuestas, los proyectos en curso y los recursos destinados a la recuperación.",
                Stats = [S("Propuestas", "24"), S("En formalización", "4"), S("En monitoreo", "9"), S("Cerrados", "3"),
                         S("Presupuesto disponible de donaciones", "$ 12.400 M")],
                Map = true, TableTitle = "Propuestas", RowTo = "REC-03",
                Cols = [C("disaster", "Desastre", s: desastres), C("zone", "Zona", s: "Armenia|Calarcá|Cali|Manizales"),
                        C("assetType", "Activo", s: "Puente|Escuela|Hospital|Vivienda"), C("damage", "Daño", CT.Badge, "Destroyed|Severe|Severe|Moderate"),
                        C("proposedBudget", "Presupuesto", CT.Money), C("status", "Estado", CT.Badge, "Proposed|Selected|Proposed|Obsolete")],
                Filters = [F("disaster", "Desastre", o: desastres)],
                Links = [L("Propuestas", "REC-02"), L("Proyectos", "REC-04"), L("Proyecto de ejemplo", "REC-05")],
            },
            M("REC-02", REC, "Propuestas de reconstrucción", "Reconstruccion/Propuestas", Kind.List, 'A',
              "GET /api/reconstruccion/propuestas?disasterId={id}; POST .../propuestas/generar {disasterId}") with
            {
                Intro = "Zona, activo y nivel de daño con su prioridad según el presupuesto disponible de donaciones. Selecciona las propuestas que se convertirán en proyectos.",
                Stage = 1,
                Selectable = true,
                RowTo = "REC-03",
                Cols = [C("zone", "Zona", s: "Armenia|Calarcá|Cali|Manizales"), C("assetType", "Activo", s: "Puente|Escuela|Hospital|Vivienda"),
                        C("damage", "Daño", CT.Badge, "Destroyed|Severe|Severe|Moderate"), C("priority", "Prioridad", CT.Badge, "High|Low|Unassigned|High"),
                        C("resultado", "Resultado"),
                        C("proposedBudget", "Presupuesto", CT.Money), C("status", "Estado", CT.Badge, "Proposed|Selected|Proposed|Obsolete")],
                Filters = [F("zone", "Zona", FT.Select, o: "Armenia|Calarcá|Cali|Manizales"),
                           F("assetType", "Activo", FT.Select, o: "Puente|Escuela|Hospital|Vivienda"),
                           F("damage", "Daño", FT.Select, o: danio),
                           F("status", "Estado", FT.Select, o: "Proposed|Selected|Obsolete"),
                           F("priority", "Prioridad", FT.Select, o: "High|Low|Unassigned")],
                Dialogs =
                [
                    D("generar", "Generar propuestas", "Generar propuestas",
                        F("disasterId", "Desastre", FT.Select, req: true, hint: "Solo desastres en estado Reconstrucción", o: desastres)),
                    D("proyectos", "Crear proyectos", "Crear proyectos"),
                ],
            },
            M("REC-03", REC, "Detalle de propuesta", "Reconstruccion/Propuestas/{id:guid}", Kind.Detail, 'A',
              "Localizar el id en GET /api/reconstruccion/propuestas (no hay GET individual); POST /api/reconstruccion/proyectos") with
            {
                Stage = 1,
                Cols = [C("zone", "Zona", s: "Calarcá"), C("assetType", "Activo", s: "Puente"), C("damage", "Daño", CT.Badge, "Destroyed"),
                        C("assessmentCount", "Evaluaciones", CT.Number), C("proposedBudget", "Presupuesto", CT.Money),
                        C("availableBudget", "Presupuesto disponible (donaciones)", CT.Money, "$ 8.400 M"),
                        C("coversBudget", "Cubre el presupuesto propuesto", CT.Badge, "Sí"),
                        C("averageSeverity", "Severidad media", s: "4,2"), C("priority", "Prioridad", CT.Badge, "High"),
                        C("resultado", "Resultado", s: "Se prioriza proyecto"),
                        C("status", "Estado", CT.Badge, "Proposed"), C("version", "Versión", CT.Number)],
                Tables = [T("Evaluaciones origen", "DAM-03", C("assetId", "Activo", s: "PUENTE-014|PUENTE-015"), C("damageLevel", "Daño", CT.Badge, "Destroyed|Severe"),
                            C("evaluator", "Evaluador", s: "L. Ramírez|M. Torres"), C("budget", "Presupuesto", CT.Money), C("revision", "Rev.", CT.Number))],
                Dialogs = [D("proyecto", "Crear proyecto", "Crear proyecto desde la propuesta")],
                Links = [L("Ver proyecto", "REC-05"), L("Propuestas", "REC-02")],
            },
            M("REC-04", REC, "Listado de proyectos", "Reconstruccion/Proyectos", Kind.List, 'A', "GET /api/reconstruccion/proyectos?disasterId={id}") with
            {
                RowTo = "REC-05",
                Cols = [C("name", "Proyecto", s: "Puente Calarcá|Escuela Armenia|Hospital Cali"), C("disasterId", "Desastre", s: desastres),
                        C("responsible", "Responsable", s: "Ing. Rojas|Ing. Díaz"), C("contractor", "Contratista", s: "Constructora Andina|Obras del Valle"),
                        C("finalBudget", "Presupuesto", CT.Money), C("status", "Estado", CT.Badge, estProy), C("createdAt", "Creado", CT.Date)],
                Filters = [F("status", "Estado", o: estProy)],
            },
            M("REC-05", REC, "Detalle de proyecto", "Reconstruccion/Proyectos/{id:guid}", Kind.Detail, 'A',
              "GET P; P/monitoreo; P/auditoria; P/linea-tiempo; GET /api/reconstruccion/workflow?projectId={id}") with
            {
                Cols = [C("finalBudget", "Presupuesto final", CT.Money), C("fundingSource", "Financiación", s: "Presupuesto nacional"),
                        C("responsible", "Responsable", s: "Ing. Rojas"), C("contractor", "Contratista", s: "Constructora Andina"),
                        C("status", "Estado", CT.Badge, "Monitoring"), C("version", "Versión", CT.Number), C("createdAt", "Creado", CT.Date),
                        C("isBudgetLocked", "Presupuesto bloqueado", s: "Sí"), C("signedContractReference", "Contrato", s: "CT-2026-0418"),
                        C("progressPercentage", "Avance", s: "45 %"), C("totalCost", "Costo acumulado", CT.Money),
                        C("remainingBudget", "Saldo", CT.Money), C("canFinalize", "Puede cerrarse", CT.Badge, "No")],
                Tables = [T("Actividades contractuales", null, projActCols),
                          T("Auditoría", null, C("occurredAt", "Fecha", CT.Date), C("actor", "Actor", s: "j.pardo|m.torres"),
                            C("accion", "Acción", s: "Cambio de presupuesto|Registro de contrato|Reporte de avance"))],
                Links = [L("Formalización", "REC-06"), L("Contrato", "REC-07"), L("Cronograma", "REC-08"), L("Tareas", "REC-09"),
                         L("Participantes", "REC-10"), L("Finanzas", "REC-11"), L("Reportes", "REC-12"), L("Cierre", "REC-13"),
                         L("Historial de cambios", "AUD-01")],
            },
            M("REC-06", REC, "Formalización del proyecto", "Reconstruccion/Proyectos/{id:guid}/Formalizacion", Kind.Form, 'A',
              "GET /api/reconstruccion/financiacion?disasterId={id}; PUT P/formalizacion; POST P/confirmar-formalizacion") with
            {
                Stage = 2,
                SaveTo = "REC-05", CancelTo = "REC-05",
                Fields =
                [
                    F("finalBudget", "Presupuesto final (COP)", FT.Number, req: true, min: 1),
                    F("fundingSource", "Fuente de financiación", FT.Select, req: true, o: "Presupuesto nacional|Donaciones|Cooperación internacional"),
                    F("responsible", "Responsable", req: true, max: 200), F("contractor", "Contratista", req: true, max: 200),
                    F("activities", "Actividades", FT.TextArea, req: true, hint: "Una por línea: nombre; responsable; inicio; vencimiento; presupuesto. La suma no puede superar el presupuesto final"),
                ],
                Links = [L("Registrar contrato", "REC-07")],
            },
            M("REC-07", REC, "Contrato firmado", "Reconstruccion/Proyectos/{id:guid}/Contrato", Kind.Form, 'A', "POST P/contrato-firmado; GET P") with
            {
                Stage = 2,
                Intro = "Registra la referencia de un contrato ya firmado. No hay firma electrónica.",
                SaveTo = "REC-05", CancelTo = "REC-05",
                Fields = [F("contractReference", "Referencia del contrato", req: true, max: 1000),
                          F("signedAt", "Fecha de firma", FT.DateTime, req: true, noFuture: true, hint: "No puede ser futura")],
            },
            M("REC-08", REC, "Cronograma y actividades", "Reconstruccion/Proyectos/{id:guid}/Cronograma", Kind.List, 'A',
              "A GET P/monitoreo; E GET/POST P/actividades-operativas; PUT .../{activityId}") with
            {
                Stage = 3,
                TableTitle = "Actividades del cronograma",
                Cols = projActCols,
                Tables = [T("Actividades operativas", null, C("name", "Actividad", s: "Montaje de obra|Señalización"), C("statusId", "Estado", CT.Badge, estTarea),
                            C("startsAt", "Inicio", CT.Date), C("endsAt", "Fin", CT.Date), C("contactPersonId", "Contacto", s: "A. Vargas"))],
                Dialogs = [D("nueva", "Nueva actividad operativa", "Nueva actividad operativa", activityDlg)],
                Links = [L("Tareas", "REC-09"), L("Reportes", "REC-12"), L("Proyecto", "REC-05")],
            },
            M("REC-09", REC, "Gestión de tareas", "Reconstruccion/Proyectos/{id:guid}/Tareas", Kind.List, 'E', "E GET/POST P/tareas; PUT P/tareas/{taskId}") with
            {
                Stage = 3,
                Cols = taskCols,
                Filters = [F("priorityId", "Prioridad", o: prios), F("statusId", "Estado", o: estTarea)],
                Dialogs = [D("nueva", "Nueva tarea", "Nueva tarea", F("name", "Nombre", req: true), F("description", "Descripción", FT.TextArea),
                                F("priorityId", "Prioridad", FT.Select, req: true, o: prios), F("assignee", "Responsable"), F("dueAt", "Vencimiento", FT.DateTime),
                                F("timeEstimated", "Horas estimadas", FT.Number, min: 0))],
                Links = [L("Cronograma", "REC-08"), L("Proyecto", "REC-05")],
            },
            M("REC-10", REC, "Participantes", "Reconstruccion/Proyectos/{id:guid}/Participantes", Kind.List, 'E', "E GET/POST P/participantes; PUT P/participantes/{participantId}") with
            {
                Stage = 3,
                RowTo = "ORG-02",
                Cols = [C("name", "Nombre", s: "Constructora Andina|Cruz Roja|Ing. Rojas|Interventoría Sur"), C("kind", "Tipo", CT.Badge, "Organización|Organización|Persona|Organización"),
                        C("roleId", "Rol", s: "Contratista|Apoyo social|Líder técnico|Interventor"), C("statusId", "Estado", CT.Badge, "Activo|En espera")],
                Dialogs = [D("asociar", "Asociar participante", "Asociar participante", F("kind", "Tipo", FT.Select, req: true, o: "Organización|Persona"),
                                F("name", "Nombre", req: true), F("roleId", "Rol", req: true), F("statusId", "Estado", FT.Select, o: "Activo|En espera"))],
                Links = [L("Proyecto", "REC-05")],
            },
            M("REC-11", REC, "Monitoreo financiero", "Reconstruccion/Proyectos/{id:guid}/Finanzas", Kind.Dashboard, 'A', "GET P/monitoreo; PUT P/presupuesto") with
            {
                Stage = 3,
                Stats = [S("Presupuesto", "$ 4.200 M"), S("Gasto", "$ 1.890 M"), S("Saldo", "$ 2.310 M")],
                Chart = true, ChartTitle = "Gasto por actividad (millones COP)",
                TableTitle = "Por actividad",
                Cols = [C("name", "Actividad", s: "Demolición|Cimentación|Estructura|Acabados|Entrega"), C("budget", "Presupuesto", CT.Money),
                        C("actualCost", "Costo real", CT.Money), C("remainingBudget", "Saldo", CT.Money)],
                Dialogs = [D("revisar", "Revisar presupuesto", "Revisar presupuesto", F("budget", "Nuevo presupuesto", FT.Number, req: true, min: 1),
                                F("reason", "Motivo", FT.TextArea, req: true, max: 1000))],
                Links = [L("Reportes", "REC-12"), L("Proyecto", "REC-05"), L("Historial de cambios", "AUD-01")],
            },
            M("REC-12", REC, "Reportes y evidencias", "Reconstruccion/Proyectos/{id:guid}/Reportes", Kind.Form, 'A',
              "POST P/adjuntos (multipart file/category); GET P/adjuntos/{attachmentId}; POST P/actividades/{activityId}/reportes") with
            {
                Stage = 3,
                SaveTo = "REC-12", CancelTo = "REC-05",
                Fields = [F("activityId", "Actividad contractual", FT.Select, req: true, o: "Demolición|Cimentación|Estructura|Acabados|Entrega"),
                          F("status", "Estado reportado", FT.Select, req: true, o: "Pending|InProgress|Completed"),
                          F("actualCost", "Costo incremental", FT.Number, req: true, min: 0),
                          F("evidence", "Fotografías (PNG o JPEG, máx. 20 MiB)", FT.File), F("documents", "Documento de soporte (PDF, máx. 20 MiB)", FT.File)],
                Tables = [T("Reportes registrados", null, C("activity", "Actividad", s: "Demolición|Cimentación|Estructura"), C("status", "Estado", CT.Badge, "Completed|InProgress|Pending"),
                            C("actualCost", "Costo", CT.Money), C("actor", "Registró", s: "j.pardo|m.torres"), C("registeredAt", "Fecha", CT.Date),
                            C("adjuntos", "Adjuntos", s: "foto-12.jpg, acta.pdf|foto-07.jpg"))],
            },
            M("REC-13", REC, "Cierre y reporte final", "Reconstruccion/Proyectos/{id:guid}/Cierre", Kind.Detail, 'A', "GET P/monitoreo; POST P/cerrar {expectedVersion}; GET P/reporte-final") with
            {
                Stage = 4,
                Print = true,
                Intro = "Revisa el avance, los gastos y los documentos antes de cerrar el proyecto.",
                Cols = [C("finalBudget", "Presupuesto final", CT.Money), C("totalExecuted", "Total ejecutado", CT.Money), C("remainingBudget", "Saldo", CT.Money),
                        C("responsible", "Responsable", s: "Ing. Rojas"), C("contractor", "Contratista", s: "Constructora Andina"),
                        C("fundingSource", "Financiación", s: "Presupuesto nacional"), C("totalActivities", "Actividades", s: "5"),
                        C("totalReports", "Reportes", s: "5"), C("activitiesWithoutReports", "Sin reporte", s: "0"), C("canFinalize", "Puede cerrarse", CT.Badge, "Sí")],
                Tables = [T("Estado de actividades", null, C("name", "Actividad", s: "Demolición|Cimentación|Estructura|Acabados|Entrega"),
                            C("status", "Estado", CT.Badge, "Completed"), C("reportes", "Reportes", CT.Number), C("evidencias", "Evidencias", CT.Number))],
                Dialogs = [D("cerrar", "Cerrar proyecto", "Cerrar proyecto")],
                Links = [L("Proyecto", "REC-05")],
            },

            // ===== 8. Gestión de recursos =====
            M("RES-01", RES, "Resumen de recursos", "Recursos", Kind.Dashboard, 'E',
              "E GET /api/recursos/resumen?disasterId={id}") with
            {
                Intro = "Disponibilidad consolidada de recursos y solicitudes pendientes de atender.",
                Stats = [S("Recursos registrados", "486"), S("Disponibles", "312"), S("Asignados", "151"), S("Solicitudes pendientes", "14")],
                Chart = true, ChartTitle = "Disponibilidad por categoría",
                TableTitle = "Solicitudes pendientes",
                Cols = solicitudesRecursosCols,
                Links =
                [
                    L("Solicitar recurso", "RES-04", true), L("Inventario", "RES-02"),
                    L("Solicitudes y asignaciones", "RES-05"), L("Donaciones", "RES-06"), L("Refugios", "PEO-07"),
                ],
            },
            M("RES-02", RES, "Inventario de recursos", "Recursos/Inventario", Kind.List, 'E',
              "E GET/POST /api/recursos?disasterId={id}") with
            {
                Cols =
                [
                    C("nombre", "Recurso", s: "Agua potable embotellada|Kit de alimentos|Antibiótico genérico|Camión cisterna|Retroexcavadora|Equipo de rescate USAR|Brigadista|Carpa familiar"),
                    C("categoria", "Categoría", s: categoriasRecursos), C("disponible", "Disponible", CT.Number),
                    C("unidad", "Unidad", s: unidadesRecursos),
                    C("ubicacion", "Ubicación", s: "Bodega Armenia|Bodega Cali|Hospital San Juan de Dios|Base Bomberos Manizales"),
                    C("organizacion", "Propietario", s: orgs), C("estado", "Estado", CT.Badge, estadosRecursos),
                    C("actualizado", "Actualizado", CT.Date),
                ],
                RowTo = "RES-03",
                Filters =
                [
                    F("categoria", "Categoría", o: categoriasRecursos), F("estado", "Estado", o: estadosRecursos),
                    F("ubicacion", "Ubicación", o: "Bodega Armenia|Bodega Cali|Hospital San Juan de Dios|Base Bomberos Manizales"),
                ],
                Dialogs =
                [
                    D("crear", "Registrar recurso", "Registrar recurso",
                      F("nombre", "Nombre", req: true), F("categoria", "Categoría", FT.Select, req: true, o: categoriasRecursos),
                      F("cantidad", "Cantidad", FT.Number, req: true, min: 0), F("unidad", "Unidad", FT.Select, req: true, o: unidadesRecursos),
                      F("ubicacion", "Ubicación", req: true),
                      F("organizacion", "Organización propietaria", FT.Select, o: orgs),
                      F("observaciones", "Observaciones", FT.TextArea)),
                ],
                Links = [L("Solicitar recurso", "RES-04", true)],
            },
            M("RES-03", RES, "Detalle de recurso", "Recursos/Inventario/{id:guid}", Kind.Detail, 'E',
              "E GET /api/recursos/{id}; PUT /api/recursos/{id}/ajuste") with
            {
                Cols =
                [
                    C("nombre", "Recurso", s: "Agua potable embotellada"), C("categoria", "Categoría", s: "Agua"),
                    C("disponible", "Disponible", CT.Number), C("unidad", "Unidad", s: "litros"),
                    C("ubicacion", "Ubicación", s: "Bodega Armenia"), C("organizacion", "Propietario", s: "Defensa Civil"),
                    C("estado", "Estado", CT.Badge, "Disponible"), C("actualizado", "Actualizado", CT.Date),
                    C("observaciones", "Observaciones", s: "Reserva para atención de emergencia"),
                ],
                Tables =
                [
                    T("Asignaciones", null, C("solicitud", "Solicitud", s: "SOL-0101|SOL-0102"),
                      C("solicitante", "Solicitante", s: orgs), C("cantidad", "Cantidad", CT.Number),
                      C("fecha", "Fecha", CT.Date), C("estado", "Estado", CT.Badge, "Asignado|Entregado")),
                    T("Movimientos", null, C("fecha", "Fecha", CT.Date),
                      C("tipo", "Tipo", s: "Ingreso|Salida|Ajuste"), C("cantidad", "Cantidad", CT.Number),
                      C("usuario", "Usuario", s: "j.pardo|m.torres|a.vargas")),
                ],
                Dialogs =
                [
                    D("ajustar", "Ajustar existencias", "Ajustar existencias",
                      F("cantidad", "Cantidad", FT.Number, req: true, min: 0),
                      F("motivo", "Motivo", FT.TextArea, req: true)),
                ],
            },
            M("RES-04", RES, "Solicitud de recursos", "Recursos/Solicitudes/Crear", Kind.Form, 'N',
              "N POST /api/recursos/solicitudes con Idempotency-Key") with
            {
                Intro = "Los organismos autorizados solicitan recursos para atender un desastre.",
                SaveTo = "RES-05", CancelTo = "RES-05",
                Fields =
                [
                    F("disasterId", "Desastre", FT.Select, req: true, o: desastres),
                    F("organizationId", "Organismo solicitante", FT.Select, req: true, o: orgs),
                    F("categoria", "Categoría", FT.Select, req: true, o: categoriasRecursos),
                    F("recurso", "Recurso o descripción", req: true, max: 200),
                    F("cantidad", "Cantidad", FT.Number, req: true, min: 1),
                    F("unidad", "Unidad", FT.Select, req: true, o: unidadesRecursos),
                    F("prioridad", "Prioridad", FT.Select, req: true, o: prios),
                    F("requeridoPara", "Fecha requerida", FT.DateTime, req: true),
                    F("entrega", "Lugar de entrega", req: true, max: 200),
                    F("justificacion", "Justificación", FT.TextArea, req: true, max: 1000),
                ],
            },
            M("RES-05", RES, "Solicitudes y asignaciones", "Recursos/Solicitudes", Kind.List, 'N',
              "N GET /api/recursos/solicitudes; PUT /api/recursos/solicitudes/{id}/asignacion") with
            {
                Cols = solicitudesRecursosCols,
                Filters = [F("prioridad", "Prioridad", o: prios), F("estado", "Estado", o: "Pendiente|Aprobada|Asignada|Entregada|Rechazada")],
                Dialogs =
                [
                    D("asignar", "Asignar recurso", "Asignar recurso",
                      F("codigo", "Código de solicitud", req: true), F("recurso", "Recurso", req: true),
                      F("cantidad", "Cantidad", FT.Number, req: true, min: 1),
                      F("origen", "Bodega de origen", FT.Select, req: true, o: bodegas),
                      F("observaciones", "Observaciones", FT.TextArea)),
                    D("rechazar", "Rechazar solicitud", "Rechazar solicitud",
                      F("codigo", "Código de solicitud", req: true),
                      F("motivo", "Motivo", FT.TextArea, req: true, max: 1000)),
                ],
                Links = [L("Nueva solicitud", "RES-04", true), L("Inventario", "RES-02")],
            },
            M("RES-06", RES, "Donaciones", "Recursos/Donaciones", Kind.List, 'E',
              "E GET/POST /api/recursos/donaciones") with
            {
                Optional = true,
                Cols =
                [
                    C("donante", "Donante", s: "Fundación Éxito|Cámara de Comercio|Ciudadano anónimo"),
                    C("tipo", "Tipo", CT.Badge, "Dinero|Alimentos|Medicamentos|Equipos"),
                    C("descripcion", "Descripción", s: "Alimentos no perecederos|Aporte para kits familiares|Equipos de protección"),
                    C("cantidad", "Cantidad", CT.Number), C("receptor", "Receptor", s: orgs), C("fecha", "Fecha", CT.Date),
                    C("estado", "Estado", CT.Badge, "Recibida|En verificación|Distribuida"),
                ],
                Dialogs =
                [
                    D("registrar", "Registrar donación", "Registrar donación",
                      F("donante", "Donante", req: true), F("tipo", "Tipo", FT.Select, req: true, o: "Dinero|Alimentos|Medicamentos|Equipos"),
                      F("descripcion", "Descripción", req: true), F("cantidad", "Cantidad", FT.Number, req: true, min: 1),
                      F("receptor", "Organización receptora", FT.Select, req: true, o: orgs)),
                ],
            },

            // ===== 9. Administración de tipos =====
            M("ADM-01", ADM, "Tipos de desastre y niveles de atención", "Administracion", Kind.List, 'N',
              "N GET/POST /api/administracion/tipos-desastre; GET/PUT /api/administracion/niveles-atencion") with
            {
                Intro = "Los tipos y niveles se configuran aquí, sin cambiar el código.",
                TableTitle = "Tipos de desastre",
                Cols =
                [
                    C("tipo", "Tipo de desastre", s: "Terremoto|Inundación|Incendio|Deslizamiento|Huracán|Erupción volcánica"),
                    C("descripcion", "Descripción", s: "Movimiento del terreno|Desbordamiento de ríos|Fuego no controlado"),
                    C("activo", "Estado", CT.Badge, "Activo|Activo|Inactivo"),
                ],
                Filters = [F("activo", "Estado", o: "Activo|Inactivo")],
                Tables =
                [
                    T("Niveles de atención", null,
                      C("nivel", "Nivel", s: nivelesAtencion),
                      C("criterio", "Criterio", s: "Recursos locales|Coordinación entre organismos|Recursos regionales o nacionales|Coordinación nacional e internacional")),
                ],
                Dialogs =
                [
                    D("crear", "Nuevo tipo de desastre", "Nuevo tipo de desastre",
                      F("nombre", "Nombre", req: true), F("descripcion", "Descripción", FT.TextArea)),
                    D("desactivar", "Desactivar tipo", "Desactivar tipo",
                      F("nombre", "Tipo de desastre", req: true)),
                ],
            },
            M("ADM-02", ADM, "Tipos de activo y categorías de daño", "Administracion/Activos", Kind.List, 'N',
              "N GET/POST /api/administracion/tipos-activo; GET/POST /api/administracion/categorias-dano") with
            {
                Intro = "Administra los tipos de bienes y las categorías usadas para evaluar sus daños.",
                TableTitle = "Tipos de activo",
                Cols =
                [
                    C("tipo", "Tipo de activo", s: "Vivienda|Hospital|Escuela|Puente|Carretera|Red eléctrica"),
                    C("criterios", "Criterios de evaluación", s: "Estructura, habitabilidad|Capacidad, servicios|Estado de la superficie"),
                    C("activo", "Estado", CT.Badge, "Activo|Activo|Inactivo"),
                ],
                Tables =
                [
                    T("Categorías de daño", null,
                      C("categoria", "Categoría", s: "Sin daño|Daño leve|Daño moderado|Daño grave|Destrucción total"),
                      C("descripcion", "Descripción", s: "Sin afectación visible|Daño reparable|Daño estructural grave"),
                      C("aplicaA", "Aplica a", s: "Todos los activos|Edificaciones|Vías e infraestructura")),
                ],
                Dialogs =
                [
                    D("tipo", "Nuevo tipo de activo", "Nuevo tipo de activo",
                      F("nombre", "Tipo de activo", req: true), F("criterios", "Criterios", FT.TextArea)),
                    D("categoria", "Nueva categoría de daño", "Nueva categoría de daño",
                      F("nombre", "Categoría", req: true), F("descripcion", "Descripción", FT.TextArea),
                      F("aplicaA", "Aplica a", req: true)),
                ],
            },
            M("ADM-03", ADM, "Usuarios y roles", "Administracion/Usuarios", Kind.List, 'N',
              "N GET /api/administracion/usuarios; PUT /api/administracion/usuarios/{id}/rol; PUT .../{id}/estado") with
            {
                Intro = "El acceso se controla por rol y por organización.",
                Cols =
                [
                    C("usuario", "Usuario", s: "j.pardo|m.torres|a.vargas"),
                    C("nombre", "Nombre", s: "Juan Pardo|Marta Torres|Ana Vargas"),
                    C("organizacion", "Organización", s: orgs),
                    C("rol", "Rol", s: "Administrador|Coordinador|Evaluador de campo|Consulta"),
                    C("estado", "Estado", CT.Badge, "Activo|Activo|Suspendido"),
                    C("ultimoAcceso", "Último acceso", CT.Date),
                ],
                Filters = [F("rol", "Rol", o: "Administrador|Coordinador|Evaluador de campo|Consulta"),
                           F("estado", "Estado", o: "Activo|Suspendido")],
                Dialogs =
                [
                    D("rol", "Cambiar rol", "Cambiar rol",
                      F("usuario", "Usuario", req: true),
                      F("rol", "Rol", FT.Select, req: true, o: "Administrador|Coordinador|Evaluador de campo|Consulta")),
                    D("estado", "Suspender usuario", "Suspender usuario",
                      F("usuario", "Usuario", req: true), F("motivo", "Motivo", FT.TextArea, req: true)),
                ],
            },
            M("AUD-01", AUD, "Auditoría y trazabilidad", "Auditoria", Kind.List, 'N',
              "N GET /api/auditoria?entidad=&usuario=&desde=&hasta=") with
            {
                Intro = "Quién hizo qué, cuándo, sobre qué información y con qué resultado.",
                Print = true,
                Cols =
                [
                    C("occurredAt", "Fecha y hora", CT.Date), C("usuario", "Usuario", s: "j.pardo|m.torres|a.vargas"),
                    C("organizacion", "Organización", s: orgs),
                    C("accion", "Acción", s: "Cambió nivel de atención|Asignó recurso|Modificó presupuesto|Actualizó persona afectada|Cambió estado del proyecto"),
                    C("entidad", "Entidad", s: "Desastre|Recurso|Presupuesto|Persona afectada|Proyecto"),
                    C("referencia", "Sobre qué", s: "DIS-0001|SOL-0101|PRY-0007"),
                    C("resultado", "Resultado", CT.Badge, "Exitoso|Rechazado|Error"),
                ],
                Filters =
                [
                    F("entidad", "Entidad", o: "Desastre|Recurso|Presupuesto|Persona afectada|Proyecto"),
                    F("resultado", "Resultado", o: "Exitoso|Rechazado|Error"),
                    F("organizacion", "Organización", o: orgs),
                ],
            },
            M("INT-01", INT, "Estado de integraciones", "Integraciones", Kind.Dashboard, 'N',
              "N GET /api/integraciones/estado; POST /api/integraciones/{id}/reintento") with
            {
                Intro = "Disponibilidad de los sistemas externos y mensajes pendientes de sincronizar.",
                Stats =
                [
                    S("Sistemas conectados", "7"), S("Disponibles", "5"),
                    S("Degradados o caídos", "2"), S("Mensajes en cola", "38"),
                ],
                TableTitle = "Estado de los sistemas",
                Cols =
                [
                    C("sistema", "Sistema", s: "Alertas y monitoreo|Sistema GIS|Sistema hospitalario|Gestión de recursos|Personas afectadas (Eden)|Financiero|Notificaciones"),
                    C("tipo", "Mecanismo", s: "REST|Eventos|Webhook|Mensajería"),
                    C("estado", "Estado", CT.Badge, "Disponible|Degradado|No disponible"),
                    C("circuito", "Circuito", CT.Badge, "Normal|En corte|Prueba"),
                    C("ultimo", "Último intento", CT.Date), C("cola", "En cola", CT.Number),
                ],
                Tables =
                [
                    T("Pendientes de sincronizar", null,
                      C("operacion", "Operación", s: "Actualizar desastre|Registrar evaluación|Asignar recurso"),
                      C("sistema", "Sistema", s: "Sistema GIS|Sistema hospitalario|Gestión de recursos"),
                      C("creada", "Creada", CT.Date), C("intentos", "Intentos", CT.Number),
                      C("estado", "Estado", CT.Badge, "Pendiente|Reintentando|Fallida")),
                ],
                Dialogs =
                [
                    D("reintentar", "Reintentar envío", "Reintentar envío",
                      F("sistema", "Sistema", FT.Select, req: true,
                        o: "Alertas y monitoreo|Sistema GIS|Sistema hospitalario|Gestión de recursos|Personas afectadas|Financiero|Notificaciones")),
                ],
            },
        };
    }
}