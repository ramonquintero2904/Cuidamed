namespace Cuidamed.Models
{
    public class UploadImagenResponse
    {
        public int ImagenesId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string UrlPublica { get; set; } = string.Empty;
        public string RutaFisica { get; set; } = string.Empty;
        public string Carpeta { get; set; } = string.Empty;
        public string Periodo { get; set; } = string.Empty;
    }
}
