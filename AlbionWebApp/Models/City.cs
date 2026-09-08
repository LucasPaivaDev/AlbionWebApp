namespace AlbionWebApp.Models
{
    public class City
    {
        public int Id { get; set; }

        // Nome da cidade como a AODP retorna (ex: "Caerleon", "Bridgewatch")
        public string Name { get; set; }
    }
}
