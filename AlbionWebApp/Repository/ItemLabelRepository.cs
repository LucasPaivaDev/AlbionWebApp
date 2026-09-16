using AlbionWebApp.Data;

namespace AlbionWebApp.Repository
{
    public class ItemLabelRepository
    {
        public AppDbContext _appDbContext;

        public ItemLabelRepository(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<string> GetItemLabelByItemId(string itemId)
        {
          
        }


    }
}
