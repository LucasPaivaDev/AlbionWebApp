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
            _appDbContext.ItemLabels.Add(new ItemLabel
            {
                BaseName = aodpDto.Name,
                Name = aodpDto.Name,
                URL_Image = aodpDto.ItemId,
                Category = "teste",
                Subcategory = "teste",
                CreatedAt =  DateTime.UtcNow.ToShortDateString()
            });
            await _appDbContext.SaveChangesAsync();
            return _appDbContext.ItemLabels.Last();
        }


    }
}
