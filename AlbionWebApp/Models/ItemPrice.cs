namespace AlbionWebApp.Models
{
    public class ItemPrice
    {
        public long Id { get; set; }

        // Vínculo com o conceito "seco" do item (nome + foto), sem tier/encantamento
        public int ItemLabelId { get; set; }
        public ItemLabel ItemLabel { get; set; }

        // item_id cru como a AODP devolve (ex: T4_BAG@1). Mantido para bater 1:1
        // com a resposta da API, facilitar upsert e debug.
        public string ItemId { get; set; }

        // Dimensões parseadas a partir do item_id
        public byte Tier { get; set; }         // 1..8
        public byte Enchantment { get; set; }  // 0..4

        // Dimensão que a AODP já entrega separada
        public int QualityId { get; set; }    // 1..5
        public Quality Quality { get; set; }

        public int CityId { get; set; }
        public City City { get; set; }

        // Preços (null = sem dado reportado)
        public int? SellPriceMin { get; set; }
        public DateTime? SellPriceMinDate { get; set; }  // data do report na AODP
        public int? SellPriceMax { get; set; }
        public int? BuyPriceMin { get; set; }
        public int? BuyPriceMax { get; set; }
        public DateTime? BuyPriceMaxDate { get; set; }   // data do report na AODP

        // Momento em que o SEU job capturou o dado (distinto da data de report)
        public DateTime CapturedAt { get; set; }
    }
}
