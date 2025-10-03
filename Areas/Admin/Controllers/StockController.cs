using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiendaPc.Data;

namespace TiendaPc.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class StockController : Controller
{
    private readonly AppDbContext _db;
    public StockController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(int? almacenId)
    {
        ViewBag.Almacenes = await _db.Almacenes.OrderBy(x=>x.Nombre).ToListAsync();

        var q = from s in _db.Stocks
                join p in _db.Productos on s.IdProducto equals p.IdProducto
                join a in _db.Almacenes on s.IdAlmacen equals a.IdAlmacen
                select new { s.IdProducto, p.Sku, p.Nombre, s.IdAlmacen, Almacen = a.Nombre, s.Cantidad };

        if (almacenId.HasValue) q = q.Where(x => x.IdAlmacen == almacenId.Value);

        var list = await q.OrderBy(x=>x.Almacen).ThenBy(x=>x.Nombre).ToListAsync();
        return View(list);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Ajustar(int idProducto, int idAlmacen, int cantidad, string? motivo)
    {
        var s = await _db.Stocks.FirstOrDefaultAsync(x => x.IdProducto == idProducto && x.IdAlmacen == idAlmacen);
        if (s == null)
        {
            s = new Models.Stock { IdProducto = idProducto, IdAlmacen = idAlmacen, Cantidad = 0 };
            _db.Stocks.Add(s);
        }
        s.Cantidad += cantidad; // cantidad puede ser negativa para disminuir
        await _db.SaveChangesAsync();

        _db.MovimientosInventario.Add(new Models.MovimientoInventario {
            IdProducto = idProducto, IdAlmacen = idAlmacen, Fecha = DateTimeOffset.UtcNow,
            Tipo = cantidad >= 0 ? "AJUSTE_ENTRADA" : "AJUSTE_SALIDA",
            Referencia = "AJUSTE_MANUAL", Cantidad = cantidad, CostoUnitario = 0, Observacion = motivo ?? ""
        });
        await _db.SaveChangesAsync();

        TempData["msg"] = "Ajuste aplicado.";
        return RedirectToAction(nameof(Index), new { almacenId = idAlmacen });
    }
}
