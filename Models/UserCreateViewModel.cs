using System.ComponentModel.DataAnnotations;

namespace DiaFisTransferEntegrasyonu.Models
{
    public class UserCreateViewModel
    {
        [Required(ErrorMessage = "Kullanıcı adı gereklidir.")]
        [Display(Name = "Kullanıcı Adı")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Şifre gereklidir.")]
        [DataType(DataType.Password)]
        [Display(Name = "Şifre")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Firma kodu gereklidir.")]
        [Display(Name = "Firma Kodu")]
        public string FirmaKodu { get; set; }

        [Required(ErrorMessage = "Dönem kodu gereklidir.")]
        [Display(Name = "Dönem Kodu")]
        public string DonemKodu { get; set; }

        [Required(ErrorMessage = "API Key gereklidir.")]
        [Display(Name = "API Key")]
        public string ApiKey { get; set; }

        [Required(ErrorMessage = "API URL gereklidir.")]
        [Url(ErrorMessage = "Geçerli bir URL giriniz.")]
        [Display(Name = "API URL")]
        public string ApiUrl { get; set; }

        [Required(ErrorMessage = "Şube Kodu gereklidir.")]
        [Display(Name = "Şube Kodu")]
        public string SubeKodu { get; set; }

        [Display(Name = "Üst İşem Türü(Firmada Aktif İse)")]
        public string UstIslemTuru { get; set; }
    }
}
