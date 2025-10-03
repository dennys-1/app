using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace TiendaPc.Areas.Admin.Controllers
{
    [Area("Admin")]
    //[Authorize(Roles = "Admin")] // vuelve a activarlo si ya entras con admin
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View(); // ← ya no uses la ruta absoluta
        }
    }
}
