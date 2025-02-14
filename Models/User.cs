using System.ComponentModel.DataAnnotations;

namespace DiaFisTransferEntegrasyonu.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string Username { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string? ApiKey { get; set; }

        [Required]
        public string? SubeKodu { get; set; }

        [Required]
        [Url]
        public string ApiUrl { get; set; }

        public int FirmaKodu { get; set; }

        public int DonemKodu { get; set; }

        public int? UstIslemTuru { get; set; }

        public bool IsAdmin { get; set; } = false; // Default değer false olarak ayarlandı

        public string? LoginUsername { get; set; }
        public string? LoginPassword { get; set; }

        public DateTime SonGuncellenmeTarihi { get; set; } = DateTime.UtcNow; // Varsayılan olarak şu anki zaman

        // İlişkiler
        public ICollection<KasaKarti> KasaKartlari { get; set; } = new List<KasaKarti>();
        public ICollection<OdemePlani> OdemePlanlari { get; set; } = new List<OdemePlani>();
        public ICollection<BankaHesabi> BankaHesaplari { get; set; } = new List<BankaHesabi>();
        public ICollection<Cari> Cariler { get; set; } = new List<Cari>();
    }

    public class DovizKuru
    {
        public string DovizKodu { get; set; }
        public decimal Kur { get; set; }
        public DateTime Tarih { get; set; }
    }

    public class KasaKarti
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public string KasaId { get; set; }
        public string KasaAdi { get; set; }
    }

    public class OdemePlani
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public string? PlanId { get; set; }
        public string? PlanAdi { get; set; }
        public string? PlanHesapKey { get; set; }
    }

    public class BankaHesabi
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public string HesapId { get; set; }
        public string HesapAdi { get; set; }
    }

    public class Cari
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public string CariId { get; set; }
        public string CariAdi { get; set; }
    }
}