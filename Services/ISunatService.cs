namespace TiendaPc.Services;

public record SunatRucDto(string? Ruc, string? RazonSocial, string? Direccion, string? Estado);

public interface ISunatService
{
    Task<SunatRucDto?> BuscarRucAsync(string ruc, CancellationToken ct = default);
}
