using DiaFisTransferEntegrasyonu.Models;

namespace DiaFisTransferEntegrasyonu.Services
{
    public interface ICacheService
    {
        Task<List<Cari>> GetCarilerAsync(int userId);
        Task CacheCarilerAsync(int userId);
        Task<bool> IsCacheLoadingAsync(int userId);
    }
}
