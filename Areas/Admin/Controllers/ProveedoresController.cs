// Areas/Admin/Controllers/ProveedoresController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiendaPc.Data;
using TiendaPc.Models;
using TiendaPc.Services;

namespace TiendaPc.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ProveedoresController : Controller
{
    private readonly AppDbContext _db;

    public ProveedoresController(AppDbContext db)
    {
        _db = db;
    }

    // DEBUG: /Admin/Proveedores/Ping
    [HttpGet]
    public async Task<IActionResult> Ping()
    {
        try
        {
            var ok = await _db.Database.CanConnectAsync();
            var count = await _db.Proveedores.CountAsync();
            return Content($"DB:{ok} Proveedores:{count}");
        }
        catch (Exception ex)
        {
            return Content("PING ERROR -> " + ex.Message + "\n" + ex.StackTrace);
        }
    }

    // GET: /Admin/Proveedores
    public async Task<IActionResult> Index(string? q, bool? activos)
    {
        try
        {
            var query = _db.Proveedores.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(p =>
                    (p.Ruc ?? "").Contains(q) ||
                    (p.RazonSocial ?? "").Contains(q) ||
                    (p.Telefono ?? "").Contains(q) ||
                    (p.Email ?? "").Contains(q));
            }

            if (activos.HasValue)
                query = query.Where(p => p.Activo == activos.Value);

            var lista = await query.OrderBy(p => p.RazonSocial).ToListAsync();
            return View(lista);
        }
        catch (Exception ex)
        {
            // Para ver el error en pantalla mientras depuras
            ViewData["IndexError"] = ex.ToString();
            return View(new List<Proveedor>());
        }
    }

    // GET: /Admin/Proveedores/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var prov = await _db.Proveedores.FirstOrDefaultAsync(p => p.IdProveedor == id);
        if (prov == null) return NotFound();
        return View(prov);
    }

    // GET: /Admin/Proveedores/Create
    public IActionResult Create() => View(new Proveedor { Activo = true });

    // POST: /Admin/Proveedores/Create
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Proveedor model)
    {
        if (!ModelState.IsValid) return View(model);

        if (!string.IsNullOrWhiteSpace(model.Ruc))
        {
            var existe = await _db.Proveedores.AnyAsync(p => p.Ruc == model.Ruc);
            if (existe)
            {
                ModelState.AddModelError(nameof(model.Ruc), "Ya existe un proveedor con ese RUC.");
                return View(model);
            }
        }

        _db.Proveedores.Add(model);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: /Admin/Proveedores/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var prov = await _db.Proveedores.FindAsync(id);
        if (prov == null) return NotFound();
        return View(prov);
    }

    // POST: /Admin/Proveedores/Edit/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Proveedor model)
    {
        if (id != model.IdProveedor) return BadRequest();
        if (!ModelState.IsValid) return View(model);

        var prov = await _db.Proveedores.FindAsync(id);
        if (prov == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(model.Ruc))
        {
            var duplicado = await _db.Proveedores.AnyAsync(p => p.IdProveedor != id && p.Ruc == model.Ruc);
            if (duplicado)
            {
                ModelState.AddModelError(nameof(model.Ruc), "Otro proveedor ya usa ese RUC.");
                return View(model);
            }
        }

        prov.Ruc = model.Ruc?.Trim();
        prov.RazonSocial = model.RazonSocial?.Trim();
        prov.Direccion = model.Direccion?.Trim();
        prov.Telefono = model.Telefono?.Trim();
        prov.Email = model.Email?.Trim();
        prov.Activo = model.Activo;

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // POST: /Admin/Proveedores/Delete/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var prov = await _db.Proveedores.FindAsync(id);
        if (prov != null)
        {
            _db.Proveedores.Remove(prov);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    // AJAX: usa ISunatService sólo aquí, inyectado por acción
    // /Admin/Proveedores/ConsultaSunat?ruc=XXXXXXXXXXX
    [HttpGet]
    public async Task<IActionResult> ConsultaSunat(string ruc, [FromServices] ISunatService sunat)
    {
        try
        {
            var info = await sunat.ConsultarPorRucAsync(ruc);
            if (info == null) return Json(new { ok = false, message = "RUC inválido o no encontrado." });
            return Json(new { ok = true, data = info });
        }
        catch (Exception ex)
        {
            return Json(new { ok = false, message = ex.Message });
        }
    }
}
