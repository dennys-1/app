namespace TiendaPc.Services
{
    public class SunatInfoDto
    {
        public string? Ruc { get; set; }
        public string? RazonSocial { get; set; }
        public string? Direccion { get; set; }
    }

    public interface ISunatService
    {
        Task<SunatInfoDto?> ConsultarPorRucAsync(string ruc);
    }
}
