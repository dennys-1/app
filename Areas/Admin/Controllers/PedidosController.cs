using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiendaPc.Data;

namespace TiendaPc.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class PedidosController : Controller
{
    private readonly AppDbContext _db;
    public PedidosController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? estado, DateTime? desde, DateTime? hasta)
    {
        var q = _db.Pedidos.AsQueryable();

        if (!string.IsNullOrWhiteSpace(estado))
            q = q.Where(p => p.Estado == estado);
        if (desde.HasValue)
            q = q.Where(p => p.CreadoEn >= desde.Value);
        if (hasta.HasValue)
            q = q.Where(p => p.CreadoEn <= hasta.Value.AddDays(1));

        var list = await q.OrderByDescending(p => p.CreadoEn).Take(200).ToListAsync();
        ViewBag.Estado = estado; ViewBag.Desde = desde; ViewBag.Hasta = hasta;
        return View(list);
    }

    public async Task<IActionResult> Detalle(int id)
    {
        var p = await _db.Pedidos.FirstOrDefaultAsync(x => x.IdPedido == id);
        if (p == null) return NotFound();

        var items = await (from d in _db.ItemsPedido
                           join pr in _db.Productos on d.IdProducto equals pr.IdProducto
                           where d.IdPedido == id
                           select new { pr.Nombre, pr.Sku, d.Cantidad, d.PrecioUnitario, d.TotalLinea }).ToListAsync();
        ViewBag.Items = items;
        return View(p);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id, string estado)
    {
        var p = await _db.Pedidos.FindAsync(id);
        if (p == null) return NotFound();
        p.Estado = estado;
        await _db.SaveChangesAsync();
        TempData["msg"] = "Estado actualizado.";
        return RedirectToAction(nameof(Detalle), new { id });
    }
}
