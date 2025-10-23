using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiendaPc.Data;
using TiendaPc.Models;

namespace TiendaPc.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ProductosController : Controller
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    public ProductosController(AppDbContext db, IWebHostEnvironment env)
    { _db = db; _env = env; }

    public async Task<IActionResult> Index(string? q, int page = 1, int pageSize = 15)
    {
        var query = _db.Productos.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p => p.Nombre.Contains(q) || p.Sku.Contains(q));

        var total = await query.CountAsync();
        var list = await query
            .OrderByDescending(p => p.IdProducto)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Total = total; ViewBag.Page = page; ViewBag.PageSize = pageSize; ViewBag.Q = q;
        return View(list);
    }
    

public async Task<IActionResult> Create()
    {
        
        try
        {
            ViewBag.Marcas = await _db.Marcas.OrderBy(x => x.Nombre).ToListAsync();
            ViewBag.Categorias = await _db.Categorias.OrderBy(x => x.Nombre).ToListAsync();
            return View(new Producto { Activo = true });
        }
        catch (Exception ex)
        {
            return Problem("Create (GET) explotó: " + ex.Message);
        }
    }


[HttpPost, ValidateAntiForgeryToken]
public async Task<IActionResult> Create(Producto model, IFormFile? imagen)
{
        // Validaciones básicas
    if (string.IsNullOrWhiteSpace(model.Especificaciones))
    model.Especificaciones = "{}";   // JSON válido

    if (string.IsNullOrWhiteSpace(model.Sku))
            ModelState.AddModelError(nameof(model.Sku), "El SKU es obligatorio.");
    if (string.IsNullOrWhiteSpace(model.Nombre))
        ModelState.AddModelError(nameof(model.Nombre), "El nombre es obligatorio.");
    if (model.IdMarca <= 0)
        ModelState.AddModelError(nameof(model.IdMarca), "Seleccione una marca.");
    if (model.IdCategoria <= 0)
        ModelState.AddModelError(nameof(model.IdCategoria), "Seleccione una categoría.");

    // SKU duplicado (muy común)
    if (!string.IsNullOrWhiteSpace(model.Sku) &&
        await _db.Productos.AnyAsync(p => p.Sku == model.Sku))
    {
        ModelState.AddModelError(nameof(model.Sku), "Ya existe un producto con este SKU.");
    }

    if (!ModelState.IsValid)
    {
        ViewBag.Marcas     = await _db.Marcas.OrderBy(x => x.Nombre).ToListAsync();
        ViewBag.Categorias = await _db.Categorias.OrderBy(x => x.Nombre).ToListAsync();
        return View(model);
    }

    try
    {
        _db.Productos.Add(model);
        await _db.SaveChangesAsync();  // ← si la BD exige NOT NULL en algo, explotaba aquí

        // Imagen opcional
        if (imagen != null && imagen.Length > 0)
        {
            var wwwroot = _env.WebRootPath;
            if (string.IsNullOrEmpty(wwwroot))
                wwwroot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            var folder = Path.Combine(wwwroot, "img", "products");
            Directory.CreateDirectory(folder);

            var fileName = $"p_{model.IdProducto}_{Guid.NewGuid():N}{Path.GetExtension(imagen.FileName)}";
            var full = Path.Combine(folder, fileName);

            using (var fs = System.IO.File.Create(full))
                await imagen.CopyToAsync(fs);

            _db.ImagenesProducto.Add(new ImagenProducto
            {
                IdProducto = model.IdProducto,
                Url = $"/img/products/{fileName}",
                Principal = true
            });
            await _db.SaveChangesAsync();
        }

        TempData["msg"] = "Producto creado.";
        return RedirectToAction(nameof(Index));
    }
    catch (DbUpdateException ex)
    {
        // Muestra el mensaje real de BD
        ModelState.AddModelError(string.Empty, ex.InnerException?.Message ?? ex.Message);
        ViewBag.Marcas     = await _db.Marcas.OrderBy(x => x.Nombre).ToListAsync();
        ViewBag.Categorias = await _db.Categorias.OrderBy(x => x.Nombre).ToListAsync();
        return View(model);
    }
    catch (Exception ex)
    {
        // Mensaje explícito para depurar
        return Problem("Create(POST) falló: " + ex.Message + (ex.InnerException != null ? " / " + ex.InnerException.Message : ""));
    }
}


    public async Task<IActionResult> Edit(int id)
    {
        var p = await _db.Productos.FindAsync(id);
        if (p == null) return NotFound();
        ViewBag.Marcas = await _db.Marcas.ToListAsync();
        ViewBag.Categorias = await _db.Categorias.ToListAsync();
        return View(p);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Producto model, IFormFile? imagen)
    {
        if (string.IsNullOrWhiteSpace(model.Especificaciones))
    model.Especificaciones = "{}";   // JSON válido

        if (!ModelState.IsValid)
        {
            ViewBag.Marcas = await _db.Marcas.ToListAsync();
            ViewBag.Categorias = await _db.Categorias.ToListAsync();
            return View(model);
        }

        _db.Productos.Update(model);
        await _db.SaveChangesAsync();

        if (imagen != null && imagen.Length > 0)
        {
            var folder = Path.Combine(_env.WebRootPath, "img", "products");
            Directory.CreateDirectory(folder);
            var fileName = $"p_{model.IdProducto}_{Guid.NewGuid():N}{Path.GetExtension(imagen.FileName)}";
            var full = Path.Combine(folder, fileName);
            using var fs = System.IO.File.Create(full);
            await imagen.CopyToAsync(fs);

            // si no hay principal, marca como principal
            var tienePrincipal = await _db.ImagenesProducto.AnyAsync(i => i.IdProducto == model.IdProducto && i.Principal);
            _db.ImagenesProducto.Add(new ImagenProducto
            {
                IdProducto = model.IdProducto,
                Url = $"/img/products/{fileName}",
                Principal = !tienePrincipal
            });
            await _db.SaveChangesAsync();
        }

        TempData["msg"] = "Producto actualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await _db.Productos.FindAsync(id);
        if (p == null) return NotFound();
        _db.Productos.Remove(p);
        await _db.SaveChangesAsync();
        TempData["msg"] = "Producto eliminado.";
        return RedirectToAction(nameof(Index));
    }
}
