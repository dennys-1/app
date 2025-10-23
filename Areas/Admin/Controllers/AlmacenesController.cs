using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiendaPc.Data;
using TiendaPc.Models;

namespace TiendaPc.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class AlmacenesController : Controller
{
    private readonly AppDbContext _db;
    public AlmacenesController(AppDbContext db) => _db = db;

    // GET: /Admin/Almacenes
    public async Task<IActionResult> Index()
    {
        var list = await _db.Almacenes
            .OrderBy(a => a.Nombre)
            .ToListAsync();
        return View(list);
    }

    // GET: /Admin/Almacenes/Create
    public IActionResult Create()
        => View(new Almacen { Activo = true });

    // POST: /Admin/Almacenes/Create
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Almacen model)
    {
        if (!ModelState.IsValid)
            return View(model);

        _db.Almacenes.Add(model);
        await _db.SaveChangesAsync();

        TempData["msg"] = "Almacén creado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Admin/Almacenes/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var alm = await _db.Almacenes.FindAsync(id);
        if (alm == null) return NotFound();
        return View(alm);
    }

    // POST: /Admin/Almacenes/Edit
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Almacen model)
    {
        if (!ModelState.IsValid)
            return View(model);

        _db.Almacenes.Update(model);
        await _db.SaveChangesAsync();

        TempData["msg"] = "Almacén actualizado.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Admin/Almacenes/Delete/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var alm = await _db.Almacenes.FindAsync(id);
        if (alm == null) return NotFound();

        _db.Almacenes.Remove(alm);
        await _db.SaveChangesAsync();

        TempData["msg"] = "Almacén eliminado.";
        return RedirectToAction(nameof(Index));
    }
}
