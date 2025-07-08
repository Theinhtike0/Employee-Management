namespace HR_Products.Models
{
    public class EmailSettings
    {
        public string SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public string AuthUsername { get; set; } 
        public string AuthPassword { get; set; } 
        public bool EnableSsl { get; set; }
    }
}