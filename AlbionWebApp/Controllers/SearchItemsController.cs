using AlbionWebApp.Services;
using Microsoft.AspNetCore.Mvc;
using AlbionWebApp.DTOs;

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

        [HttpPost(Name = "asyncGetItemByName")]
        public async Task<IActionResult> asyncGetItemByName([FromBody] SearchItemsFiltersDTO filtersDTO)
        {
            var json = await _searchItensService.AsyncGetItems(filtersDTO);

            return Content(json, "application/json");
        }
    }
}
