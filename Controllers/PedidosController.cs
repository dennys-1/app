using System.Security.Claims;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using TiendaPc.Data;
using TiendaPc.Models;

using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;

namespace TiendaPc.Controllers;

[Authorize]
public class PedidosController : Controller
{
    private readonly AppDbContext _db;
    public PedidosController(AppDbContext db) => _db = db;

    // GET /Pedidos/MisPedidos
    [HttpGet]
public async Task<IActionResult> MisPedidos(int page = 1, int pageSize = 10)
{
    try
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 50) pageSize = 10;

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var query = _db.Pedidos
            .Where(p => p.IdUsuario == userId)
            .OrderByDescending(p => p.CreadoEn);

        var total = await query.CountAsync();
        var pedidos = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PedidoListItemVm
            {
                IdPedido = p.IdPedido,
                NumeroPedido = p.NumeroPedido ?? ("P" + p.IdPedido.ToString("D6")),
                Estado = p.Estado ?? "Pendiente",
                Total = p.Total,
                CreadoEn = p.CreadoEn
            })
            .ToListAsync();

        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.Total = total;
        return View(pedidos);
    }
    catch (Exception ex)
    {
        return Problem("MisPedidos falló: " + ex.Message);
    }
}

    // GET /Pedidos/Detalle/{id}
    [HttpGet]
    public async Task<IActionResult> Detalle(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var pedido = await _db.Pedidos
            .FirstOrDefaultAsync(p => p.IdPedido == id && p.IdUsuario == userId);

        if (pedido == null) return NotFound();

        var items = await (
            from d in _db.ItemsPedido
            join pr in _db.Productos on d.IdProducto equals pr.IdProducto
            where d.IdPedido == id
            select new PedidoDetalleItemVm
            {
                Producto = pr.Nombre,
                Sku = pr.Sku,
                Cantidad = d.Cantidad,
                PrecioUnitario = d.PrecioUnitario,
                TotalLinea = d.TotalLinea
            }
        ).ToListAsync();

        var vm = new PedidoDetalleVm
        {
            IdPedido = pedido.IdPedido,
            NumeroPedido = pedido.NumeroPedido ?? ("P" + pedido.IdPedido.ToString("D6")),
            Estado = pedido.Estado ?? "Pendiente",
            Subtotal = pedido.Subtotal,
            Impuesto = pedido.Impuesto,
            Total = pedido.Total,
            CreadoEn = pedido.CreadoEn,
            Items = items
        };

        return View(vm);
    }
    [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> RepetirCompra(int id)
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    var pedido = await _db.Pedidos
        .FirstOrDefaultAsync(p => p.IdPedido == id && p.IdUsuario == userId);
    if (pedido == null) return NotFound();

    // crea / obtiene carrito del usuario
    var carrito = await _db.Carritos.FirstOrDefaultAsync(c => c.IdUsuario == userId);
    if (carrito == null)
    {
        carrito = new Carrito { IdUsuario = userId, CreadoEn = DateTimeOffset.UtcNow };
        _db.Carritos.Add(carrito);
        await _db.SaveChangesAsync();
    }

    // traer items del pedido
    var items = await _db.ItemsPedido
        .Where(i => i.IdPedido == id)
        .Select(i => new { i.IdProducto, i.Cantidad, i.PrecioUnitario })
        .ToListAsync();

    foreach (var it in items)
    {
        // si ya existe en el carrito, suma cantidades
        var existente = await _db.ItemsCarrito
            .FirstOrDefaultAsync(x => x.IdCarrito == carrito.IdCarrito && x.IdProducto == it.IdProducto);

        if (existente == null)
        {
            _db.ItemsCarrito.Add(new ItemCarrito
            {
                IdCarrito = carrito.IdCarrito,
                IdProducto = it.IdProducto,
                Cantidad = it.Cantidad,
                PrecioUnitario = it.PrecioUnitario
            });
        }
        else
        {
            existente.Cantidad += it.Cantidad;
            existente.PrecioUnitario = it.PrecioUnitario; // última referencia
        }
    }

    await _db.SaveChangesAsync();

    TempData["msg"] = "Se cargaron los artículos del pedido a tu carrito.";
    return RedirectToAction("Index", "Carrito");
}


    // GET /Pedidos/Descargar/{id}
    [HttpGet]
    public async Task<IActionResult> Descargar(int id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var pedido = await _db.Pedidos
                .FirstOrDefaultAsync(p => p.IdPedido == id && p.IdUsuario == userId);

            if (pedido == null)
                return NotFound("Pedido no encontrado o no pertenece al usuario.");

            var items = await (
                from d in _db.ItemsPedido
                join pr in _db.Productos on d.IdProducto equals pr.IdProducto
                where d.IdPedido == id
                select new
                {
                    Nombre = pr.Nombre,
                    Sku = pr.Sku,
                    d.Cantidad,
                    d.PrecioUnitario,
                    d.TotalLinea
                }
            ).ToListAsync();

            var numero = pedido.NumeroPedido ?? $"P{pedido.IdPedido:D6}";
            var fecha = pedido.CreadoEn.LocalDateTime;

            // Logo opcional
            var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "logo.jpeg");
            var hayLogo = System.IO.File.Exists(logoPath);

            byte[] pdf = Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);

                    // Header
                    page.Header().Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"Comprobante de compra #{numero}")
                                    .SemiBold().FontSize(18);
                            c.Item().Text($"Estado: {pedido.Estado ?? "Pendiente"}");
                            c.Item().Text($"Fecha: {fecha:dd/MM/yyyy HH:mm}");
                            c.Item().Text($"Usuario: {userId}");
                        });

                        if (hayLogo)
                            r.ConstantItem(140).AlignRight().Image(logoPath);
                    });

                    // Content
                    page.Content().Column(col =>
                    {
                        col.Spacing(10);

                        // Tabla
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(4); // Producto
                                c.RelativeColumn(2); // SKU
                                c.RelativeColumn(1); // Cant.
                                c.RelativeColumn(2); // P.Unit
                                c.RelativeColumn(2); // Total
                            });

                            // Header
                            t.Header(h =>
                            {
                                h.Cell().Element(HeaderCell).Text("Producto");
                                h.Cell().Element(HeaderCell).Text("SKU");
                                h.Cell().Element(HeaderCell).AlignRight().Text("Cant.");
                                h.Cell().Element(HeaderCell).AlignRight().Text("P. Unit.");
                                h.Cell().Element(HeaderCell).AlignRight().Text("Total");

                                static IContainer HeaderCell(IContainer c) =>
                                    c.PaddingVertical(4)
                                     .DefaultTextStyle(x => x.SemiBold())
                                     .BorderBottom(1).BorderColor("#DDD");
                            });

                            // Filas
                            foreach (var it in items)
                            {
                                t.Cell().Text(it.Nombre ?? "-");
                                t.Cell().Text(it.Sku ?? "-");
                                t.Cell().AlignRight().Text($"{it.Cantidad:0}");
                                t.Cell().AlignRight().Text($"S/ {it.PrecioUnitario:N2}");
                                t.Cell().AlignRight().Text($"S/ {it.TotalLinea:N2}");
                            }
                        });

                        // Totales
                        col.Item().AlignRight().Column(tot =>
                        {
                            tot.Spacing(4);
                            tot.Item().Text($"Subtotal: S/ {pedido.Subtotal:N2}");
                            tot.Item().Text($"IGV: S/ {pedido.Impuesto:N2}");
                            tot.Item().Text($"Total: S/ {pedido.Total:N2}").SemiBold();
                        });

                        col.Item().Text("¡Gracias por su compra!").AlignCenter();
                    });

                    // Footer
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("TiendaPc • ").Light();
                        x.Span($"{DateTime.Now:dd/MM/yyyy HH:mm}");
                    });
                });
            }).GeneratePdf();

            return File(pdf, "application/pdf", $"Pedido_{numero}.pdf");
        }
        catch (Exception ex)
        {
            return Problem($"No se pudo generar el PDF: {ex.Message}");
        }
    }
}