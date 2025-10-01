using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiendaPc.Data;

namespace TiendaPc.Controllers;

public class CatalogoController : Controller
{
    private readonly AppDbContext _db;
    public CatalogoController(AppDbContext db) { _db = db; }

    // /Catalogo/Index?cat=1&marca=2&q=rtx&min=500&max=2000&orden=precioAsc&pag=1&t=12
    public async Task<IActionResult> Index(
        int? cat, int? marca, string? q, decimal? min, decimal? max,
        string? orden = "relevancia", int pag = 1, int t = 12)
    {
        pag = Math.Max(1, pag);
        t   = Math.Clamp(t, 12, 48);

        // Query base
        var query = _db.Productos.AsNoTracking().Where(p => p.Activo);

        if (cat.HasValue)   query = query.Where(p => p.IdCategoria == cat);
        if (marca.HasValue) query = query.Where(p => p.IdMarca == marca);
        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(p =>
                EF.Functions.ILike(p.Nombre, $"%{q}%") ||
                EF.Functions.ILike(p.Sku, $"%{q}%"));
        }
        if (min.HasValue) query = query.Where(p => p.Precio >= min.Value);
        if (max.HasValue) query = query.Where(p => p.Precio <= max.Value);

        // Orden
        query = orden switch
        {
            "precioAsc"  => query.OrderBy(p => p.Precio).ThenBy(p => p.Nombre),
            "precioDesc" => query.OrderByDescending(p => p.Precio).ThenBy(p => p.Nombre),
            "nombre"     => query.OrderBy(p => p.Nombre),
            _            => query.OrderBy(p => p.IdProducto) // "relevancia" simple
        };

        // Conteo total y paginación
        var total = await query.CountAsync();
        var items = await query.Skip((pag - 1) * t).Take(t).ToListAsync();

        // Datos para filtros (sidebar)
        var categorias = await _db.Categorias
            .OrderBy(c => c.Nombre)
            .Select(c => new FiltroItemVm { Id = c.IdCategoria, Nombre = c.Nombre })
            .ToListAsync();

        var marcas = await _db.Marcas
            .OrderBy(m => m.Nombre)
            .Select(m => new FiltroItemVm { Id = m.IdMarca, Nombre = m.Nombre })
            .ToListAsync();

        var vm = new CatalogoVm
        {
            Items = items,
            Categorias = categorias,
            Marcas = marcas,
            // Estado de filtros actual
            Q = q, Cat = cat, Marca = marca, Min = min, Max = max, Orden = orden ?? "relevancia",
            Pag = pag, T = t, Total = total
        };
        return View(vm);
    }
}

public class CatalogoVm
{
    public List<TiendaPc.Models.Producto> Items { get; set; } = new();
    public List<FiltroItemVm> Categorias { get; set; } = new();
    public List<FiltroItemVm> Marcas { get; set; } = new();

    // Estado filtros
    public string? Q { get; set; }
    public int? Cat { get; set; }
    public int? Marca { get; set; }
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
    public string Orden { get; set; } = "relevancia";
    public int Pag { get; set; } = 1;
    public int T { get; set; } = 12;
    public int Total { get; set; } = 0;

    public int TotalPaginas => (int)Math.Ceiling((double)Math.Max(0, Total) / Math.Max(1, T));
}

public record FiltroItemVm
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
}
