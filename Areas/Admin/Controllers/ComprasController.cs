
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TiendaPc.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles="Admin,Compras")]
public class ComprasController : Controller
{
    public IActionResult Index() => Content("Panel de Compras (stub).");
}
