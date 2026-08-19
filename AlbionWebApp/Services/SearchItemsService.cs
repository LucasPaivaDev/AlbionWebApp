namespace AlbionWebApp.Services
{
    public class SearchItemsService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public SearchItemsService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<string> asyncGetItems(string searchTerm)
        {
            var httpClient = _httpClientFactory.CreateClient();

            var response = await httpClient.GetAsync(
                $"https://west.albion-online-data.com/api/v2/stats/prices/{searchTerm}.json?locations=Caerleon");

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
    }
}
