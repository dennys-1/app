using System.Net.Http.Headers;
using System.Text.Json;

namespace TiendaPc.Services;

public interface ISunatClient
{
    Task<SunatRucResult?> BuscarRucAsync(string ruc, CancellationToken ct = default);
}

public class SunatClient : ISunatClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _token;

    public SunatClient(HttpClient http, IConfiguration cfg)
    {
        _http = http;
        _baseUrl = cfg["Sunat:BaseUrl"] ?? "";
        _token   = cfg["Sunat:Token"] ?? "";
    }

    public async Task<SunatRucResult?> BuscarRucAsync(string ruc, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_baseUrl))
            return new SunatRucResult { Ok = false, Error = "SUNAT BaseUrl no configurado." };

        var url = $"{_baseUrl}?numero={ruc}";
        var req = new HttpRequestMessage(HttpMethod.Get, url);

        if (!string.IsNullOrWhiteSpace(_token))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);

        using var res = await _http.SendAsync(req, ct);
        if (!res.IsSuccessStatusCode)
        {
            var txt = await res.Content.ReadAsStringAsync(ct);
            return new SunatRucResult { Ok = false, Error = $"HTTP {(int)res.StatusCode}: {txt}" };
        }

        var json = await res.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);

        // Mapeo flexible de posibles campos según proveedor de API
        // nombres comunes: "razonSocial" | "nombre", "direccion" | "domicilioFiscal"
        string razon = doc.RootElement.TryGetProperty("razonSocial", out var rs) ? rs.GetString() ?? "" :
                       doc.RootElement.TryGetProperty("nombre", out var nm) ? nm.GetString() ?? "" : "";

        string direccion = doc.RootElement.TryGetProperty("direccion", out var dr) ? dr.GetString() ?? "" :
                           doc.RootElement.TryGetProperty("domicilioFiscal", out var df) ? df.GetString() ?? "" : "";

        if (string.IsNullOrWhiteSpace(razon))
            return new SunatRucResult { Ok = false, Error = "No se encontró razón social en la respuesta." };

        return new SunatRucResult
        {
            Ok = true,
            Ruc = ruc,
            RazonSocial = razon,
            Direccion = direccion
        };
    }
}

public class SunatRucResult
{
    public bool Ok { get; set; }
    public string? Error { get; set; }
    public string? Ruc { get; set; }
    public string? RazonSocial { get; set; }
    public string? Direccion { get; set; }
}
