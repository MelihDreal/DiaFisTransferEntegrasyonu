using System.Threading.Tasks;
using System.Text.Json;
using DiaFisTransferEntegrasyonu.Models;

public interface IDiaService
{
    Task<string> LoginAsync();
    Task<bool> LogoutAsync(string sessionId);
    Task UpdateKasaKartlariAsync();
    Task UpdateOdemePlanlariAsync();
    Task UpdateBankaHesaplariAsync();
    Task UpdateDataAsync(int userId);
    Task<(bool Success, string Message)> NakitTahsilatFisKayit(int userId,string cariId, string cariAdi, string kasaId, string kasaAdi, decimal tutar, DateTime tarih, string doviz, string aciklama);
    Task<(bool Success, string Message)> KartTahsilatFisKayit(int userId, string cariId, string planId, string planHesapKey, decimal tutar, DateTime tarih, string doviz, string aciklama);
    Task<VirmanFisiResponse> SendVirmanFisiAsync(VirmanFisiModel model, int userId);
    Task <(bool Success, string Message)> GelenHavaleFisiKayit(int userId, string cariId, string bankahesabiKey, decimal tutar, DateTime tarih, string doviz, string aciklama);
}