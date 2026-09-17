using AlbionWebApp.DTOs;
using AlbionWebApp.Options;
using AlbionWebApp.Repository;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace AlbionWebApp.Services
{
    public class SearchItemsService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly HttpClient _httpClient;
        private readonly AodpOptions _aodpOptions;
        private readonly ItemLabelRepository _itemLabelRepository;
       
        public SearchItemsService(
            IHttpClientFactory httpClientFactory,
            IOptions<AodpOptions> aodpOptions,
            ItemLabelRepository itemLabelRepository
        )
        {
            _httpClientFactory = httpClientFactory;
            _aodpOptions = aodpOptions.Value;
            _httpClient = _httpClientFactory.CreateClient();
            _itemLabelRepository = itemLabelRepository;
        }



        public async Task<List<AodpItemPriceResponseDTO>> AsyncGetItems(SearchItemsFiltersDTO filtersDTO)
        {
            var response = await _httpClient.GetAsync(
                $"{_aodpOptions.Host}/api/v2/stats/prices/{filtersDTO.ItemName}.json?locations={filtersDTO.ItemBuyCity?.ToString()}");

            response.EnsureSuccessStatusCode();

            var responseDtoList =  await AsyncFormatAODPResponse(response);
            foreach (AodpItemPriceResponseDTO dto in responseDtoList)
            {
                await _itemLabelRepository.CreateItemLabelByAodpDto(dto);
            }



            return responseDtoList;
        }

        public async Task<List<AodpItemPriceResponseDTO>> AsyncFormatAODPResponse(HttpResponseMessage response)
        {
            var json = await response.Content.ReadAsStringAsync();

            //Equivalente ao ToArray() dentro do DTO em PHP 
            var items = JsonSerializer.Deserialize<List<AodpItemPriceResponseDTO>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return items ?? new List<AodpItemPriceResponseDTO>();
        }
    }
}
