namespace DiaFisTransferEntegrasyonu.Models
{
    public class TransferViewModel
    {
        public List<KasaKarti> KasaKartlari { get; set; }
        public List<OdemePlani> OdemePlanlari { get; set; }
        public List<BankaHesabi> BankaHesaplari { get; set; }
        public List<Cari>? Cariler { get; set; }
        public List<DovizKuru> DovizKurlari { get; set; }
    }
}
