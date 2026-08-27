using AlbionWebApp.DTOs;

namespace AlbionWebApp.Services
{
    public class SearchItemsService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public SearchItemsService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<string> asyncGetItems(SearchItemsFiltersDTO filtersDTO)
        {
            var httpClient = _httpClientFactory.CreateClient();

            var response = await httpClient.GetAsync(
                $"https://west.albion-online-data.com/api/v2/stats/prices/{filtersDTO.ItemName}.json?locations={filtersDTO.ItemBuyCity}");

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
    }
}
