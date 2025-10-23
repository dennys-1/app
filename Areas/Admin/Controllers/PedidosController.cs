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

    // GET: /Admin/Pedidos
    // Filtros: q (nro pedido o usuario), estado, rango de fechas
    public async Task<IActionResult> Index(string? q, string? estado, DateTime? desde, DateTime? hasta)
    {
        var query = _db.Pedidos.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p =>
                (p.NumeroPedido ?? "").Contains(q) ||
                (p.IdUsuario ?? "").Contains(q));

        if (!string.IsNullOrWhiteSpace(estado))
            query = query.Where(p => (p.Estado ?? "") == estado);

        if (desde.HasValue)
            query = query.Where(p => p.CreadoEn >= new DateTimeOffset(desde.Value));
        if (hasta.HasValue)
        {
            var h = hasta.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(p => p.CreadoEn <= new DateTimeOffset(h));
        }

        var list = await query
            .OrderByDescending(p => p.CreadoEn)
            .Select(p => new PedidoListVm
            {
                IdPedido = p.IdPedido,
                Numero   = p.NumeroPedido ?? ("P" + p.IdPedido.ToString("D6")),
                Estado   = p.Estado ?? "Pendiente",
                Total    = p.Total,
                CreadoEn = p.CreadoEn,
                Usuario  = p.IdUsuario ?? "-"
            })
            .ToListAsync();

        ViewBag.Estado = estado; ViewBag.Q = q; ViewBag.Desde = desde; ViewBag.Hasta = hasta;
        return View(list);
    }

    // GET: /Admin/Pedidos/Detalle/5
    public async Task<IActionResult> Detalle(int id)
    {
        // Traemos el pedido
        var p = await _db.Pedidos.FirstOrDefaultAsync(x => x.IdPedido == id);
        if (p == null) return NotFound();

        // Items
        var items = await (
            from d in _db.ItemsPedido
            join pr in _db.Productos on d.IdProducto equals pr.IdProducto
            where d.IdPedido == id
            select new PedidoItemVm
            {
                Producto = pr.Nombre,
                Sku = pr.Sku,
                Cantidad = d.Cantidad,
                PrecioUnitario = d.PrecioUnitario,
                TotalLinea = d.TotalLinea
            }).ToListAsync();

        // Propiedades sombra (pago/envío)
        var referenciaPago = _db.Entry(p).Property<string>("referencia_pago").CurrentValue;
        var pagadoEn       = _db.Entry(p).Property<DateTimeOffset?>("pagado_en").CurrentValue;
        var courier        = _db.Entry(p).Property<string>("courier").CurrentValue;
        var tracking       = _db.Entry(p).Property<string>("tracking").CurrentValue;
        var enviadoEn      = _db.Entry(p).Property<DateTimeOffset?>("enviado_en").CurrentValue;

        var vm = new PedidoAdminVm
        {
            IdPedido = p.IdPedido,
            Numero   = p.NumeroPedido ?? ("P" + p.IdPedido.ToString("D6")),
            Usuario  = p.IdUsuario ?? "-",
            Estado   = p.Estado ?? "Pendiente",
            Subtotal = p.Subtotal,
            Impuesto = p.Impuesto,
            Total    = p.Total,
            CreadoEn = p.CreadoEn,
            Items    = items,

            // Pago / Envío
            ReferenciaPago = referenciaPago,
            PagadoEn       = pagadoEn,
            Courier        = courier,
            Tracking       = tracking,
            EnviadoEn      = enviadoEn
        };

        return View(vm);
    }

    // POST: /Admin/Pedidos/CambiarEstado
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id, string nuevo)
    {
        var p = await _db.Pedidos.FindAsync(id);
        if (p == null) return NotFound();

        p.Estado = nuevo;
        await _db.SaveChangesAsync();
        TempData["msg"] = $"Estado actualizado a {nuevo}.";
        return RedirectToAction(nameof(Detalle), new { id });
    }

    // =====================
    // NUEVO: MARCAR PAGADO
    // =====================
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarPagado(int id, string? referencia)
    {
        var p = await _db.Pedidos.FindAsync(id);
        if (p == null) return NotFound();

        // set sombra
        _db.Entry(p).Property("referencia_pago").CurrentValue = string.IsNullOrWhiteSpace(referencia) ? null : referencia.Trim();
        _db.Entry(p).Property("pagado_en").CurrentValue = DateTimeOffset.UtcNow;

        // opcional: mover estado si estaba Pendiente
        if ((p.Estado ?? "Pendiente").Equals("Pendiente", StringComparison.OrdinalIgnoreCase))
            p.Estado = "Pagado";

        await _db.SaveChangesAsync();
        TempData["msg"] = "Pago registrado.";
        return RedirectToAction(nameof(Detalle), new { id });
    }

    // ======================
    // NUEVO: REGISTRAR ENVÍO
    // ======================
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarEnvio(int id, string? courier, string? tracking, DateTime? fecha)
    {
        var p = await _db.Pedidos.FindAsync(id);
        if (p == null) return NotFound();

        _db.Entry(p).Property("courier").CurrentValue = string.IsNullOrWhiteSpace(courier) ? null : courier.Trim();
        _db.Entry(p).Property("tracking").CurrentValue = string.IsNullOrWhiteSpace(tracking) ? null : tracking.Trim();
        _db.Entry(p).Property("enviado_en").CurrentValue = fecha.HasValue ? new DateTimeOffset(fecha.Value) : DateTimeOffset.UtcNow;

        // opcional: si estaba Pagado, pásalo a Enviado
        if ((p.Estado ?? "").Equals("Pagado", StringComparison.OrdinalIgnoreCase))
            p.Estado = "Enviado";

        await _db.SaveChangesAsync();
        TempData["msg"] = "Envío registrado.";
        return RedirectToAction(nameof(Detalle), new { id });
    }

    // VMs locales
    public class PedidoListVm
    {
        public int IdPedido { get; set; }
        public string Numero { get; set; } = "-";
        public string Estado { get; set; } = "Pendiente";
        public decimal Total { get; set; }
        public DateTimeOffset CreadoEn { get; set; }
        public string Usuario { get; set; } = "-";
    }

    public class PedidoAdminVm
    {
        public int IdPedido { get; set; }
        public string Numero { get; set; } = "-";
        public string Usuario { get; set; } = "-";
        public string Estado { get; set; } = "Pendiente";
        public decimal Subtotal { get; set; }
        public decimal Impuesto { get; set; }
        public decimal Total { get; set; }
        public DateTimeOffset CreadoEn { get; set; }
        public List<PedidoItemVm> Items { get; set; } = new();

        // Pago
        public string? ReferenciaPago { get; set; }
        public DateTimeOffset? PagadoEn { get; set; }

        // Envío
        public string? Courier { get; set; }
        public string? Tracking { get; set; }
        public DateTimeOffset? EnviadoEn { get; set; }
    }

    public class PedidoItemVm
    {
        public string Producto { get; set; } = "";
        public string Sku { get; set; } = "";
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal TotalLinea { get; set; }
    }
}
