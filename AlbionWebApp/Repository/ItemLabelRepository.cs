using AlbionWebApp.Data;
using AlbionWebApp.DTOs;
using AlbionWebApp.Models;

namespace AlbionWebApp.Repository
{
    public class ItemLabelRepository
    {
        public AppDbContext _appDbContext;

        public ItemLabelRepository(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<ItemLabel> CreateItemLabelByAodpDto(AodpItemPriceResponseDTO aodpDto)
        {
            var itemLabel = new ItemLabel
            {
                BaseName = aodpDto.Name,
                Name = aodpDto.Name,
                URL_Image = aodpDto.ItemId,
                Category = "teste",
                Subcategory = "teste",
                CreatedAt = DateTime.UtcNow.ToShortDateString()
            };

            _appDbContext.ItemLabels.Add(itemLabel);
            await _appDbContext.SaveChangesAsync();
            return itemLabel;
        }

        public async Task<ItemPrice> CreateItemPriceByAodpDto(AodpItemPriceResponseDTO aodpDto, int itemLabelId)
        {
            var itemPrice = new ItemPrice
            {
                ItemLabelId = itemLabelId,
                ItemId = aodpDto.ItemId,
                QualityId = (int)aodpDto.Quality,
                CityId = (int)aodpDto.City,
                SellPriceMin = aodpDto.SellPriceMin > 0 ? aodpDto.SellPriceMin : null,
                SellPriceMinDate = ToUtcOrNull(aodpDto.SellPriceMinDate),
                SellPriceMax = aodpDto.SellPriceMax > 0 ? aodpDto.SellPriceMax : null,
                BuyPriceMin = aodpDto.BuyPriceMin > 0 ? aodpDto.BuyPriceMin : null,
                BuyPriceMax = aodpDto.BuyPriceMax > 0 ? aodpDto.BuyPriceMax : null,
                BuyPriceMaxDate = ToUtcOrNull(aodpDto.BuyPriceMaxDate),
                CapturedAt = DateTime.UtcNow
            };

            _appDbContext.ItemPrices.Add(itemPrice);
            await _appDbContext.SaveChangesAsync();
            return itemPrice;
        }

        private static DateTime? ToUtcOrNull(DateTime value)
        {
            if (value <= DateTime.MinValue)
            {
                return null;
            }

            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }



    }
}
