using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiendaPc.Data;

namespace TiendaPc.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;
    public HomeController(AppDbContext db) { _db = db; }

    public async Task<IActionResult> Index()
    {
        // Promos activas (OFERTA) con productos
        var ahora = DateTimeOffset.UtcNow;
        var promos = await (
            from rd in _db.ReglasPrecioDetalle
            join r in _db.ReglasPrecio on rd.IdRegla equals r.IdRegla
            join p in _db.Productos on rd.IdProducto equals p.IdProducto
            where r.Activo && r.Tipo == "OFERTA" && r.Inicio <= ahora && r.Fin >= ahora && p.Activo
            orderby r.Fin
            select new PromoVm {
                IdProducto = p.IdProducto,
                Nombre = p.Nombre,
                Sku = p.Sku,
                PrecioNormal = p.Precio,
                PrecioWeb = rd.PrecioPromocional ?? p.Precio,
                Fin = r.Fin
            }
        ).Take(8).ToListAsync();

        // Marcas destacadas (las que tengan productos activos)
        var marcas = await (
            from p in _db.Productos
            join m in _db.Marcas on p.IdMarca equals m.IdMarca
            where p.Activo
            group m by new { m.IdMarca, m.Nombre } into g
            orderby g.Count() descending
            select new MarcaVm { IdMarca = g.Key.IdMarca, Nombre = g.Key.Nombre }
        ).Take(10).ToListAsync();

        return View(new HomeVm { Promos = promos, Marcas = marcas });
    }
}

public record PromoVm {
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = "";
    public string Sku { get; set; } = "";
    public decimal PrecioNormal { get; set; }
    public decimal PrecioWeb { get; set; }
    public DateTimeOffset Fin { get; set; }
}
public record MarcaVm {
    public int IdMarca { get; set; }
    public string Nombre { get; set; } = "";
}
public class HomeVm {
    public List<PromoVm> Promos { get; set; } = new();
    public List<MarcaVm> Marcas { get; set; } = new();
}
