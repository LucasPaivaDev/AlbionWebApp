using System.Text.Json.Serialization;

namespace AlbionWebApp.DTOs
{
    public class AodpItemPriceResponseDTO
    {
        /// item_id cru como a AODP devolve (ex: T4_BAG, T8_2H_HOLYSTAFF@1).
        [JsonPropertyName("item_id")]
        public string ItemId { get; set; } = string.Empty;

        /// Cidade a que o preço se refere (ex: "Lymhurst", "Caerleon").
        [JsonPropertyName("city")]
        public string City { get; set; } = string.Empty;

        /// Qualidade do item (1 = Normal .. 5 = Masterpiece).
        [JsonPropertyName("quality")]
        public int Quality { get; set; }

        /// Menor preço entre as sell orders ativas. 0 = sem dado reportado.
        [JsonPropertyName("sell_price_min")]
        public int SellPriceMin { get; set; }

        /// Data/hora do report da AODP para <see cref="SellPriceMin"/>. "0001-01-01T00:00:00" = sem dado.
        [JsonPropertyName("sell_price_min_date")]
        public DateTime SellPriceMinDate { get; set; }

        /// Maior preço entre as sell orders ativas. 0 = sem dado reportado.
        [JsonPropertyName("sell_price_max")]
        public int SellPriceMax { get; set; }

        /// Data/hora do report da AODP para <see cref="SellPriceMax"/>. "0001-01-01T00:00:00" = sem dado.
        [JsonPropertyName("sell_price_max_date")]
        public DateTime SellPriceMaxDate { get; set; }

        /// Menor preço entre as buy orders ativas. 0 = sem dado reportado.
        [JsonPropertyName("buy_price_min")]
        public int BuyPriceMin { get; set; }

        /// Data/hora do report da AODP para <see cref="BuyPriceMin"/>. "0001-01-01T00:00:00" = sem dado.
        [JsonPropertyName("buy_price_min_date")]
        public DateTime BuyPriceMinDate { get; set; }

        /// Maior preço entre as buy orders ativas. 0 = sem dado reportado.
        [JsonPropertyName("buy_price_max")]
        public int BuyPriceMax { get; set; }

        /// Data/hora do report da AODP para <see cref="BuyPriceMax"/>. "0001-01-01T00:00:00" = sem dado.
        [JsonPropertyName("buy_price_max_date")]
        public DateTime BuyPriceMaxDate { get; set; }
    }
}
