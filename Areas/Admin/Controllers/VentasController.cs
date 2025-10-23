using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using TiendaPc.Data;

namespace TiendaPc.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class VentasController : Controller
{
    private readonly AppDbContext _db;
    public VentasController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(DateTime? desde, DateTime? hasta)
    {
        var q = _db.Pedidos.Where(p => (p.Estado == "Pagado" || p.Estado == "Completado"));

        if (desde.HasValue)
            q = q.Where(p => p.CreadoEn >= new DateTimeOffset(desde.Value));
        if (hasta.HasValue)
        {
            var h = hasta.Value.Date.AddDays(1).AddTicks(-1);
            q = q.Where(p => p.CreadoEn <= new DateTimeOffset(h));
        }

        var list = await q
            .OrderByDescending(p => p.CreadoEn)
            .Select(p => new VentaVm
            {
                Numero = p.NumeroPedido ?? ("P" + p.IdPedido.ToString("D6")),
                Fecha = p.CreadoEn,
                Total = p.Total,
                Estado = p.Estado ?? "-"
            }).ToListAsync();

        ViewBag.Total = list.Sum(x => x.Total);
        ViewBag.Desde = desde; ViewBag.Hasta = hasta;
        return View(list);
    }

    [HttpGet]
    public async Task<FileResult> Csv(DateTime? desde, DateTime? hasta)
    {
        var q = _db.Pedidos.Where(p => (p.Estado == "Pagado" || p.Estado == "Completado"));
        if (desde.HasValue) q = q.Where(p => p.CreadoEn >= new DateTimeOffset(desde.Value));
        if (hasta.HasValue)
        {
            var h = hasta.Value.Date.AddDays(1).AddTicks(-1);
            q = q.Where(p => p.CreadoEn <= new DateTimeOffset(h));
        }

        var rows = await q.OrderBy(p => p.CreadoEn).ToListAsync();
        var sb = new StringBuilder();
        sb.AppendLine("Numero,Fecha,Total,Estado");
        foreach (var x in rows)
            sb.AppendLine($"{(x.NumeroPedido ?? $"P{x.IdPedido:D6}")},{x.CreadoEn.LocalDateTime:yyyy-MM-dd HH:mm},{x.Total:N2},{x.Estado}");

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"ventas_{DateTime.Now:yyyyMMddHHmm}.csv");
    }

    public class VentaVm
    {
        public string Numero { get; set; } = "-";
        public DateTimeOffset Fecha { get; set; }
        public decimal Total { get; set; }
        public string Estado { get; set; } = "-";
    }
}
