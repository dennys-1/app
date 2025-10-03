using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiendaPc.Data;
using TiendaPc.Models;

namespace TiendaPc.Controllers;

[Authorize]
public class CheckoutController : Controller
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _cfg;

    public CheckoutController(AppDbContext db, IConfiguration cfg)
    {
        _db = db;
        _cfg = cfg;
    }

    private const string COOKIE_KEY = "carrito_id";  // Debe coincidir con el usado en Carrito
    private const int ALMACEN_VENTA = 1;

    private Guid? GetCarritoId()
    {
        if (Request.Cookies.TryGetValue(COOKIE_KEY, out var val) && Guid.TryParse(val, out var id))
            return id;
        return null;
    }

    // GET /Checkout
    [HttpGet("/Checkout")]
    public async Task<IActionResult> Index()
    {
        var carritoId = GetCarritoId();
        if (carritoId == null) return RedirectToAction("Index", "Carrito");

        var items = await (
            from i in _db.ItemsCarrito
            join p in _db.Productos on i.IdProducto equals p.IdProducto
            where i.IdCarrito == carritoId.Value
            select new CheckoutItemVm
            {
                IdItem = i.IdItem,
                IdProducto = p.IdProducto,
                Nombre = p.Nombre,
                Sku = p.Sku,
                Cantidad = i.Cantidad,
                PrecioUnitario = i.PrecioUnitario,
                Total = i.Cantidad * i.PrecioUnitario
            }
        ).ToListAsync();

        if (items.Count == 0) return RedirectToAction("Index", "Carrito");

        var subtotal = items.Sum(x => x.Total);
        var igvRate = _cfg.GetSection("Impuestos").GetValue<decimal>("IGV", 0.18m);
        var igv = Math.Round(subtotal * igvRate, 2, MidpointRounding.AwayFromZero);
        var total = subtotal + igv;

        return View(new CheckoutVm { Items = items, Subtotal = subtotal, Igv = igv, Total = total });
    }

    // POST /Checkout/Confirmar
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
            select new { i, p }
        ).ToListAsync();

        if (items.Count == 0) return RedirectToAction("Index", "Carrito");

        var igvRate = _cfg.GetSection("Impuestos").GetValue<decimal>("IGV", 0.18m);
        var subtotal = items.Sum(x => x.i.Cantidad * x.i.PrecioUnitario);
        var igv = Math.Round(subtotal * igvRate, 2, MidpointRounding.AwayFromZero);
        var total = subtotal + igv;

        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 1) Crear cabecera (numero_pedido lo genera la BD; NO asignarlo aquí)
            var pedido = new Pedido
            {
                IdUsuario = userId,
                Subtotal = subtotal,
                Impuesto = igv,
                Total = total,
                Estado = "Pendiente",
                CreadoEn = DateTimeOffset.UtcNow
            };

            _db.Pedidos.Add(pedido);
            await _db.SaveChangesAsync();        // genera id_pedido (y numero_pedido si tiene DEFAULT)

            // Si numero_pedido lo setea la BD (DEFAULT/trigger), ya viene en la entidad.
            // Si prefieres asegurar lectura, puedes descomentar:
            // await _db.Entry(pedido).ReloadAsync();

            // 2) Insertar detalle (NO asignar TotalLinea: lo calcula la BD)
            foreach (var it in items)
            {
                _db.ItemsPedido.Add(new ItemPedido
                {
                    IdPedido = pedido.IdPedido,        // FK real a pedido.id_pedido
                    IdProducto = it.p.IdProducto,
                    Cantidad = it.i.Cantidad,
                    PrecioUnitario = it.i.PrecioUnitario
                    // TotalLinea: NO asignar (columna computada)
                });

                // Asegurar fila de stock para (producto, almacén) sin SaveChanges dentro del bucle
                var existeStock = await _db.Stocks
                    .AnyAsync(s => s.IdProducto == it.p.IdProducto && s.IdAlmacen == ALMACEN_VENTA);

                if (!existeStock)
                {
                    _db.Stocks.Add(new Stock
                    {
                        IdProducto = it.p.IdProducto,
                        IdAlmacen = ALMACEN_VENTA,
                        Cantidad = 0
                    });
                }

                _db.MovimientosInventario.Add(new MovimientoInventario
                {
                    IdAlmacen = ALMACEN_VENTA,
                    IdProducto = it.p.IdProducto,
                    Fecha = DateTimeOffset.UtcNow,
                    Tipo = "SALIDA_VENTA",
                    Referencia = $"P{pedido.IdPedido}",   // o usa pedido.NumeroPedido si lo tienes formateado
                    Cantidad = -it.i.Cantidad,
                    CostoUnitario = 0,
                    Observacion = "Checkout"
                });
            }

            await _db.SaveChangesAsync(); // guarda detalle, stock que faltaba y movimientos

            // 3) Vaciar carrito
            var all = _db.ItemsCarrito.Where(x => x.IdCarrito == carritoId.Value);
            _db.ItemsCarrito.RemoveRange(all);
            await _db.SaveChangesAsync();

            await tx.CommitAsync();
            return RedirectToAction("Exito", new { id = pedido.IdPedido });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            Console.WriteLine("ERROR Checkout/Confirmar: " + ex);
            TempData["msg"] = "Error al confirmar: " + (ex.InnerException?.Message ?? ex.Message);
            return RedirectToAction("Index");
        }
    }

    // GET /Checkout/Exito/{id}
    [HttpGet("/Checkout/Exito/{id:int}")]
    public async Task<IActionResult> Exito(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var p = await _db.Pedidos.FirstOrDefaultAsync(x => x.IdPedido == id && x.IdUsuario == userId);
        if (p == null) return NotFound();
        return View(p); // sin VM extra; trabajas directo con Pedido como dijiste
    }
}

// ====== ViewModels ======
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
