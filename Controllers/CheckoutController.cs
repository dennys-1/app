using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiendaPc.Data;

namespace TiendaPc.Controllers;

[Authorize]
public class CheckoutController : Controller
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _cfg;
    public CheckoutController(AppDbContext db, IConfiguration cfg) { _db = db; _cfg = cfg; }

    const string COOKIE_KEY = "carrito_id";

    private Guid? GetCarritoId()
    {
        if (Request.Cookies.TryGetValue(COOKIE_KEY, out var val) && Guid.TryParse(val, out var id)) return id;
        return null;
    }

    // >>> Ruta explícita <<<
    [HttpGet("/Checkout")]
    public async Task<IActionResult> Index()
    {
        var carritoId = GetCarritoId();
        if (carritoId == null) return RedirectToAction("Index", "Carrito");

        var items = await (
            from i in _db.ItemsCarrito
            join p in _db.Productos on i.IdProducto equals p.IdProducto
            where i.IdCarrito == carritoId.Value
            select new CheckoutItemVm {
                IdItem = i.IdItem, IdProducto = p.IdProducto, Nombre = p.Nombre, Sku = p.Sku,
                Cantidad = i.Cantidad, PrecioUnitario = i.PrecioUnitario, Total = i.Cantidad * i.PrecioUnitario
            }).ToListAsync();

        if (items.Count == 0) return RedirectToAction("Index", "Carrito");

        var subtotal = items.Sum(x => x.Total);
        var igvRate = _cfg.GetSection("Impuestos").GetValue<decimal>("IGV", 0.18m);
        var igv = Math.Round(subtotal * igvRate, 2);
        var total = subtotal + igv;

        return View(new CheckoutVm { Items = items, Subtotal = subtotal, Igv = igv, Total = total });
    }

    [HttpPost("/Checkout/Confirmar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirmar()
    {
        var carritoId = GetCarritoId();
        if (carritoId == null) return RedirectToAction("Index", "Carrito");
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var items = await (
            from i in _db.ItemsCarrito
            join p in _db.Productos on i.IdProducto equals p.IdProducto
            where i.IdCarrito == carritoId.Value
            select new { i, p }).ToListAsync();

        if (items.Count == 0) return RedirectToAction("Index", "Carrito");

        var subtotal = items.Sum(x => x.i.Cantidad * x.i.PrecioUnitario);
        var igvRate = _cfg.GetSection("Impuestos").GetValue<decimal>("IGV", 0.18m);
        var igv = Math.Round(subtotal * igvRate, 2);
        var total = subtotal + igv;

        using var tx = await _db.Database.BeginTransactionAsync();
        var pedido = new TiendaPc.Models.Pedido {
            IdUsuario = userId, Subtotal = subtotal, Impuesto = igv, Total = total,
            Estado = "Pendiente", CreadoEn = DateTimeOffset.UtcNow
        };
        _db.Pedidos.Add(pedido);
        await _db.SaveChangesAsync();

        foreach (var it in items)
        {
            _db.ItemsPedido.Add(new TiendaPc.Models.ItemPedido {
                IdPedido = pedido.IdPedido, IdProducto = it.p.IdProducto,
                Cantidad = it.i.Cantidad, PrecioUnitario = it.i.PrecioUnitario
            });
            // Si quieres, aquí puedes agregar movimiento de inventario (salida venta)
        }
        await _db.SaveChangesAsync();

        _db.ItemsCarrito.RemoveRange(_db.ItemsCarrito.Where(x => x.IdCarrito == carritoId.Value));
        await _db.SaveChangesAsync();

        await tx.CommitAsync();
        return RedirectToAction("Exito", new { id = pedido.IdPedido });
    }

    [HttpGet("/Checkout/Exito/{id:int}")]
    public async Task<IActionResult> Exito(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var p = await _db.Pedidos.FirstOrDefaultAsync(x => x.IdPedido == id && x.IdUsuario == userId);
        if (p == null) return NotFound();
        return View(p);
    }
}

public class CheckoutVm
{
    public List<CheckoutItemVm> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal Igv { get; set; }
    public decimal Total { get; set; }
}
public class CheckoutItemVm
{
    public int IdItem { get; set; }
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = "";
    public string Sku { get; set; } = "";
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Total { get; set; }
}
