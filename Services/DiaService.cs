using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DiaFisTransferEntegrasyonu.Models;
using Microsoft.AspNetCore.Http;
using DiaFisTransferEntegrasyonu.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Net.Http.Headers;

namespace DiaFisTransferEntegrasyonu.Services
{
    public class DiaService : IDiaService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<DiaService> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IDovizKuruService _dovizKuruService;

        public DiaService(ILogger<DiaService> logger, ApplicationDbContext context, IHttpContextAccessor httpContextAccessor, IDovizKuruService dovizKuruService)
        {
            _httpClient = new HttpClient();
            _logger = logger;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _dovizKuruService = dovizKuruService;

        }

        private async Task<List<DovizKuru>> GetDovizKurlariAsync(string sessionId)
        {
            try
            {
                var diaInfo = GetDiaInfo();
                var requestBody = new
                {
                    sis_doviz_kuru_listele_sontarih = new
                    {
                        session_id = sessionId,
                        firma_kodu = diaInfo.FirmaKodu,
                        donem_kodu = diaInfo.DonemKodu,
                        filters = "",
                        sorts = "",
                        @params = new
                        {
                            sontarih = DateTime.Now.ToString("yyyy-MM-dd")
                        },
                        limit = 0,
                        offset = 0
                    }
                };

                var jsonResponse = await SendRequestAsync<JObject>(diaInfo.ApiUrl + "/api/v3/sis/json", requestBody);

                if (jsonResponse["data"] is JArray dataArray)
                {
                    return dataArray.Select(item => new DovizKuru
                    {
                        DovizKodu = item["adi"].ToString(),
                        Kur = decimal.Parse(item["kur4"].ToString()),
                        Tarih = DateTime.UtcNow
                    }).ToList();
                }

                return new List<DovizKuru>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Döviz kurları alınırken HTTP isteği hatası oluştu");
                throw;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Döviz kurları parse edilirken JSON hatası oluştu");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Döviz kurları alınırken beklenmeyen bir hata oluştu");
                throw;
            }
        }

        private DiaInfo GetDiaInfo()
        {
            try
            {
                var diaInfoJson = _httpContextAccessor.HttpContext.Request.Cookies["DiaInfo"];
                if (string.IsNullOrEmpty(diaInfoJson))
                {
                    throw new Exception("Dia bilgileri bulunamadı.");
                }

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DateFormatHandling = DateFormatHandling.IsoDateFormat
                };

                return JsonConvert.DeserializeObject<DiaInfo>(diaInfoJson, settings);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Cari kartlar parse edilirken JSON hatası oluştu");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetDiaInfo metodunda hata oluştu");
                throw;
            }
        }

        public async Task<string> LoginAsync()
        {
            try
            {
                var diaInfo = GetDiaInfo();
                var loginRequest = new
                {
                    login = new
                    {
                        username = diaInfo.Username,
                        password = diaInfo.Password,
                        disconnect_same_user = "True",
                        @params = new
                        {
                            apikey = diaInfo.ApiKey
                        }
                    }
                };

                var response = await SendRequestAsync<LoginResponse>(diaInfo.ApiUrl + "/api/v3/sis/json", loginRequest);
                return response?.code == "200" ? response.msg : null;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Login işlemi sırasında HTTP isteği hatası oluştu");
                throw;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Cari kartlar parse edilirken JSON hatası oluştu");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login işlemi sırasında beklenmeyen bir hata oluştu");
                throw;
            }
        }

        public async Task<bool> LogoutAsync(string sessionId)
        {
            try
            {
                var diaInfo = GetDiaInfo();
                var logoutRequest = new
                {
                    logout = new
                    {
                        session_id = sessionId
                    }
                };

                await SendRequestAsync<object>(diaInfo.ApiUrl + "/api/v3/sis/json", logoutRequest);
                return true;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Logout işlemi sırasında HTTP isteği hatası oluştu");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout işlemi sırasında beklenmeyen bir hata oluştu");
                return false;
            }
        }

        public async Task UpdateKasaKartlariAsync()
        {
            string sessionId = null;
            try
            {
                sessionId = await LoginAsync();
                if (string.IsNullOrEmpty(sessionId))
                {
                    throw new Exception("Login işlemi başarısız oldu.");
                }

                var kasaKartlari = ParseKasaKartlari(await GetKasaKartlariAsync(sessionId));

                var diaInfo = GetDiaInfo();
                var user = await _context.Set<User>().FirstOrDefaultAsync(u => u.Username == diaInfo.Username);
                if (user == null)
                {
                    throw new Exception("Kullanıcı bulunamadı.");
                }

                var existingKasaKartlari = await _context.Set<KasaKarti>().Where(k => k.UserId == user.Id).ToListAsync();
                _context.RemoveRange(existingKasaKartlari);

                foreach (var kasaKarti in kasaKartlari)
                {
                    kasaKarti.UserId = user.Id;
                    _context.Add(kasaKarti);
                }

                user.SonGuncellenmeTarihi = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Kasa kartları güncellenirken veritabanı hatası oluştu");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kasa kartları güncellenirken beklenmeyen bir hata oluştu");
                throw;
            }
            finally
            {
                if (!string.IsNullOrEmpty(sessionId))
                {
                    await LogoutAsync(sessionId);
                }
            }
        }

        private async Task<JObject> GetKasaKartlariAsync(string sessionId)
        {
            try
            {
                var diaInfo = GetDiaInfo();
                var requestBody = new
                {
                    scf_kasakart_listele = new
                    {
                        session_id = sessionId,
                        firma_kodu = diaInfo.FirmaKodu,
                        donem_kodu = diaInfo.DonemKodu,
                        filters = "",
                        sorts = "",
                        @params = "",
                        limit = 100,
                        offset = 0
                    }
                };

                var jsonResponse = await SendRequestAsync<JObject>(diaInfo.ApiUrl + "/api/v3/scf/json", requestBody);
                return jsonResponse;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Kasa kartları alınırken HTTP isteği hatası oluştu");
                throw;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Cari kartlar parse edilirken JSON hatası oluştu");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kasa kartları alınırken beklenmeyen bir hata oluştu");
                throw;
            }
        }


        private List<KasaKarti> ParseKasaKartlari(JObject responseData)
        {
            try
            {
                var kasaKartlari = new List<KasaKarti>();

                var resultArray = responseData["result"] as JArray;

                if (resultArray != null)
                {
                    foreach (var item in resultArray)
                    {
                        var kasaId = item["_key"]?.ToString();
                        var kasaAdi = item["adi"]?.ToString();

                        if (!string.IsNullOrEmpty(kasaId) && !string.IsNullOrEmpty(kasaAdi))
                        {
                            kasaKartlari.Add(new KasaKarti
                            {
                                KasaId = kasaId,
                                KasaAdi = kasaAdi
                            });
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("Beklenen 'result' dizisi bulunamadı veya geçerli bir dizi değil");
                }

                return kasaKartlari;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Cari kartlar parse edilirken JSON hatası oluştu");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kasa kartları parse edilirken beklenmeyen bir hata oluştu");
                throw;
            }
        }

        public async Task UpdateOdemePlanlariAsync()
        {
            string sessionId = null;
            try
            {
                sessionId = await LoginAsync();
                if (string.IsNullOrEmpty(sessionId))
                {
                    throw new Exception("Login işlemi başarısız oldu.");
                }

                var odemePlanlariData = await GetOdemePlanlariAsync(sessionId);
                var odemePlanlari = ParseOdemePlanlari(odemePlanlariData);

                var diaInfo = GetDiaInfo();
                var user = await _context.Set<User>().FirstOrDefaultAsync(u => u.Username == diaInfo.Username);
                if (user == null)
                {
                    throw new Exception("Kullanıcı bulunamadı.");
                }

                var existingOdemePlanlari = await _context.Set<OdemePlani>().Where(o => o.UserId == user.Id).ToListAsync();
                _context.RemoveRange(existingOdemePlanlari);

                foreach (var odemePlani in odemePlanlari)
                {
                    odemePlani.UserId = user.Id;
                    _context.Add(odemePlani);
                }

                user.SonGuncellenmeTarihi = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Ödeme planları güncellenirken veritabanı hatası oluştu");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ödeme planları güncellenirken beklenmeyen bir hata oluştu");
                throw;
            }
            finally
            {
                if (!string.IsNullOrEmpty(sessionId))
                {
                    await LogoutAsync(sessionId);
                }
            }
        }

        private async Task<JObject> GetOdemePlanlariAsync(string sessionId)
        {
            try
            {
                var diaInfo = GetDiaInfo();
                var requestBody = new
                {
                    scf_banka_odeme_plani_listele = new
                    {
                        session_id = sessionId,
                        firma_kodu = diaInfo.FirmaKodu,
                        donem_kodu = diaInfo.DonemKodu,
                        filters = "",
                        sorts = "",
                        @params = "",
                        limit = 100,
                        offset = 0
                    }
                };

                var jsonResponse = await SendRequestAsync<JObject>(diaInfo.ApiUrl + "/api/v3/scf/json", requestBody);
                return jsonResponse;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Ödeme planları alınırken HTTP isteği hatası oluştu");
                throw;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Cari kartlar parse edilirken JSON hatası oluştu");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ödeme planları alınırken beklenmeyen bir hata oluştu");
                throw;
            }
        }

        private List<OdemePlani> ParseOdemePlanlari(JObject responseData)
        {
            try
            {
                var odemePlanlari = new List<OdemePlani>();

                var resultArray = responseData["result"] as JArray;

                if (resultArray != null && resultArray.Count > 0)
                {
                    foreach (var item in resultArray)
                    {
                        string planId = item["_key"]?.ToString();
                        string planAdi = item["aciklama"]?.ToString();
                        string planHesapKey = item["_key_bcs_bankahesabi"]?.ToString();

                        if (!string.IsNullOrEmpty(planId) && !string.IsNullOrEmpty(planAdi) && !string.IsNullOrEmpty(planHesapKey))
                        {
                            odemePlanlari.Add(new OdemePlani
                            {
                                PlanId = planId,
                                PlanAdi = planAdi,
                                PlanHesapKey = planHesapKey
                            });
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("Beklenen 'result' dizisi bulunamadı veya geçerli bir dizi değil");
                }

                return odemePlanlari;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Cari kartlar parse edilirken JSON hatası oluştu");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ödeme planları parse edilirken beklenmeyen bir hata oluştu");
                throw;
            }
        }

        public async Task UpdateBankaHesaplariAsync()
        {
            string sessionId = null;
            try
            {
                sessionId = await LoginAsync();
                if (string.IsNullOrEmpty(sessionId))
                {
                    throw new Exception("Login işlemi başarısız oldu.");
                }

                var bankaHesaplariData = await GetBankaHesaplariAsync(sessionId);
                var bankaHesaplari = ParseBankaHesaplari(bankaHesaplariData);

                var diaInfo = GetDiaInfo();
                var user = await _context.Set<User>().FirstOrDefaultAsync(u => u.Username == diaInfo.Username);
                if (user == null)
                {
                    throw new Exception("Kullanıcı bulunamadı.");
                }

                var existingBankaHesaplari = await _context.Set<BankaHesabi>().Where(b => b.UserId == user.Id).ToListAsync();
                _context.RemoveRange(existingBankaHesaplari);

                foreach (var bankaHesabi in bankaHesaplari)
                {
                    bankaHesabi.UserId = user.Id;
                    _context.Add(bankaHesabi);
                }

                user.SonGuncellenmeTarihi = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Banka hesapları güncellenirken veritabanı hatası oluştu");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Banka hesapları güncellenirken beklenmeyen bir hata oluştu");
                throw;
            }
            finally
            {
                if (!string.IsNullOrEmpty(sessionId))
                {
                    await LogoutAsync(sessionId);
                }
            }
        }

        private async Task<JObject> GetBankaHesaplariAsync(string sessionId)
        {
            try
            {
                var diaInfo = GetDiaInfo();
                var requestBody = new
                {
                    bcs_bankahesabi_listele = new
                    {
                        session_id = sessionId,
                        firma_kodu = diaInfo.FirmaKodu,
                        donem_kodu = diaInfo.DonemKodu,
                        filters = "",
                        sorts = "",
                        @params = "",
                        limit = 100,
                        offset = 0
                    }
                };

                var jsonResponse = await SendRequestAsync<JObject>(diaInfo.ApiUrl + "/api/v3/bcs/json", requestBody);
                return jsonResponse;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Banka hesapları alınırken HTTP isteği hatası oluştu");
                throw;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Cari kartlar parse edilirken JSON hatası oluştu");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Banka hesapları alınırken beklenmeyen bir hata oluştu");
                throw;
            }
        }

        private List<BankaHesabi> ParseBankaHesaplari(JObject responseData)
        {
            try
            {
                var bankaHesaplari = new List<BankaHesabi>();

                var resultArray = responseData["result"] as JArray;

                if (resultArray != null)
                {
                    foreach (var item in resultArray)
                    {
                        var hesapId = item["_key"]?.ToString();
                        var hesapAdi = item["hesapadi"]?.ToString();
                        var isActive = item["durum"]?.ToString();

                        if (!string.IsNullOrEmpty(hesapId) && !string.IsNullOrEmpty(hesapAdi))
                        {
                            if(isActive != "P")
                            {
                                bankaHesaplari.Add(new BankaHesabi
                                {
                                    HesapId = hesapId,
                                    HesapAdi = hesapAdi
                                });
                            }
                            
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("Beklenen 'result' dizisi bulunamadı veya geçerli bir dizi değil");
                }

                return bankaHesaplari;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Cari kartlar parse edilirken JSON hatası oluştu");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Banka hesapları parse edilirken beklenmeyen bir hata oluştu");
                throw;
            }
        }

        private async Task<JObject> GetCariKartlarAsync(string sessionId)
        {
            try
            {
                var diaInfo = GetDiaInfo();
                var requestBody = new
                {
                    scf_carikart_listele = new
                    {
                        session_id = sessionId,
                        firma_kodu = diaInfo.FirmaKodu,
                        donem_kodu = diaInfo.DonemKodu,
                        filters = "",
                        sorts = new[] { new { field = "carikartkodu", sorttype = "DESC" } },
                        @params = new { irsaliyeleriDahilEt = "False" },
                        limit = 100,
                        offset = 0
                    }
                };

                var jsonResponse = await SendRequestAsync<JObject>(diaInfo.ApiUrl + "/api/v3/scf/json", requestBody);
                return jsonResponse;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Cari kartlar alınırken HTTP isteği hatası oluştu");
                throw;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Cari kartlar yanıtı JSON çözümlemesi sırasında hata oluştu");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cari kartlar alınırken beklenmeyen bir hata oluştu");
                throw;
            }
        }

        private List<Cari> ParseCariKartlar(JObject responseData)
        {
            try
            {
                var cariKartlar = new List<Cari>();

                var resultArray = responseData["result"] as JArray;
                if (resultArray != null)
                {
                    foreach (var item in resultArray)
                    {
                        string key = item["_key"]?.ToString();
                        string kodveunvan = item["kodveunvan"]?.ToString();

                        if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(kodveunvan))
                        {
                            string[] parts = kodveunvan.Split('|');
                            string cariAdi = parts.Length > 1 ? parts[1].Trim() : kodveunvan;

                            cariKartlar.Add(new Cari
                            {
                                CariId = key,
                                CariAdi = cariAdi
                            });
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("Beklenen 'result' dizisi bulunamadı veya geçerli bir dizi değil");
                }

                return cariKartlar;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Cari kartlar parse edilirken JSON hatası oluştu");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cari kartlar parse edilirken beklenmeyen bir hata oluştu");
                throw;
            }
        }

        public async Task UpdateDataAsync(int userId)
        {
            string sessionId = null;
            try
            {
                // Kullanıcıyı ilgili koleksiyonlarla birlikte yükleyin
                var user = await _context.Users
                    .Include(u => u.KasaKartlari)
                    .Include(u => u.OdemePlanlari)
                    .Include(u => u.BankaHesaplari)
                    .Include(u => u.Cariler)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    throw new ArgumentException("Kullanıcı bulunamadı.");
                }

                sessionId = await LoginAsync();
                if (string.IsNullOrEmpty(sessionId))
                {
                    throw new Exception("Login işlemi başarısız oldu.");
                }

                var kasaKartlari = ParseKasaKartlari(await GetKasaKartlariAsync(sessionId));
                var odemePlanlari = ParseOdemePlanlari(await GetOdemePlanlariAsync(sessionId));
                var bankaHesaplari = ParseBankaHesaplari(await GetBankaHesaplariAsync(sessionId));
                var cariKartlar = ParseCariKartlar(await GetCariKartlarAsync(sessionId));

                // Mevcut verileri silin
                _context.KasaKartlari.RemoveRange(user.KasaKartlari);
                _context.OdemePlanlari.RemoveRange(user.OdemePlanlari);
                _context.BankaHesaplari.RemoveRange(user.BankaHesaplari);
                _context.Cariler.RemoveRange(user.Cariler);

                // Yeni verileri ekleyin
                user.KasaKartlari = kasaKartlari.Select(k => new KasaKarti
                {
                    KasaId = k.KasaId,
                    KasaAdi = k.KasaAdi,
                    UserId = userId
                }).ToList();

                user.OdemePlanlari = odemePlanlari.Select(o => new OdemePlani
                {
                    PlanId = o.PlanId,
                    PlanAdi = o.PlanAdi,
                    PlanHesapKey = o.PlanHesapKey,
                    UserId = userId
                }).ToList();

                user.BankaHesaplari = bankaHesaplari.Select(b => new BankaHesabi
                {
                    HesapId = b.HesapId,
                    HesapAdi = b.HesapAdi,
                    UserId = userId
                }).ToList();

                user.Cariler = cariKartlar.Select(c => new Cari
                {
                    CariId = c.CariId,
                    CariAdi = c.CariAdi,
                    UserId = userId
                }).ToList();

                user.SonGuncellenmeTarihi = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Veri güncellenirken veritabanı hatası oluştu");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Veri güncellenirken beklenmeyen bir hata oluştu");
                throw;
            }
            finally
            {
                if (!string.IsNullOrEmpty(sessionId))
                {
                    await LogoutAsync(sessionId);
                }
            }
        }

        private string GenerateRandomNumber()
        {
            Random random = new Random();
            return random.Next(10000000, 99999999).ToString();
        }

        public async Task<(bool Success, string Message)> NakitTahsilatFisKayit(int userId, string cariId, string cariAdi, string kasaId, string kasaAdi, decimal tutar, DateTime tarih, string doviz, string aciklama)
        {
            string sessionId = null;
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                    throw new ArgumentException("Kullanıcı bulunamadı.");

                if (!int.TryParse(cariId, out int cariIdInt))
                    throw new ArgumentException("Geçersiz cari ID formatı.");

                if (!int.TryParse(kasaId, out int kasaIdInt))
                    throw new ArgumentException("Geçersiz kasa ID formatı.");

                
                string randomNumber = GenerateRandomNumber();
                string wsAdres = user.ApiUrl + "/api/v3/scf/json";

                // Döviz kurlarını al
                var dovizKurlari = await _dovizKuruService.GetDovizKurlariAsync(user.ApiKey, user.ApiUrl, user.FirmaKodu, user.DonemKodu);

                // İlgili döviz kurunu bul
                var dovizKuru = dovizKurlari.FirstOrDefault(k => k.DovizKodu == doviz)
                    ?? throw new ArgumentException($"Geçersiz döviz kodu: {doviz}");

                // Türkiye zaman dilimini al
                TimeZoneInfo turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");

                // Şu anki Türkiye saatini al
                DateTime nowInTurkey = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, turkeyTimeZone);

                // Gelen tarih ile şu anki Türkiye saatini birleştir
                DateTime combinedDateTime = tarih.Date.Add(nowInTurkey.TimeOfDay);

                var kartData = new
                {
                    _key_bcs_bankahesabi = 0,
                    _key_muh_hesapkarti = 0,
                    _key_muh_masrafmerkezi = 0,
                    _key_ote_poskarti = 0,
                    _key_ote_rezervasyonkarti = 0,
                    _key_ote_rezervasyonkarti_misafir = 0,
                    _key_prj_proje = 0,
                    _key_scf_banka_odeme_plani = 0,
                    _key_sis_ust_islem_turu = 564595,
                    _key_scf_carikart = new { _key = cariIdInt.ToString() },
                    _key_scf_kasa = new { _key = kasaIdInt.ToString() },
                    _key_scf_kasavirman = 0,
                    _key_scf_malzeme_baglantisi = 0,
                    _key_scf_odeme_plani = 0,
                    _key_scf_satiselemani = 0,
                    _key_sis_doviz = new { adi = doviz },
                    _key_sis_doviz_cari = new { adi = doviz },
                    _key_sis_sube = new { subekodu = user.SubeKodu.ToString() },
                    aciklama = aciklama,
                    aciklama2 = "",
                    aciklama3 = "",
                    alacak = "0.000000",
                    alacak_cari = "0.000000",
                    borc = tutar.ToString("F6"),
                    borc_cari = tutar.ToString("F6"),
                    dovizkuru = dovizKuru.Kur.ToString("F4"),
                    kurfarkialacak = "0.000000",
                    kurfarkiborc = "0.000000",
                    belgeno = $"WS{randomNumber}",
                    fisno = $"WS{randomNumber}",
                    odemeturu = "E",
                    saat = combinedDateTime.ToString("HH:mm:ss"),
                    tarih = tarih.ToString("yyyy-MM-dd"),
                    turu = "TAH",
                    _key_sis_ozelkod = 0,
                    _key_sis_seviyekodu = 0
                };

                sessionId = await LoginAsync();

                var requestData = new
                {
                    scf_kasaislemleri_ekle = new
                    {
                        session_id = sessionId,
                        firma_kodu = user.FirmaKodu,
                        donem_kodu = user.DonemKodu,
                        kart = kartData
                    }
                };

                string wsInput = JsonConvert.SerializeObject(requestData, Formatting.Indented);

                _logger.LogInformation($"Sending request to API: {wsInput}");

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var content = new StringContent(wsInput, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(wsAdres, content);
                    var responseString = await response.Content.ReadAsStringAsync();

                    _logger.LogInformation($"Raw API Response: {responseString}");

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException($"API error: {response.StatusCode}, Response: {responseString}");
                    }

                    // JSON yanıtını parse et
                    var jsonResponse = JObject.Parse(responseString);

                    if (jsonResponse["code"]?.Value<string>() == "200")
                    {
                        string successMessage = jsonResponse["msg"]?.ToString() ?? "İşlem başarılı";
                        string key = jsonResponse["key"]?.ToString() ?? "";

                        // Unicode escape karakterlerini çözümle
                        successMessage = JsonConvert.DeserializeObject<string>($"\"{successMessage}\"");

                        _logger.LogInformation($"API çağrısı başarılı: {successMessage}. Anahtar: {key}");
                        return (true, successMessage);
                    }
                    else
                    {
                        string errorMessage = jsonResponse["msg"]?.ToString() ?? "Bilinmeyen hata";

                        // Unicode escape karakterlerini çözümle
                        errorMessage = JsonConvert.DeserializeObject<string>($"\"{errorMessage}\"");

                        var errors = jsonResponse["errors"]?.ToObject<List<string>>() ?? new List<string>();
                        var warnings = jsonResponse["warnings"]?.ToObject<List<string>>() ?? new List<string>();

                        // Hata ve uyarı mesajlarındaki Unicode escape karakterlerini çözümle
                        errors = errors.Select(e => JsonConvert.DeserializeObject<string>($"\"{e}\"")).ToList();
                        warnings = warnings.Select(w => JsonConvert.DeserializeObject<string>($"\"{w}\"")).ToList();

                        string fullErrorMessage = $"{errorMessage}";
                        if (errors.Any())
                        {
                            fullErrorMessage += $" Hatalar: {string.Join(", ", errors)}";
                        }
                        if (warnings.Any())
                        {
                            fullErrorMessage += $" Uyarılar: {string.Join(", ", warnings)}";
                        }

                        _logger.LogError($"API çağrısı başarısız: {fullErrorMessage}");
                        return (false, fullErrorMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nakit tahsilat fişi kaydedilirken hata oluştu");
                throw;
            }
            finally
            {
                if (!string.IsNullOrEmpty(sessionId))
                {
                    await LogoutAsync(sessionId);
                }
            }
        }

        public async Task<(bool Success, string Message)> KartTahsilatFisKayit(int userId, string cariId, string planId, string planHesapKey,  decimal tutar, DateTime tarih, string doviz, string aciklama)
        {
            string sessionId = null;
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                    throw new ArgumentException("Kullanıcı bulunamadı.");

                if (!int.TryParse(cariId, out int cariIdInt))
                    throw new ArgumentException("Geçersiz cari ID formatı.");



                
                string randomNumber = GenerateRandomNumber();
                string wsAdres = user.ApiUrl + "/api/v3/scf/json";

                // Döviz kurlarını al
                var dovizKurlari = await _dovizKuruService.GetDovizKurlariAsync(user.ApiKey, user.ApiUrl, user.FirmaKodu, user.DonemKodu);

                // İlgili döviz kurunu bul
                var dovizKuru = dovizKurlari.FirstOrDefault(k => k.DovizKodu == doviz)
                    ?? throw new ArgumentException($"Geçersiz döviz kodu: {doviz}");

                // Türkiye zaman dilimini al
                TimeZoneInfo turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");

                // Şu anki Türkiye saatini al
                DateTime nowInTurkey = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, turkeyTimeZone);

                // Gelen tarih ile şu anki Türkiye saatini birleştir
                DateTime combinedDateTime = tarih.Date.Add(nowInTurkey.TimeOfDay);

                var kartData = new
                {
                    _key_scf_malzeme_baglantisi = 0,
                    _key_scf_odeme_plani = 0,
                    _key_sis_ozelkod = 0,
                    _key_sis_seviyekodu = 0,
                    _key_sis_ust_islem_turu = 564595,
                    _key_sis_sube = new { subekodu = user.SubeKodu.ToString() },
                    aciklama1 = "KREDİ KARTI",
                    aciklama2 = "",
                    aciklama3 = "",
                    belgeno = $"WS{randomNumber}",
                    fisno = $"WS{randomNumber}",
                    m_kalemler = new[]
                    {
                new
                {
                    _key_muh_masrafmerkezi = 0,
                    _key_ote_rezervasyonkarti = 0,
                    _key_prj_proje = 0,
                    _key_scf_banka_odeme_plani = planId,
                    _key_scf_carikart = new { _key = cariIdInt.ToString() },
                    _key_bcs_bankahesabi = new { _key = planHesapKey },
                    _key_scf_odeme_plani = 0,
                    _key_scf_satiselemani = 0,
                    _key_shy_servisformu = 0,
                    _key_sis_doviz = new { adi = doviz },
                    _key_sis_ozelkod = 0,
                    aciklama = aciklama,
                    alacak = tutar.ToString("F4"),
                    dovizkuru = dovizKuru.Kur.ToString("F4"),
                    kurfarkialacak = "0.00",
                    kurfarkiborc = "0.00",
                    makbuzno = "",
                    carikart = "",
                    cariunvan = "",
                    vade = tarih.ToString("yyyy-MM-dd")
                }
            },
                    saat = combinedDateTime.ToString("HH:mm:ss"),
                    tarih = tarih.ToString("yyyy-MM-dd"),
                    turu = "KK"
                };

                sessionId = await LoginAsync();

                var requestData = new
                {
                    scf_carihesap_fisi_ekle = new
                    {
                        session_id = sessionId,
                        firma_kodu = user.FirmaKodu,
                        donem_kodu = user.DonemKodu,
                        kart = kartData
                    }
                };

                string wsInput = JsonConvert.SerializeObject(requestData, Formatting.Indented);

                _logger.LogInformation($"Sending request to API: {wsInput}");

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var content = new StringContent(wsInput, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(wsAdres, content);
                    var responseString = await response.Content.ReadAsStringAsync();

                    _logger.LogInformation($"Raw API Response: {responseString}");

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException($"API error: {response.StatusCode}, Response: {responseString}");
                    }

                    // JSON yanıtını parse et
                    var jsonResponse = JObject.Parse(responseString);

                    if (jsonResponse["code"]?.Value<string>() == "200")
                    {
                        string successMessage = jsonResponse["msg"]?.ToString() ?? "İşlem başarılı";
                        string key = jsonResponse["key"]?.ToString() ?? "";

                        // Unicode escape karakterlerini çözümle
                        successMessage = JsonConvert.DeserializeObject<string>($"\"{successMessage}\"");

                        _logger.LogInformation($"API çağrısı başarılı: {successMessage}. Anahtar: {key}");
                        return (true, successMessage);
                    }
                    else
                    {
                        string errorMessage = jsonResponse["msg"]?.ToString() ?? "Bilinmeyen hata";

                        // Unicode escape karakterlerini çözümle
                        errorMessage = JsonConvert.DeserializeObject<string>($"\"{errorMessage}\"");

                        var errors = jsonResponse["errors"]?.ToObject<List<string>>() ?? new List<string>();
                        var warnings = jsonResponse["warnings"]?.ToObject<List<string>>() ?? new List<string>();

                        // Hata ve uyarı mesajlarındaki Unicode escape karakterlerini çözümle
                        errors = errors.Select(e => JsonConvert.DeserializeObject<string>($"\"{e}\"")).ToList();
                        warnings = warnings.Select(w => JsonConvert.DeserializeObject<string>($"\"{w}\"")).ToList();

                        string fullErrorMessage = $"{errorMessage}";
                        if (errors.Any())
                        {
                            fullErrorMessage += $" Hatalar: {string.Join(", ", errors)}";
                        }
                        if (warnings.Any())
                        {
                            fullErrorMessage += $" Uyarılar: {string.Join(", ", warnings)}";
                        }

                        _logger.LogError($"API çağrısı başarısız: {fullErrorMessage}");
                        return (false, fullErrorMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kredi kartı tahsilat fişi kaydedilirken hata oluştu");
                throw;
            }
            finally
            {
                if (!string.IsNullOrEmpty(sessionId))
                {
                    await LogoutAsync(sessionId);
                }
            }
        }

        public async Task<VirmanFisiResponse> SendVirmanFisiAsync(VirmanFisiModel model, int userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                throw new ArgumentException("Kullanıcı bulunamadı.");

            string randomNumber = GenerateRandomNumber();
            string wsAdres = user.ApiUrl + "/api/v3/scf/json";


            // Döviz kurlarını al
            var dovizKurlari = await _dovizKuruService.GetDovizKurlariAsync(user.ApiKey, user.ApiUrl, user.FirmaKodu, user.DonemKodu);

            // Türkiye zaman dilimini al
            TimeZoneInfo turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");

            // Şu anki Türkiye saatini al
            DateTime nowInTurkey = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, turkeyTimeZone);

            // Gelen tarih ile şu anki Türkiye saatini birleştir
            DateTime combinedDateTime = model.Tarih.Date.Add(nowInTurkey.TimeOfDay);

            try
            {
                string sessionId = await LoginAsync();
                if (string.IsNullOrEmpty(sessionId))
                {
                    throw new Exception("Login işlemi başarısız oldu.");
                }

                var kalemler = model.Kalemler
                    .Where(k => k._key_scf_carikart != 0 && (k.Alacak > 0 || k.Borc > 0))
                    .Select(k =>
                    {
                    // İlgili döviz kurunu bul
                    var dovizKuru = dovizKurlari.FirstOrDefault(d => d.DovizKodu == k.DovizAdi)
                        ?? throw new ArgumentException($"Geçersiz döviz kodu: {k.DovizKuru}");

                        return new
                        {
                            _key_bcs_bankahesabi = 0,
                            _key_muh_masrafmerkezi = 0,
                            _key_ote_rezervasyonkarti = 0,
                            _key_scf_carikart = new { _key = k._key_scf_carikart.ToString() },
                            _key_prj_proje = 0,
                            _key_scf_banka_odeme_plani = 0,
                            _key_scf_odeme_plani = 0,
                            _key_scf_satiselemani = 0,
                            _key_shy_servisformu = 0,
                            _key_sis_doviz = new { adi = dovizKuru.DovizKodu.ToString() },
                            _key_sis_ozelkod = 0,
                            aciklama = k.Aciklama,
                            alacak = k.Alacak > 0 ? k.Alacak.ToString() : (string)null,
                            borc = k.Borc > 0 ? k.Borc.ToString() : (string)null,


                            dovizkuru = dovizKuru.Kur.ToString("F4"),
                            kurfarkialacak = "0.00",
                            kurfarkiborc = "0.00",
                            vade = k.Vade.ToString("yyyy-MM-dd")
                        };
                }).ToList();

                object kart;
                if (user.UstIslemTuru.HasValue && user.UstIslemTuru.Value != 0)
                {
                    kart = new
                    {
                        _key_scf_malzeme_baglantisi = 0,
                        _key_scf_odeme_plani = 0,
                        _key_sis_ozelkod = 0,
                        _key_sis_seviyekodu = 0,
                        _key_sis_ust_islem_turu = user.UstIslemTuru.Value,
                        _key_sis_sube = new { subekodu = user.SubeKodu.ToString() },
                        aciklama1 = model.Aciklama1,
                        aciklama2 = model.Aciklama2,
                        aciklama3 = model.Aciklama3,
                        belgeno = "WS" + randomNumber,
                        fisno = "WS" + randomNumber,
                        m_kalemler = kalemler,
                        saat = combinedDateTime.ToString("HH:mm:ss"),
                        tarih = model.Tarih.ToString("yyyy-MM-dd"),
                        turu = "VF"
                    };
                }
                else
                {
                    kart = new
                    {
                        _key_scf_malzeme_baglantisi = 0,
                        _key_scf_odeme_plani = 0,
                        _key_sis_ozelkod = 0,
                        _key_sis_seviyekodu = 0,
                        _key_sis_sube = new { subekodu = user.SubeKodu.ToString() },
                        aciklama1 = model.Aciklama1,
                        aciklama2 = model.Aciklama2,
                        aciklama3 = model.Aciklama3,
                        belgeno = "WS" + randomNumber,
                        fisno = "WS" + randomNumber,
                        m_kalemler = kalemler,
                        saat = model.Saat.ToString("HH:mm:ss"),
                        tarih = model.Tarih.ToString("yyyy-MM-dd"),
                        turu = "VF"
                    };
                }

                var requestData = new
                {
                    scf_carihesap_fisi_ekle = new
                    {
                        session_id = sessionId,
                        firma_kodu = user.FirmaKodu,
                        donem_kodu = user.DonemKodu,
                        kart = kart
                    }
                };

                var response = await SendRequestAsync<VirmanFisiResponse>(user.ApiUrl + "/api/v3/scf/json", requestData);

                //await LogoutAsync(sessionId);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cari hesap fişi gönderilirken hata oluştu");
                return new VirmanFisiResponse { Code = "500", Message = "Bir hata oluştu: " + ex.Message };
            }
        }

        public async Task<(bool Success, string Message)> GelenHavaleFisiKayit(int userId, string cariId, string bankahesabiKey, decimal tutar, DateTime tarih, string doviz, string aciklama)
        {
            string sessionId = null;
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                    throw new ArgumentException("Kullanıcı bulunamadı.");

                if (!int.TryParse(cariId, out int cariIdInt))
                    throw new ArgumentException("Geçersiz cari ID formatı.");

                


               
                string randomNumber = GenerateRandomNumber();
                string wsAdres = user.ApiUrl + "/api/v3/bcs/json";

                // Döviz kurlarını al
                var dovizKurlari = await _dovizKuruService.GetDovizKurlariAsync(user.ApiKey, user.ApiUrl, user.FirmaKodu, user.DonemKodu);

                // İlgili döviz kurunu bul
                var dovizKuru = dovizKurlari.FirstOrDefault(k => k.DovizKodu == doviz)
                    ?? throw new ArgumentException($"Geçersiz döviz kodu: {doviz}");

                // Türkiye zaman dilimini al
                TimeZoneInfo turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");

                // Şu anki Türkiye saatini al
                DateTime nowInTurkey = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, turkeyTimeZone);

                // Gelen tarih ile şu anki Türkiye saatini birleştir
                DateTime combinedDateTime = tarih.Date.Add(nowInTurkey.TimeOfDay);

                var kartData = new
                {
                    _key_sis_ozelkod = 0,
                    _key_sis_seviyekodu = 0,
                    _key_sis_ust_islem_turu = 564595,
                    _key_sis_sube_source = new { subekodu = user.SubeKodu.ToString() },
                    aciklama1 = "GELEN HAVALE",
                    aciklama2 = "",
                    aciklama3 = "",
                    fisno = $"WS{randomNumber}",
                    m_kalemler = new[]
                    {
                new
                {
                    _key_bcs_banka_kredi_taksit = 0,
                    _key_bcs_bankahesabi = new { _key = bankahesabiKey },
                    _key_muh_masrafmerkezi = 0,
                    _key_prj_proje = 0,
                    _key_scf_cari = new { _key = cariIdInt.ToString() },
                    _key_scf_carikart_banka = 0,
                    _key_sis_doviz = new { adi = doviz },
                    _key_sis_ozelkod = 0,
                    aciklama = aciklama,
                    belgeno = "WS" + randomNumber,
                    borc =  tutar.ToString("F4"),
                    borc_cari = tutar.ToString("F4"),
                    detay = "CHSP",
                    dovizkuru = dovizKuru.Kur.ToString("F4"),
                    kurfarkiborc = "0.00"
                }
            },
                    odemeturu = "A",
                    saat = combinedDateTime.ToString("HH:mm:ss"),  // Türkiye'deki şu anki saat
                    tarih = tarih.ToString("yyyy-MM-dd"),
                    turu = "GEHVL"
                };

                sessionId = await LoginAsync();

                var requestData = new
                {
                    bcs_banka_fisi_ekle = new
                    {
                        session_id = sessionId,
                        firma_kodu = user.FirmaKodu,
                        donem_kodu = user.DonemKodu,
                        kart = kartData
                    }
                };

                string wsInput = JsonConvert.SerializeObject(requestData, Formatting.Indented);

                _logger.LogInformation($"Sending request to API: {wsInput}");

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var content = new StringContent(wsInput, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(wsAdres, content);
                    var responseString = await response.Content.ReadAsStringAsync();

                    _logger.LogInformation($"Raw API Response: {responseString}");

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException($"API error: {response.StatusCode}, Response: {responseString}");
                    }

                    // JSON yanıtını parse et
                    var jsonResponse = JObject.Parse(responseString);

                    if (jsonResponse["code"]?.Value<string>() == "200")
                    {
                        string successMessage = jsonResponse["msg"]?.ToString() ?? "İşlem başarılı";
                        string key = jsonResponse["key"]?.ToString() ?? "";

                        // Unicode escape karakterlerini çözümle
                        successMessage = JsonConvert.DeserializeObject<string>($"\"{successMessage}\"");

                        _logger.LogInformation($"API çağrısı başarılı: {successMessage}. Anahtar: {key}");
                        return (true, successMessage);
                    }
                    else
                    {
                        string errorMessage = jsonResponse["msg"]?.ToString() ?? "Bilinmeyen hata";

                        // Unicode escape karakterlerini çözümle
                        errorMessage = JsonConvert.DeserializeObject<string>($"\"{errorMessage}\"");

                        var errors = jsonResponse["errors"]?.ToObject<List<string>>() ?? new List<string>();
                        var warnings = jsonResponse["warnings"]?.ToObject<List<string>>() ?? new List<string>();

                        // Hata ve uyarı mesajlarındaki Unicode escape karakterlerini çözümle
                        errors = errors.Select(e => JsonConvert.DeserializeObject<string>($"\"{e}\"")).ToList();
                        warnings = warnings.Select(w => JsonConvert.DeserializeObject<string>($"\"{w}\"")).ToList();

                        string fullErrorMessage = $"{errorMessage}";
                        if (errors.Any())
                        {
                            fullErrorMessage += $" Hatalar: {string.Join(", ", errors)}";
                        }
                        if (warnings.Any())
                        {
                            fullErrorMessage += $" Uyarılar: {string.Join(", ", warnings)}";
                        }

                        _logger.LogError($"API çağrısı başarısız: {fullErrorMessage}");
                        return (false, fullErrorMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gelen havale fişi kaydedilirken hata oluştu");
                throw;
            }
            finally
            {
                if (!string.IsNullOrEmpty(sessionId))
                {
                    await LogoutAsync(sessionId);
                }
            }
        }

        private async Task<T> SendRequestAsync<T>(string url, object requestBody)
        {
            try
            {
                var jsonSettings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DateFormatHandling = DateFormatHandling.IsoDateFormat
                };

                var content = new StringContent(JsonConvert.SerializeObject(requestBody, jsonSettings), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);

                response.EnsureSuccessStatusCode();
                var responseString = await response.Content.ReadAsStringAsync();
                var jsonResponse = JObject.Parse(responseString);

                if (jsonResponse["code"]?.Value<string>() == "200")
                {
                    string successMessage = jsonResponse["msg"]?.ToString() ?? "İşlem başarılı";
                    string key = jsonResponse["key"]?.ToString() ?? "";

                    successMessage = JsonConvert.DeserializeObject<string>($"\"{successMessage}\"");

                    _logger.LogInformation($"API çağrısı başarılı: {successMessage}. Anahtar: {key}");
                    return JsonConvert.DeserializeObject<T>(responseString, jsonSettings);
                }
                else
                {
                    string errorMessage = jsonResponse["msg"]?.ToString() ?? "Bilinmeyen hata";
                    errorMessage = JsonConvert.DeserializeObject<string>($"\"{errorMessage}\"");

                    var errors = jsonResponse["errors"]?.ToObject<List<string>>() ?? new List<string>();
                    var warnings = jsonResponse["warnings"]?.ToObject<List<string>>() ?? new List<string>();

                    errors = errors.Select(e => JsonConvert.DeserializeObject<string>($"\"{e}\"")).ToList();
                    warnings = warnings.Select(w => JsonConvert.DeserializeObject<string>($"\"{w}\"")).ToList();

                    string fullErrorMessage = $"{errorMessage}";
                    if (errors.Any())
                    {
                        fullErrorMessage += $" Hatalar: {string.Join(", ", errors)}";
                    }
                    if (warnings.Any())
                    {
                        fullErrorMessage += $" Uyarılar: {string.Join(", ", warnings)}";
                    }

                    _logger.LogError($"API çağrısı başarısız: {fullErrorMessage}");
                    throw new ApplicationException(fullErrorMessage);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP isteği sırasında hata oluştu: {Url}", url);
                throw;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parse hatası oluştu");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İstek gönderme sırasında beklenmeyen bir hata oluştu");
                throw;
            }
        }
    }

    public class LoginResponse
    {
        public string code { get; set; }
        public string msg { get; set; }
    }

    public class DiaInfo
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string ApiKey { get; set; }
        public string ApiUrl { get; set; }
        public int FirmaKodu { get; set; }
        public int DonemKodu { get; set; }
    }
}