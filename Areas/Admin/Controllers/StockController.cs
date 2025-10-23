using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiendaPc.Data;
using TiendaPc.Models;
using TiendaPc.Areas.Admin.Models;

namespace TiendaPc.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class StockController : Controller
{
    private readonly AppDbContext _db;
    public StockController(AppDbContext db) => _db = db;

    // ==========================
    // LISTA
    // ==========================
    public async Task<IActionResult> Index(string? q, int? almacenId)
    {
        ViewBag.Q = q;
        ViewBag.AlmacenId = almacenId;
        ViewBag.Almacenes = await _db.Almacenes.OrderBy(a => a.Nombre).ToListAsync();

        var query =
            from s in _db.Stocks
            join p in _db.Productos on s.IdProducto equals p.IdProducto
            join a in _db.Almacenes on s.IdAlmacen equals a.IdAlmacen
            select new { s, p, a };

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => x.p.Nombre.Contains(q) || x.p.Sku.Contains(q));

        if (almacenId.HasValue)
            query = query.Where(x => x.s.IdAlmacen == almacenId.Value);

        var list = await query
            .OrderBy(x => x.p.Nombre)
            .Select(x => new StockRowVm
            {
                IdProducto = x.p.IdProducto,
                Producto   = x.p.Nombre,
                Sku        = x.p.Sku,
                IdAlmacen  = x.a.IdAlmacen,
                Almacen    = x.a.Nombre,
                Cantidad   = x.s.Cantidad
            })
            .ToListAsync();

