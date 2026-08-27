using System.ComponentModel.DataAnnotations;
using AlbionWebApp.Enums;

namespace AlbionWebApp.DTOs
{
    public class SearchItemsFiltersDTO
    {
        [Required (ErrorMessage = "Nome do item é obrigatório.")]
        public string ItemName { get; set; }

        [Required]
        [Range(1, 8, ErrorMessage = "Tier inválido.")]
        public string ItemTier { get; set; }

        [EnumDataType(typeof(ItemQualityEnum), ErrorMessage = "Qualidade inválida.")]
        public ItemQualityEnum? ItemQuality { get; set; }

        [EnumDataType(typeof(AlbionCityEnum), ErrorMessage = "Cidade inválida.")]
        public AlbionCityEnum? ItemBuyCity { get; set; }

        [EnumDataType(typeof(AlbionCityEnum), ErrorMessage = "Cidade inválida.")]
        public AlbionCityEnum? ItemCraftCity { get; set; }

        [EnumDataType(typeof(AlbionCityEnum), ErrorMessage = "Cidade inválida.")]
        public AlbionCityEnum? ItemSellCity { get; set; }
    }
}
