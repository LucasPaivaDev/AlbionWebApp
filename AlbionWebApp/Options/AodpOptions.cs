namespace AlbionWebApp.Options
{
    /// Configurações de acesso ao Albion Online Data Project (AODP).
    /// Mapeada a partir da seção "Aodp" do appsettings.
    public class AodpOptions
    {
        /// Nome da seção no appsettings.json.
        public const string SectionName = "Aodp";

        /// Host regional da API da AODP (ex.: https://west.albion-online-data.com).
        public string Host { get; set; } = string.Empty;

        /// Timeout, em segundos, aplicado às chamadas HTTP à AODP.
        public int TimeoutSeconds { get; set; } = 10;
    }
}
