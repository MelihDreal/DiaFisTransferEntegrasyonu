using DiaFisTransferEntegrasyonu.Models;
using DiaFisTransferEntegrasyonu.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

public interface IDovizKuruService
{
    Task<List<DovizKuru>> GetDovizKurlariAsync(string apiKey, string apiUrl, int firmaKodu, int donemKodu);
}

public class DovizKuruService : IDovizKuruService
{
    private readonly ConcurrentDictionary<string, (List<DovizKuru> Kurlar, DateTime LastUpdated)> _kurCache
        = new ConcurrentDictionary<string, (List<DovizKuru>, DateTime)>();

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<DovizKuruService> _logger;

    public DovizKuruService(IHttpClientFactory httpClientFactory, ILogger<DovizKuruService> logger, IHttpContextAccessor httpContextAccessor)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<List<DovizKuru>> GetDovizKurlariAsync(string apiKey, string apiUrl, int firmaKodu, int donemKodu)
    {
        var cacheKey = $"{apiKey}_{firmaKodu}_{donemKodu}";

        if (_kurCache.TryGetValue(cacheKey, out var cachedData))
        {
            if (DateTime.Now.Date == cachedData.LastUpdated.Date)
            {
                return cachedData.Kurlar;
            }
        }

        var yeniKurlar = await FetchDovizKurlariAsync(apiKey, apiUrl, firmaKodu, donemKodu);
        _kurCache[cacheKey] = (yeniKurlar, DateTime.Now);

        return yeniKurlar;
    }

    private DiaInfo GetDiaInfo()
    {
        try
        {
            var diaInfoJson = _httpContextAccessor.HttpContext?.Request.Cookies["DiaInfo"];

            if (string.IsNullOrEmpty(diaInfoJson))
            {
                _logger.LogWarning("DiaInfo cookie'si bulunamadı");
                throw new Exception("Dia bilgileri bulunamadı. Lütfen tekrar giriş yapın.");
            }

            _logger.LogDebug("DiaInfo cookie içeriği: {DiaInfoJson}", diaInfoJson);

            var diaInfo = JsonConvert.DeserializeObject<DiaInfo>(diaInfoJson);

            if (diaInfo == null)
            {
                _logger.LogError("DiaInfo deserialize edilemedi");
                throw new Exception("Dia bilgileri geçersiz format.");
            }

            return diaInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetDiaInfo metodunda hata");
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

    private async Task<List<DovizKuru>> FetchDovizKurlariAsync(string apiKey, string apiUrl, int firmaKodu, int donemKodu)
    {
        var sessionId = await LoginAsync();

        var requestBody = new
        {
            sis_doviz_kuru_listele_sontarih = new
            {
                session_id = sessionId,
                firma_kodu = firmaKodu,
                donem_kodu = donemKodu,
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

        var jsonResponse = await SendRequestAsync<JObject>(apiUrl + "/api/v3/sis/json", requestBody);

        if (jsonResponse["result"] is JArray resultArray)
        {
            return resultArray.Select(item => new DovizKuru
            {
                DovizKodu = item["adi"].ToString(),
                Kur = decimal.Parse(item["kur4"].ToString()),
                Tarih = DateTime.Parse(item["tarih"].ToString())
            }).ToList();
        }

        return new List<DovizKuru>();
    }

    private async Task<T> SendRequestAsync<T>(string url, object requestBody)
    {
        try
        {
            using var httpClient = _httpClientFactory.CreateClient();

            var jsonSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DateFormatHandling = DateFormatHandling.IsoDateFormat
            };

            var content = new StringContent(JsonConvert.SerializeObject(requestBody, jsonSettings), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(url, content);

            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(responseString, jsonSettings);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP isteği sırasında hata oluştu: {Url}", url);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Döviz kurları parse edilirken JSON hatası oluştu");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "İstek gönderme sırasında beklenmeyen bir hata oluştu");
            throw;
        }
    }
}