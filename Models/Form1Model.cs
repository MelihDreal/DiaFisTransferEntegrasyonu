using System.ComponentModel.DataAnnotations;

namespace DiaFisTransferEntegrasyonu.Models
{
    public class Form1Model
    {
        [Required]
        public string KasaId { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public string CariId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Tutar sıfırdan büyük olmalıdır.")]
        public decimal Amount { get; set; }

        [Required]
        public string Currency { get; set; }

        public string Description { get; set; }
    }
}
