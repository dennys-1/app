using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiendaPc.Data;
using TiendaPc.Models;

namespace TiendaPc.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ComprasController : Controller
{
    private readonly AppDbContext _db;
    public ComprasController(AppDbContext db) => _db = db;

    // =========================
    // LISTA
    // =========================
    public async Task<IActionResult> Index(string? q, string? estado)
    {
        // JOIN explícito porque OrdenCompra no tiene navegación a Proveedor/Almacen
        var query =
            from o in _db.OrdenesCompra
            join pr in _db.Proveedores on o.IdProveedor equals pr.IdProveedor into prj
            from pr in prj.DefaultIfEmpty()
            join al in _db.Almacenes on o.IdAlmacen equals al.IdAlmacen into alj
            from al in alj.DefaultIfEmpty()
            select new { o, pr, al };

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x =>
                (x.o.NumeroOc ?? "").Contains(q) ||
                (x.pr.RazonSocial ?? "").Contains(q));

        if (!string.IsNullOrWhiteSpace(estado))
            query = query.Where(x => (x.o.Estado ?? "") == estado);

        var list = await query
            .OrderByDescending(x => x.o.FechaEmision)
            .Select(x => new OcRowVm
            {
                IdOc         = x.o.IdOc,
                Numero       = x.o.NumeroOc ?? ("OC" + x.o.IdOc.ToString("D6")),
                Proveedor    = x.pr.RazonSocial ?? "-",
                Almacen      = x.al.Nombre ?? "-",
                Total        = x.o.Total,
                Estado       = x.o.Estado ?? "Emitida",
                FechaEmision = x.o.FechaEmision
            })
            .ToListAsync();

