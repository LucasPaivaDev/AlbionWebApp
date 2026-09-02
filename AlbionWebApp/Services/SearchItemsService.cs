using AlbionWebApp.DTOs;
using AlbionWebApp.Options;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace AlbionWebApp.Services
{
    public class SearchItemsService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly HttpClient _httpClient;
        private readonly AodpOptions _aodpOptions;
       

        public SearchItemsService(IHttpClientFactory httpClientFactory, IOptions<AodpOptions> aodpOptions)
        {
            _httpClientFactory = httpClientFactory;
            _aodpOptions = aodpOptions.Value;
            _httpClient = _httpClientFactory.CreateClient();
        }



        public async Task<string> AsyncGetItems(SearchItemsFiltersDTO filtersDTO)
        {
            var response = await _httpClient.GetAsync(
                $"{_aodpOptions.Host}/api/v2/stats/prices/{filtersDTO.ItemName}.json?locations={filtersDTO.ItemBuyCity?.ToString()}");

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
    }
}
