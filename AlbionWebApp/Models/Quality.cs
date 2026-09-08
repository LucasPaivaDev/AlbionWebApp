namespace AlbionWebApp.Models
{
    public class Quality
    {
        // Id corresponde ao valor de quality que a AODP retorna (1..5)
        public int Id { get; set; }

        // Nome legível (Normal, Good, Outstanding, Excellent, Masterpiece)
        public string Name { get; set; }
    }
}
    