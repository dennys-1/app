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

    public async Task<IActionResult> Index()
        => View(await _db.Almacenes.OrderBy(x=>x.IdAlmacen).ToListAsync());

    public IActionResult Create() => View(new Almacen{Activo = true});

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Almacen m)
    {
        if (!ModelState.IsValid) return View(m);
        _db.Almacenes.Add(m); await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var a = await _db.Almacenes.FindAsync(id);
        if (a == null) return NotFound();
        return View(a);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Almacen m)
    {
        if (!ModelState.IsValid) return View(m);
        _db.Almacenes.Update(m); await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var a = await _db.Almacenes.FindAsync(id);
        if (a == null) return NotFound();
        _db.Almacenes.Remove(a); await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
