using DiaFisTransferEntegrasyonu.Data;
using DiaFisTransferEntegrasyonu.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;


namespace DiaFisTransferEntegrasyonu.Services
{
    public class CacheService : ICacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ApplicationDbContext _context;
        private const int BATCH_SIZE = 1000;

        public CacheService(IDistributedCache cache, ApplicationDbContext context)
        {
            _cache = cache;
            _context = context;
        }

        public async Task<List<Cari>> GetCarilerAsync(int userId)
        {
            var cachedData = await _cache.GetAsync($"cariler_{userId}");
            if (cachedData != null)
            {
                return JsonSerializer.Deserialize<List<Cari>>(cachedData);
            }

            // Cache yükleniyorsa veya yoksa boş liste dön
            return new List<Cari>();
        }

        public async Task CacheCarilerAsync(int userId)
        {
            // Cache yükleniyor flag'ini set et
            await _cache.SetAsync($"carilerLoading_{userId}", BitConverter.GetBytes(true));

            try
            {
                var totalCount = await _context.Cariler.CountAsync(c => c.UserId == userId);
                var processedCount = 0;
                var cariler = new List<Cari>();

                while (processedCount < totalCount)
                {
                    var batch = await _context.Cariler
                        .Where(c => c.UserId == userId)
                        .Skip(processedCount)
                        .Take(BATCH_SIZE)
                        .ToListAsync();

                    cariler.AddRange(batch);
                    processedCount += batch.Count;

                    // Her batch sonrası cache'i güncelle
                    await _cache.SetAsync(
                        $"cariler_{userId}",
                        JsonSerializer.SerializeToUtf8Bytes(cariler),
                        new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                        });
                }
            }
            finally
            {
                // Loading flag'ini kaldır
                await _cache.RemoveAsync($"carilerLoading_{userId}");
            }
        }

        public async Task<bool> IsCacheLoadingAsync(int userId)
        {
            var loadingFlag = await _cache.GetAsync($"carilerLoading_{userId}");
            return loadingFlag != null;
        }
    }
}
