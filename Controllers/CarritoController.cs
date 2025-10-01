using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiendaPc.Data;

namespace TiendaPc.Controllers;

public class CarritoController : Controller
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _cfg;
    public CarritoController(AppDbContext db, IConfiguration cfg) { _db = db; _cfg = cfg; }

    const string COOKIE_KEY = "carrito_id";

    // Crea/lee el GUID del carrito desde cookie
    private async Task<Guid> EnsureCarritoId()
    {
        if (!Request.Cookies.TryGetValue(COOKIE_KEY, out var val) || !Guid.TryParse(val, out var id))
        {
            id = Guid.NewGuid();
            await _db.Carritos.AddAsync(new Models.Carrito { IdCarrito = id, CreadoEn = DateTimeOffset.UtcNow });
            await _db.SaveChangesAsync();
            Response.Cookies.Append(COOKIE_KEY, id.ToString(), new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(14), IsEssential = true });
        }
        return id;
    }

    // Precio vigente = Oferta activa (si existe) o precio de producto
    private async Task<decimal> GetPrecioVigenteAsync(int idProducto)
    {
        var ahora = DateTimeOffset.UtcNow;
        var query =
            from rd in _db.ReglasPrecioDetalle
            join r in _db.ReglasPrecio on rd.IdRegla equals r.IdRegla
            where r.Activo && r.Tipo == "OFERTA" && r.Inicio <= ahora && r.Fin >= ahora && rd.IdProducto == idProducto
            select rd.PrecioPromocional;

        var promo = await query.FirstOrDefaultAsync();
        if (promo.HasValue) return promo.Value;

        var basePrice = await _db.Productos.Where(p => p.IdProducto == idProducto).Select(p => p.Precio).FirstOrDefaultAsync();
        return basePrice;
    }

    // GET /Carrito
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var idCarrito = await EnsureCarritoId();
        var items = await (
            from i in _db.ItemsCarrito
            join p in _db.Productos on i.IdProducto equals p.IdProducto
            where i.IdCarrito == idCarrito
            select new CarritoItemVm
            {
                IdItem = i.IdItem,
                IdProducto = p.IdProducto,
                Nombre = p.Nombre,
                Sku = p.Sku,
                Cantidad = i.Cantidad,
                PrecioUnitario = i.PrecioUnitario,
                Total = i.Cantidad * i.PrecioUnitario
            }).ToListAsync();

        var subtotal = items.Sum(x => x.Total);
        var igvRate = _cfg.GetSection("Impuestos").GetValue<decimal>("IGV", 0.18m);
        var igv = Math.Round(subtotal * igvRate, 2);
        var total = subtotal + igv;

        var vm = new CarritoVm { Items = items, Subtotal = subtotal, Igv = igv, Total = total };
        return View(vm);
    }

    // POST /Carrito/Agregar
    [HttpPost]
    public async Task<IActionResult> Agregar(int idProducto, int cantidad = 1)
    {
        if (cantidad <= 0) cantidad = 1;

        var prod = await _db.Productos.FirstOrDefaultAsync(p => p.IdProducto == idProducto && p.Activo);
        if (prod == null) return NotFound();

        // (Opcional) validar stock mínimo en almacén 1
        // var stock = await _db.Stocks.Where(s => s.IdProducto == idProducto && s.IdAlmacen == 1).Select(s => s.Cantidad).FirstOrDefaultAsync();
        // if (stock < cantidad) { TempData["msg"] = "No hay stock suficiente."; return RedirectToAction(nameof(Index)); }

        var idCarrito = await EnsureCarritoId();
        var existente = await _db.ItemsCarrito.FirstOrDefaultAsync(i => i.IdCarrito == idCarrito && i.IdProducto == idProducto);

        var precioVigente = await GetPrecioVigenteAsync(idProducto);

        if (existente == null)
        {
            _db.ItemsCarrito.Add(new TiendaPc.Models.ItemCarrito
            {
                IdCarrito = idCarrito,
                IdProducto = idProducto,
                Cantidad = cantidad,
                PrecioUnitario = precioVigente
            });
        }
        else
        {
            existente.Cantidad += cantidad;
            // Si quieres que el precio se actualice cada vez según oferta vigente, descomenta:
            // existente.PrecioUnitario = precioVigente;
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // POST /Carrito/ActualizarCantidad
    [HttpPost]
    public async Task<IActionResult> ActualizarCantidad(int idItem, int cantidad)
    {
        var it = await _db.ItemsCarrito.FindAsync(idItem);
        if (it == null) return NotFound();

        if (cantidad <= 0)
        {
            _db.ItemsCarrito.Remove(it);
        }
        else
        {
            it.Cantidad = cantidad;
        }
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // POST /Carrito/Quitar
    [HttpPost]
    public async Task<IActionResult> Quitar(int idItem)
    {
        var it = await _db.ItemsCarrito.FindAsync(idItem);
        if (it != null)
        {
            _db.ItemsCarrito.Remove(it);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    // POST /Carrito/Vaciar
    [HttpPost]
    public async Task<IActionResult> Vaciar()
    {
        var idCarrito = await EnsureCarritoId();
        var items = _db.ItemsCarrito.Where(i => i.IdCarrito == idCarrito);
        _db.ItemsCarrito.RemoveRange(items);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}

// ==== ViewModels ====
public class CarritoVm
{
    public List<CarritoItemVm> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal Igv { get; set; }
    public decimal Total { get; set; }
}
public class CarritoItemVm
{
    public int IdItem { get; set; }
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = "";
    public string Sku { get; set; } = "";
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Total { get; set; }
}
