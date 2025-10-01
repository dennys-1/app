using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiendaPc.Data;

namespace TiendaPc.Controllers;

public class OfertasController : Controller
{
    private readonly AppDbContext _db;
    public OfertasController(AppDbContext db) { _db = db; }

    // /Ofertas?orden=precio&pagina=1
    [HttpGet("/Ofertas")]
    public async Task<IActionResult> Index(string? orden = "fin", int pagina = 1, int tam = 12)
    {
        pagina = Math.Max(1, pagina);
        tam = Math.Clamp(tam, 8, 48);
        var ahora = DateTimeOffset.UtcNow;

        var baseQ =
            from rd in _db.ReglasPrecioDetalle
            join r in _db.ReglasPrecio on rd.IdRegla equals r.IdRegla
            join p in _db.Productos on rd.IdProducto equals p.IdProducto
            where r.Activo && r.Tipo == "OFERTA" && r.Inicio <= ahora && r.Fin >= ahora && p.Activo
            select new OfertaVm {
                IdProducto = p.IdProducto,
                Nombre = p.Nombre,
                Sku = p.Sku,
                PrecioNormal = p.Precio,
                PrecioWeb = rd.PrecioPromocional ?? p.Precio,
                Fin = r.Fin
            };

        // Orden
        baseQ = orden switch
        {
            "precio" => baseQ.OrderBy(x => x.PrecioWeb),
            "nombre" => baseQ.OrderBy(x => x.Nombre),
            _ => baseQ.OrderBy(x => x.Fin) // default por fin de promo
        };

        // Paginación
        var total = await baseQ.CountAsync();
        var items = await baseQ.Skip((pagina - 1) * tam).Take(tam).ToListAsync();

        var vm = new OfertasPageVm {
            Items = items,
            Orden = orden ?? "fin",
            Pagina = pagina,
            Tam = tam,
            Total = total
        };
        return View(vm);
    }
}

public record OfertaVm
{
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = "";
    public string Sku { get; set; } = "";
    public decimal PrecioNormal { get; set; }
    public decimal PrecioWeb { get; set; }
    public DateTimeOffset Fin { get; set; }
}

public class OfertasPageVm
{
    public List<OfertaVm> Items { get; set; } = new();
    public string Orden { get; set; } = "fin";
    public int Pagina { get; set; } = 1;
    public int Tam { get; set; } = 12;
    public int Total { get; set; } = 0;

    public int TotalPaginas => (int)Math.Ceiling((double)Math.Max(0, Total) / Math.Max(1, Tam));
}
