using System.Text.RegularExpressions;

namespace TiendaPc.Services
{
    public class SunatService : ISunatService
    {
        public Task<SunatInfoDto?> ConsultarPorRucAsync(string ruc)
        {
            // Valida RUC simple
            ruc = (ruc ?? "").Trim();
            if (!Regex.IsMatch(ruc, @"^\d{11}$")) return Task.FromResult<SunatInfoDto?>(null);

            // TODO: aquí llamas a tu API real de SUNAT
            // Stub de ejemplo:
            var fake = new SunatInfoDto
            {
                Ruc = ruc,
                RazonSocial = "EMPRESA DE PRUEBA S.A.C.",
                Direccion = "Av. Ejemplo 123 - Arequipa"
            };
            return Task.FromResult<SunatInfoDto?>(fake);
        }
    }
}
