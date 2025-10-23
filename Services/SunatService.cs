using System.Net.Http.Json;

namespace TiendaPc.Services;

public class SunatService : ISunatService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _cfg;

    public SunatService(HttpClient http, IConfiguration cfg)
    {
      _http = http;
      _cfg = cfg;
    }

    public async Task<SunatRucDto?> BuscarRucAsync(string ruc, CancellationToken ct = default)
    {
        var baseUrl = _cfg["Sunat:BaseUrl"];
        var token   = _cfg["Sunat:Token"];

        // sin config -> modo demo para desarrollo
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(token))
            return new SunatRucDto(ruc, "Proveedor de Prueba S.A.C.", "Av. Siempre Viva 123", "ACTIVO");

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl.TrimEnd('/')}/v2/sunat/ruc?numero={ruc}");
            req.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");
            var resp = await _http.SendAsync(req, ct);
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadFromJsonAsync<dynamic>(cancellationToken: ct);
            if (json == null) return null;

            string? razon  = json.razonSocial ?? json.nombre ?? json["razonSocial"];
            string? direc  = json.direccion   ?? json["direccion"];
            string? estado = json.estado      ?? json["estado"];

            return new SunatRucDto(ruc, razon, direc, estado);
        }
        catch { return null; }
    }
}