        return View(list);
    }

    // ViewModel para la grilla
    public class StockRowVm
    {
        public int IdProducto { get; set; }
        public string Producto { get; set; } = "";
        public string Sku { get; set; } = "";
        public int IdAlmacen { get; set; }
        public string Almacen { get; set; } = "";
        public int Cantidad { get; set; }
    }

    // ==========================
    // AJUSTAR (GET)
    // ==========================
    [HttpGet]
    public async Task<IActionResult> Ajustar(int idProducto, int idAlmacen)
    {
        var p = await _db.Productos.FindAsync(idProducto);
        var a = await _db.Almacenes.FindAsync(idAlmacen);
        if (p == null || a == null) return NotFound();

        var stock = await _db.Stocks.FindAsync(idProducto, idAlmacen) ?? new Stock
        {
            IdProducto = idProducto,
            IdAlmacen = idAlmacen,
            Cantidad = 0
        };

        var vm = new AjusteVm
        {
            IdProducto = idProducto,
            Producto   = p.Nombre,
            Sku        = p.Sku,
            IdAlmacen  = idAlmacen,
            Almacen    = a.Nombre,
            CantidadActual = stock.Cantidad
        };
        return View(vm);
    }

    public class AjusteVm
    {
        public int IdProducto { get; set; }
        public string Producto { get; set; } = "";
        public string Sku { get; set; } = "";
        public int IdAlmacen { get; set; }
        public string Almacen { get; set; } = "";
        public int CantidadActual { get; set; }

        public int Delta { get; set; }   // (+) ingresa, (-) sale
        public string? Motivo { get; set; }
    }

    // ==========================
    // AJUSTAR (POST)
    // ==========================
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Ajustar(AjusteVm vm)
    {
        var stock = await _db.Stocks.FindAsync(vm.IdProducto, vm.IdAlmacen);
        if (stock == null)
        {
            stock = new Stock { IdProducto = vm.IdProducto, IdAlmacen = vm.IdAlmacen, Cantidad = 0 };
            _db.Stocks.Add(stock);
        }

        stock.Cantidad += vm.Delta;

        _db.MovimientosInventario.Add(new MovimientoInventario
        {
            IdAlmacen = vm.IdAlmacen,
            IdProducto = vm.IdProducto,
            Fecha = DateTimeOffset.UtcNow,
            Tipo = vm.Delta >= 0 ? "AJUSTE_INGRESO" : "AJUSTE_SALIDA",
            Referencia = "Ajuste manual",
            Cantidad = vm.Delta,
            CostoUnitario = 0,
            Observacion = vm.Motivo
        });

        await _db.SaveChangesAsync();
        TempData["msg"] = "Ajuste realizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // ==========================
    // TRANSFERIR (GET)
    // ==========================
    [HttpGet]
    public async Task<IActionResult> Transferir()
    {
        ViewBag.Almacenes = await _db.Almacenes.OrderBy(a => a.Nombre).ToListAsync();
        ViewBag.Productos = await _db.Productos.OrderBy(p => p.Nombre).ToListAsync();

        var vm = new TransferenciaVm();
        vm.Items.Add(new ItemTransferenciaVm());   // 1 fila inicial
        return View(vm);
    }

    // ==========================
    // TRANSFERIR (POST) con UPSERT
    // ==========================
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Transferir(TransferenciaVm vm)
    {
        // Normalizar items (quitar vacíos) y consolidar duplicados
        vm.Items = vm.Items
            .Where(i => i.IdProducto > 0 && i.Cantidad > 0)
            .GroupBy(i => i.IdProducto)
            .Select(g => new ItemTransferenciaVm { IdProducto = g.Key, Cantidad = g.Sum(x => x.Cantidad) })
            .ToList();

        if (vm.IdAlmacenOrigen <= 0)
            ModelState.AddModelError(nameof(vm.IdAlmacenOrigen), "Selecciona almacén origen.");
        if (vm.IdAlmacenDestino <= 0)
            ModelState.AddModelError(nameof(vm.IdAlmacenDestino), "Selecciona almacén destino.");
        if (vm.IdAlmacenOrigen == vm.IdAlmacenDestino && vm.IdAlmacenOrigen > 0)
            ModelState.AddModelError("", "El almacén origen y destino no pueden ser iguales.");
        if (!vm.Items.Any())
            ModelState.AddModelError("", "Agrega al menos un producto con cantidad.");

        // Validación de stock en ORIGEN (sobre cantidades consolidadas)
        if (ModelState.IsValid)
        {
            var ids = vm.Items.Select(i => i.IdProducto).ToList();
            var stockOrigen = await _db.Stocks
                .Where(s => s.IdAlmacen == vm.IdAlmacenOrigen && ids.Contains(s.IdProducto))
                .ToDictionaryAsync(s => s.IdProducto, s => s.Cantidad);

            foreach (var it in vm.Items)
            {
                var disp = stockOrigen.TryGetValue(it.IdProducto, out var c) ? c : 0;
                if (it.Cantidad > disp)
                {
                    var nombre = await _db.Productos
                        .Where(p => p.IdProducto == it.IdProducto)
                        .Select(p => p.Nombre)
                        .FirstOrDefaultAsync() ?? $"ID {it.IdProducto}";
                    ModelState.AddModelError("",
                        $"Stock insuficiente en origen para \"{nombre}\". Disponible: {disp}, solicitado: {it.Cantidad}.");
                }
            }
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Almacenes = await _db.Almacenes.OrderBy(a => a.Nombre).ToListAsync();
            ViewBag.Productos = await _db.Productos.OrderBy(p => p.Nombre).ToListAsync();
            return View(vm);
        }

        // Transferencia atómica + UPSERT en Postgres
        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var ahora = DateTimeOffset.UtcNow;

            foreach (var it in vm.Items)
            {
                // 1) Descuenta en ORIGEN
                await _db.Database.ExecuteSqlInterpolatedAsync($@"
                    UPDATE stock
                    SET cantidad = cantidad - {it.Cantidad}
                    WHERE id_producto = {it.IdProducto} AND id_almacen = {vm.IdAlmacenOrigen};");

                // 2) Suma en DESTINO con UPSERT
                await _db.Database.ExecuteSqlInterpolatedAsync($@"
                    INSERT INTO stock (id_producto, id_almacen, cantidad)
                    VALUES ({it.IdProducto}, {vm.IdAlmacenDestino}, {it.Cantidad})
                    ON CONFLICT (id_producto, id_almacen)
                    DO UPDATE SET cantidad = stock.cantidad + EXCLUDED.cantidad;");

                // 3) Movimientos (persisten con SaveChanges)
                _db.MovimientosInventario.Add(new MovimientoInventario
                {
                    IdAlmacen = vm.IdAlmacenOrigen,
                    IdProducto = it.IdProducto,
                    Fecha = ahora,
                    Tipo = "TRANSFER_SALIDA",
                    Referencia = $"Transf->{vm.IdAlmacenDestino}",
                    Cantidad = -it.Cantidad,
                    CostoUnitario = 0
                });
                _db.MovimientosInventario.Add(new MovimientoInventario
                {
                    IdAlmacen = vm.IdAlmacenDestino,
                    IdProducto = it.IdProducto,
                    Fecha = ahora,
                    Tipo = "TRANSFER_INGRESO",
                    Referencia = $"Transf<-{vm.IdAlmacenOrigen}",
                    Cantidad = it.Cantidad,
                    CostoUnitario = 0
                });
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            TempData["msg"] = "Transferencia realizada con éxito.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            // saca el mensaje raíz para mostrar algo útil
            var root = ex;
            while (root.InnerException != null) root = root.InnerException;

            ModelState.AddModelError("", "No se pudo completar la transferencia: " + root.Message);
            ViewBag.Almacenes = await _db.Almacenes.OrderBy(a => a.Nombre).ToListAsync();
            ViewBag.Productos = await _db.Productos.OrderBy(p => p.Nombre).ToListAsync();
            return View(vm);
        }
    }

    // ==========================
    // AJAX: stock disponible
    // ==========================
    [HttpGet]
    public async Task<IActionResult> StockDisponible(int idAlmacen, int idProducto)
    {
        var cant = await _db.Stocks
            .Where(s => s.IdAlmacen == idAlmacen && s.IdProducto == idProducto)
            .Select(s => (int?)s.Cantidad)
            .FirstOrDefaultAsync() ?? 0;

        return Json(new { cantidad = cant });
    }
}
