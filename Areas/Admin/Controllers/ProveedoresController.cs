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
    private readonly ISunatService _sunat;

    public ProveedoresController(AppDbContext db, ISunatService sunat)
    {
        _db = db;
        _sunat = sunat;
    }

    // GET: /Admin/Proveedores
    // Filtros: q (ruc/razón/telefono/email) y activos (true/false/null)
    public async Task<IActionResult> Index(string? q, bool? activos)
    {
        var query = _db.Proveedores.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(p =>
                p.Ruc.Contains(q) ||
                p.RazonSocial.Contains(q) ||
                (p.Telefono ?? "").Contains(q) ||
                (p.Email ?? "").Contains(q));
        }

        if (activos.HasValue)
            query = query.Where(p => p.Activo == activos.Value);

        var list = await query
            .OrderBy(p => p.RazonSocial)
            .ToListAsync();

        ViewBag.Q = q;
        ViewBag.Activos = activos;

        return View(list);
    }

    // GET: /Admin/Proveedores/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var p = await _db.Proveedores.FindAsync(id);
        if (p == null) return NotFound();
        return View(p);
    }

    // GET: /Admin/Proveedores/Create
    public IActionResult Create()
    {
        return View(new Proveedor { Activo = true });
    }

    // POST: /Admin/Proveedores/Create
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Proveedor model)
    {
        // validación básica de RUC único
        if (!string.IsNullOrWhiteSpace(model.Ruc))
        {
            bool yaExiste = await _db.Proveedores.AnyAsync(p => p.Ruc == model.Ruc);
            if (yaExiste)
                ModelState.AddModelError(nameof(model.Ruc), "Ya existe un proveedor con ese RUC.");
        }

        if (!ModelState.IsValid)
            return View(model);

        _db.Proveedores.Add(model);
        await _db.SaveChangesAsync();
        TempData["msg"] = "Proveedor creado.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Admin/Proveedores/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var p = await _db.Proveedores.FindAsync(id);
        if (p == null) return NotFound();
        return View(p);
    }

    // POST: /Admin/Proveedores/Edit/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Proveedor model)
    {
        if (id != model.IdProveedor) return BadRequest();

        // RUC único (excluyendo el mismo id)
        if (!string.IsNullOrWhiteSpace(model.Ruc))
        {
            bool yaExiste = await _db.Proveedores
                .AnyAsync(p => p.IdProveedor != id && p.Ruc == model.Ruc);
            if (yaExiste)
                ModelState.AddModelError(nameof(model.Ruc), "Ya existe un proveedor con ese RUC.");
        }

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            _db.Proveedores.Update(model);
            await _db.SaveChangesAsync();
            TempData["msg"] = "Proveedor actualizado.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _db.Proveedores.AnyAsync(p => p.IdProveedor == id))
                return NotFound();
            throw;
        }
    }

    // POST: /Admin/Proveedores/Delete/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await _db.Proveedores.FindAsync(id);
        if (p == null) return NotFound();

        _db.Proveedores.Remove(p);
        await _db.SaveChangesAsync();
        TempData["msg"] = "Proveedor eliminado.";
        return RedirectToAction(nameof(Index));
    }

    // ========= API auxiliar =========
    // GET: /Admin/Proveedores/BuscarRuc?ruc=XXXXXXXXXXX
    [HttpGet]
    public async Task<IActionResult> BuscarRuc(string ruc)
    {
        if (string.IsNullOrWhiteSpace(ruc) || ruc.Length < 8)
            return BadRequest(new { error = "RUC/DNI inválido." });

        var data = await _sunat.BuscarRucAsync(ruc);
        if (data == null)
            return NotFound(new { error = "No encontrado." });

        return Json(new
        {
            ruc = data.Ruc,
            razonSocial = data.RazonSocial,
            direccion = data.Direccion,
            estado = data.Estado
        });
    }
}