        ViewBag.Estado = estado;
        ViewBag.Q = q;
        return View(list);
    }

    // =========================
    // CREAR OC
    // =========================
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Proveedores = await _db.Proveedores.Where(p => p.Activo).OrderBy(p => p.RazonSocial).ToListAsync();
        ViewBag.Almacenes   = await _db.Almacenes.Where(a => a.Activo).OrderBy(a => a.Nombre).ToListAsync();
        ViewBag.Productos   = await _db.Productos.Where(p => p.Activo).OrderBy(p => p.Nombre).ToListAsync();

        var vm = new CreateOcVm
        {
            FechaEntrega = DateTime.Today.AddDays(5),
            Moneda = "PEN",
            Items = new List<CreateOcItemVm> { new() }
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateOcVm vm)
    {
        vm.Items = vm.Items
            .Where(i => i.IdProducto > 0 && i.Cantidad > 0 && i.PrecioUnitario > 0)
            .ToList();

        if (vm.IdProveedor <= 0) ModelState.AddModelError(nameof(vm.IdProveedor), "Selecciona proveedor.");
        if (vm.IdAlmacen  <= 0) ModelState.AddModelError(nameof(vm.IdAlmacen),  "Selecciona almacén.");
        if (!vm.Items.Any())    ModelState.AddModelError("", "Agrega al menos un producto.");

        decimal subtotal = vm.Items.Sum(i => i.Cantidad * i.PrecioUnitario);
        decimal igv      = Math.Round(subtotal * 0.18m, 2);
        decimal total    = subtotal + igv;

        if (!ModelState.IsValid)
        {
            ViewBag.Proveedores = await _db.Proveedores.Where(p => p.Activo).OrderBy(p => p.RazonSocial).ToListAsync();
            ViewBag.Almacenes   = await _db.Almacenes.Where(a => a.Activo).OrderBy(a => a.Nombre).ToListAsync();
            ViewBag.Productos   = await _db.Productos.Where(p => p.Activo).OrderBy(p => p.Nombre).ToListAsync();
            return View(vm);
        }

        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var oc = new OrdenCompra
            {
                IdProveedor  = vm.IdProveedor,
                IdAlmacen    = vm.IdAlmacen,
                FechaEmision = DateTime.Now,
                FechaEntrega = vm.FechaEntrega,
                Moneda       = vm.Moneda ?? "PEN",
                Subtotal     = subtotal,
                Impuesto     = igv,
                Total        = total,
                Estado       = "Emitida"
            };
            _db.OrdenesCompra.Add(oc);
            await _db.SaveChangesAsync();

            foreach (var it in vm.Items)
            {
                _db.ItemsOrdenCompra.Add(new ItemOrdenCompra
                {
                    IdOc           = oc.IdOc,
                    IdProducto     = it.IdProducto,
                    Cantidad       = it.Cantidad,
                    PrecioUnitario = it.PrecioUnitario,
                    TotalLinea     = it.Cantidad * it.PrecioUnitario
                });
            }

            if (string.IsNullOrEmpty(oc.NumeroOc))
                oc.NumeroOc = $"OC{oc.IdOc:D6}";

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            TempData["msg"] = "Orden de compra creada.";
            return RedirectToAction(nameof(Detalle), new { id = oc.IdOc });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            ModelState.AddModelError("", "No se pudo crear la OC: " + ex.Message);

            ViewBag.Proveedores = await _db.Proveedores.Where(p => p.Activo).OrderBy(p => p.RazonSocial).ToListAsync();
            ViewBag.Almacenes   = await _db.Almacenes.Where(a => a.Activo).OrderBy(a => a.Nombre).ToListAsync();
            ViewBag.Productos   = await _db.Productos.Where(p => p.Activo).OrderBy(p => p.Nombre).ToListAsync();
            return View(vm);
        }
    }

    // =========================
    // DETALLE
    // =========================
    public async Task<IActionResult> Detalle(int id)
    {
        var header = await (
            from o in _db.OrdenesCompra
            join pr in _db.Proveedores on o.IdProveedor equals pr.IdProveedor into prj
            from pr in prj.DefaultIfEmpty()
            join al in _db.Almacenes on o.IdAlmacen equals al.IdAlmacen into alj
            from al in alj.DefaultIfEmpty()
            where o.IdOc == id
            select new { o, pr, al }
        ).FirstOrDefaultAsync();

        if (header == null) return NotFound();

        var items = await (
            from d in _db.ItemsOrdenCompra
            join p in _db.Productos on d.IdProducto equals p.IdProducto
            where d.IdOc == id
            select new DetalleOcItemVm
            {
                Producto      = p.Nombre,
                Sku           = p.Sku,
                Cantidad      = d.Cantidad,
                PrecioUnitario= d.PrecioUnitario,
                TotalLinea    = d.TotalLinea
            }
        ).ToListAsync();

        var vm = new DetalleOcVm
        {
            IdOc         = header.o.IdOc,
            Numero       = header.o.NumeroOc ?? $"OC{header.o.IdOc:D6}",
            Proveedor    = header.pr?.RazonSocial ?? "-",
            Almacen      = header.al?.Nombre ?? "-",
            Estado       = header.o.Estado ?? "Emitida",
            FechaEmision = header.o.FechaEmision,
            FechaEntrega = header.o.FechaEntrega,
            Subtotal     = header.o.Subtotal,
            Impuesto     = header.o.Impuesto,
            Total        = header.o.Total,
            Items        = items
        };

        return View(vm);
    }

    // =========================
    // RECIBIR (GET)
    // =========================
    [HttpGet]
    public async Task<IActionResult> Recibir(int id)
    {
        var oc = await _db.OrdenesCompra.FirstOrDefaultAsync(o => o.IdOc == id);
        if (oc == null) return NotFound();

        var items = await (
            from d in _db.ItemsOrdenCompra
            join p in _db.Productos on d.IdProducto equals p.IdProducto
            where d.IdOc == id
            select new RecibirItemVm
            {
                IdProducto = d.IdProducto,
                Producto   = p.Nombre,
                Sku        = p.Sku,
                Pendiente  = d.Cantidad - (
                    _db.ItemsRecepcionCompra
                      .Where(ir => _db.RecepcionesCompra
                                      .Where(r => r.IdOc == id)
                                      .Select(r => r.IdRecepcion)
                                      .Contains(ir.IdRecepcion)
                                   && ir.IdProducto == d.IdProducto)
                      .Select(ir => (int?)ir.CantidadRecibida).Sum() ?? 0
                ),
                Cantidad = 0
            }
        ).ToListAsync();

        var vm = new RecibirVm { IdOc = id, Items = items };
        return View(vm);
    }

    // =========================
    // RECIBIR (POST)
    // =========================
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Recibir(RecibirVm vm)
    {
        vm.Items = vm.Items.Where(i => i.IdProducto > 0 && i.Cantidad > 0).ToList();
        if (!vm.Items.Any())
            ModelState.AddModelError("", "Ingresa cantidades a recibir.");

        var oc = await _db.OrdenesCompra.FirstOrDefaultAsync(o => o.IdOc == vm.IdOc);
        if (oc == null)
            ModelState.AddModelError("", "OC no encontrada.");

        if (!ModelState.IsValid)
            return View(vm);

        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var rec = new RecepcionCompra
            {
                IdOc         = vm.IdOc,
                Fecha        = DateTime.Now,
                GuiaRemision = vm.GuiaRemision ?? "",
                Observacion  = vm.Observacion ?? ""
            };
            _db.RecepcionesCompra.Add(rec);
            await _db.SaveChangesAsync();

            foreach (var it in vm.Items)
            {
                _db.ItemsRecepcionCompra.Add(new ItemRecepcionCompra
                {
                    IdRecepcion      = rec.IdRecepcion,
                    IdProducto       = it.IdProducto,
                    CantidadRecibida = it.Cantidad
                });

                var s = await _db.Stocks
                    .FirstOrDefaultAsync(s => s.IdAlmacen == oc!.IdAlmacen && s.IdProducto == it.IdProducto);
                if (s == null)
                {
                    s = new Stock { IdAlmacen = oc!.IdAlmacen, IdProducto = it.IdProducto, Cantidad = 0 };
                    _db.Stocks.Add(s);
                }
                s.Cantidad += it.Cantidad;

                _db.MovimientosInventario.Add(new MovimientoInventario
                {
                    IdAlmacen    = oc!.IdAlmacen,
                    IdProducto   = it.IdProducto,
                    Fecha        = DateTime.Now,
                    Tipo         = "COMPRA_INGRESO",
                    Referencia   = $"OC {oc.NumeroOc ?? oc.IdOc.ToString()}",
                    Cantidad     = it.Cantidad,
                    CostoUnitario= 0,
                    Observacion  = vm.Observacion
                });
            }

            await _db.SaveChangesAsync();

            var pedidas = await _db.ItemsOrdenCompra
                .Where(d => d.IdOc == oc!.IdOc)
                .SumAsync(z => z.Cantidad);

            var recibidas = await _db.ItemsRecepcionCompra
                .Where(ir => _db.RecepcionesCompra
                                .Where(r => r.IdOc == oc!.IdOc)
                                .Select(r => r.IdRecepcion)
                                .Contains(ir.IdRecepcion))
                .SumAsync(z => z.CantidadRecibida);

            oc!.Estado = recibidas >= pedidas ? "Completada" : "Recibida Parcial";

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            TempData["msg"] = "Recepción registrada y stock actualizado.";
            return RedirectToAction(nameof(Detalle), new { id = oc!.IdOc });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            ModelState.AddModelError("", "No se pudo registrar la recepción: " + ex.Message);
            return View(vm);
        }
    }

    // =========================
    // VMs locales
    // =========================
    public class OcRowVm
    {
        public int IdOc { get; set; }
        public string Numero { get; set; } = "-";
        public string Proveedor { get; set; } = "-";
        public string Almacen { get; set; } = "-";
        public decimal Total { get; set; }
        public string Estado { get; set; } = "Emitida";
        public DateTime FechaEmision { get; set; }
    }

    public class CreateOcVm
    {
        public int IdProveedor { get; set; }
        public int IdAlmacen { get; set; }
        public DateTime FechaEntrega { get; set; }
        public string? Moneda { get; set; } = "PEN";
        public List<CreateOcItemVm> Items { get; set; } = new();
    }

    public class CreateOcItemVm
    {
        public int IdProducto { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
    }

    public class DetalleOcVm
    {
        public int IdOc { get; set; }
        public string Numero { get; set; } = "-";
        public string Proveedor { get; set; } = "-";
        public string Almacen { get; set; } = "-";
        public string Estado { get; set; } = "Emitida";
        public DateTime FechaEmision { get; set; }
        public DateTime? FechaEntrega { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Impuesto { get; set; }
        public decimal Total { get; set; }
        public List<DetalleOcItemVm> Items { get; set; } = new();
    }

    public class DetalleOcItemVm
    {
        public string Producto { get; set; } = "";
        public string Sku { get; set; } = "";
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal TotalLinea { get; set; }
    }

    public class RecibirVm
    {
        public int IdOc { get; set; }
        public string? GuiaRemision { get; set; }
        public string? Observacion { get; set; }
        public List<RecibirItemVm> Items { get; set; } = new();
    }

    public class RecibirItemVm
    {
        public int IdProducto { get; set; }
        public string Producto { get; set; } = "";
               public string Sku { get; set; } = "";
        public int Pendiente { get; set; }
        public int Cantidad { get; set; }
    }
}
