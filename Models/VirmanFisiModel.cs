namespace DiaFisTransferEntegrasyonu.Models
{
    public class VirmanFisiModel
    {
        public int FirmaKodu { get; set; }
        public int DonemKodu { get; set; }
        public string SubeKodu { get; set; }
        public string Aciklama1 { get; set; }
        public string Aciklama2 { get; set; }
        public string Aciklama3 { get; set; }
        public string BelgeNo { get; set; }
        public string FisNo { get; set; }
        public List<VirmanFisiKalemModel> Kalemler { get; set; }
        public DateTime Saat { get; set; }
        public DateTime Tarih { get; set; }
        public string Turu { get; set; }
        public int UstIslemTuru { get; set; }
        public string OzelKod1 { get; set; }
    }

    public class VirmanFisiResponse
    {
        public string? Code { get; set; }
        public string? Message { get; set; }
        public VirmanFisiResult? Result { get; set; }
    }

    public class VirmanFisiResult
    {
        public string? FisNo { get; set; }
        public int? Id { get; set; }
    }

    public class VirmanFisiKalemModel
    {
        public string CariKartKodu { get; set; }
        public string DovizAdi { get; set; }
        public string Aciklama { get; set; }
        public decimal DovizKuru { get; set; }
        public decimal KurFarkiAlacak { get; set; }
        public decimal KurFarkiBorc { get; set; }
        public string MakbuzNo { get; set; }
        public string CariKart { get; set; }
        public string CariUnvan { get; set; }
        public int _key_scf_carikart { get; set; }
        public DateTime Vade { get; set; }
        public decimal? Alacak { get; set; }
        public decimal? Borc { get; set; }
        public string BankaHesapKodu { get; set; }

    }
}
