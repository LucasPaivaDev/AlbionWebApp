using Microsoft.AspNetCore.Mvc;

namespace AlbionWebApp.Controllers
{
    [ApiController]
    [Route("/api/[controller]")]
    public class SearchItens : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
