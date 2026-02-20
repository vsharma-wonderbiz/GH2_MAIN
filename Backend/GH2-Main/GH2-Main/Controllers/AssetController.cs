using Microsoft.AspNetCore.Mvc;

namespace GH2_Main.Controllers
{
    [ApiController]
    public class AssetController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
