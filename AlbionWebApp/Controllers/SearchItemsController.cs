using AlbionWebApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace AlbionWebApp.Controllers
{
    [ApiController]
    [Route("/api/[controller]")]
    public class SearchItemsController : Controller
    {
        private SearchItemsService _searchItensService;

        public SearchItemsController(SearchItemsService searchItensService)
        {
            _searchItensService = searchItensService;
        }

        [HttpGet(Name = "asyncGetItemByName")]
        public async Task<IActionResult> asyncGetItemByName()
        {
            var json = await _searchItensService.asyncGetItems("T4_BAG");

            return Content(json, "application/json");
        }
    }
}
