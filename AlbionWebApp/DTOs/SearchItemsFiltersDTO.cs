using System.ComponentModel.DataAnnotations;
using AlbionWebApp.Enums;

namespace AlbionWebApp.DTOs
{
    public class SearchItemsFiltersDTO
    {
        [Required]
        public string ItemName { get; set; }

        public string ItemTier { get; set; }

        [EnumDataType(typeof(ItemQualityEnum), ErrorMessage = "Qualidade inválida.")]
        public ItemQualityEnum? ItemQuality { get; set; }

        [EnumDataType(typeof(AlbionCityEnum), ErrorMessage = "Cidade inválida.")]
        public AlbionCityEnum? ItemCity { get; set; }
    }
}
