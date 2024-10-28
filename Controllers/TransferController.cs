using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using DiaFisTransferEntegrasyonu.Services;
using DiaFisTransferEntegrasyonu.Data;
using DiaFisTransferEntegrasyonu.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Text.Json;
using System.Net;
using Newtonsoft.Json;

namespace DiaFisTransferEntegrasyonu.Controllers
{
    [Authorize] // Ensure that the user is authenticated
    public class TransferController : Controller
    {
        private readonly IDiaService _diaService;
        private readonly ApplicationDbContext _context;
        private readonly IDovizKuruService _dovizKuruService;

        public TransferController(IDiaService diaService, ApplicationDbContext context, IDovizKuruService dovizKuruService)
        {
            _diaService = diaService;
            _context = context;
            _dovizKuruService = dovizKuruService;
        }

        [HttpGet]
        public async Task<IActionResult> GetDovizKurlari()
        {
            int userId = await GetCurrentUserIdAsync();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound();

            var dovizKurlari = await _dovizKuruService.GetDovizKurlariAsync(user.ApiKey, user.ApiUrl, user.FirmaKodu, user.DonemKodu);

            return Json(dovizKurlari);
        }

        // GET: /Transfer/Index
        public async Task<IActionResult> Index()
        {
            int userId = await GetCurrentUserIdAsync();

            if (userId == 0)
            {
                // User is not logged in or session has expired
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users
                .Include(u => u.KasaKartlari)
                .Include(u => u.OdemePlanlari)
                .Include(u => u.BankaHesaplari)
                .Include(u => u.Cariler)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound();

            // Döviz kurlarını al
            var dovizKurlari = await _dovizKuruService.GetDovizKurlariAsync(user.ApiKey, user.ApiUrl, user.FirmaKodu, user.DonemKodu);

            // Create a ViewModel to pass the data to the View
            var model = new TransferViewModel
            {
                KasaKartlari = user.KasaKartlari.ToList(),
                OdemePlanlari = user.OdemePlanlari.ToList(),
                BankaHesaplari = user.BankaHesaplari.ToList(),
                Cariler = user.Cariler.ToList(),
                DovizKurlari = dovizKurlari
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateInfo()
        {
            try
            {
                int userId = await GetCurrentUserIdAsync();

                if (userId == 0)
                {
                    return Json(new { success = false, message = "Kullanıcı bulunamadı veya oturum süresi dolmuş." });
                }

                await _diaService.UpdateDataAsync(userId);

                return Json(new { success = true, message = "Bilgiler başarıyla güncellendi.", redirectUrl = Url.Action("Index", "Transfer") });
            }
            catch (ArgumentException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                // You may log the error here
                return Json(new { success = false, message = "Bir hata oluştu. Lütfen tekrar deneyin." });
            }
        }

        // GET: /Transfer/SearchCariler
        [HttpGet]
        public async Task<IActionResult> SearchCariler(string term)
        {
            int userId = await GetCurrentUserIdAsync();

            if (userId == 0)
            {
                return Unauthorized(); // Or handle as appropriate
            }

            // Term'i büyük harfe çeviriyoruz
            string upperTerm = term.ToUpper();

            var cariler = await _context.Cariler
                .Where(c => c.UserId == userId && c.CariAdi.ToUpper().Contains(upperTerm))
                .Select(c => new { id = c.CariId, label = c.CariAdi, value = c.CariAdi })
                .Take(10)
                .ToListAsync();

            return Json(cariler);
        }

        // You can add similar actions for other searches if needed
        // For example, if you need to implement autocomplete for other entities

        // POST: /Transfer/SubmitForm1
        [HttpPost]
        public async Task<IActionResult> SubmitForm1(Form1Model model)
        {
            int userId = await GetCurrentUserIdAsync();

            if (userId == 0)
            {
                return Unauthorized(); // Or handle as appropriate
            }

            if (ModelState.IsValid)
            {
                // Process the form data
                // For example, save to database or call an external service

                // Redirect to a confirmation page or back to the Index
                return RedirectToAction("Index");
            }

            // If the model state is invalid, reload the data and return the view with validation errors
            var user = await _context.Users
                .Include(u => u.KasaKartlari)
                .Include(u => u.OdemePlanlari)
                .Include(u => u.BankaHesaplari)
                .Include(u => u.Cariler)
                .FirstOrDefaultAsync(u => u.Id == userId);

            var viewModel = new TransferViewModel
            {
                KasaKartlari = user.KasaKartlari.ToList(),
                OdemePlanlari = user.OdemePlanlari.ToList(),
                BankaHesaplari = user.BankaHesaplari.ToList(),
                Cariler = user.Cariler.ToList()
            };

            return View("Index", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> NakitTahsilatFisKayit(string cariId, string cariAdi, string kasaId, string kasaAdi, decimal tutar, DateTime tarih, string doviz, string aciklama)
        {
            try
            {
                int userId = await GetCurrentUserIdAsync();

                if (userId == 0)
                {
                    TempData["SweetAlertMessage"] = "Kullanıcı bulunamadı veya oturum süresi dolmuş.";
                    TempData["SweetAlertType"] = "error";
                    return RedirectToAction("Index");
                }

                var result = await _diaService.NakitTahsilatFisKayit(userId, cariId, cariAdi, kasaId, kasaAdi, tutar, tarih, doviz, aciklama);

                bool success;
                string message;

                (success, message) = result;

                // Mesajı JSON serialize et
                string jsonMessage = JsonConvert.SerializeObject(message);

                if (success)
                {
                    TempData["SweetAlertMessage"] = jsonMessage;
                    TempData["SweetAlertType"] = "success";
                }
                else
                {
                    TempData["SweetAlertMessage"] = jsonMessage;
                    TempData["SweetAlertType"] = "error";
                }
                return RedirectToAction("Index", "Transfer");
            }
            catch (Exception ex)
            {
                string errorMessage = $"Beklenmeyen bir hata oluştu: {ex.Message}";
                string jsonErrorMessage = JsonConvert.SerializeObject(errorMessage);

                TempData["SweetAlertMessage"] = jsonErrorMessage;
                TempData["SweetAlertType"] = "error";
                return RedirectToAction("Index", "Transfer");
            }
        }

        [HttpPost("KartTahsilatFisKayit")]
        public async Task<IActionResult> KartTahsilatFisKayit(string cariId, string planId, string planHesapKey, decimal tutar, DateTime tarih, string doviz, string aciklama)
        {
            try
            {
                int userId = await GetCurrentUserIdAsync();
                var result = await _diaService.KartTahsilatFisKayit(userId, cariId, planId, planHesapKey, tutar, tarih, doviz, aciklama);

                bool success;
                string message;

                (success, message) = result;

                // Mesajı JSON serialize et
                string jsonMessage = JsonConvert.SerializeObject(message);

                if (success)
                {
                    TempData["SweetAlertMessage"] = jsonMessage;
                    TempData["SweetAlertType"] = "success";
                }
                else
                {
                    TempData["SweetAlertMessage"] = jsonMessage;
                    TempData["SweetAlertType"] = "error";
                }
                return RedirectToAction("Index", "Transfer");
            }
            catch (Exception ex)
            {
                string errorMessage = $"Beklenmeyen bir hata oluştu: {ex.Message}";
                string jsonErrorMessage = JsonConvert.SerializeObject(errorMessage);

                TempData["SweetAlertMessage"] = jsonErrorMessage;
                TempData["SweetAlertType"] = "error";
                return RedirectToAction("Index", "Transfer");
            }
        }

            [HttpPost]
        public async Task<IActionResult> VirmanFisKayit(DateTime tarih, string alacakCariAdi, string alacakCariId,
    decimal alacakTutar, string alacakDoviz, string borcCariAdi, string borcCariId,
    decimal borcTutar, string borcDoviz, string aciklama)
        {
            borcDoviz = alacakDoviz;
            try
            {
                int userId = await GetCurrentUserIdAsync();

                VirmanFisiModel virmanFisi = new VirmanFisiModel
                {
                    FirmaKodu = 1, // Örnek değer, gerçek değeri siz belirleyin
                    DonemKodu = 1, // Örnek değer, gerçek değeri siz belirleyin
                    SubeKodu = "DS001", // Örnek değer, gerçek değeri siz belirleyin
                    Aciklama1 = aciklama,
                    Aciklama2 = "",
                    Aciklama3 = "",
                    BelgeNo = "",
                    FisNo = "VF" + DateTime.Now.ToString("yyyyMMddHHmmss"), // Örnek fiş no oluşturma
                    Kalemler = new List<VirmanFisiKalemModel>(),
                    Saat = DateTime.Now,
                    Tarih = tarih,
                    Turu = "VF", // Virman Fişi
                    UstIslemTuru = 0, // Gerekirse değiştirin
                    OzelKod1 = ""
                };

                // Alacak kaydı
                if (!string.IsNullOrEmpty(alacakCariId) && alacakTutar > 0)
                {
                    virmanFisi.Kalemler.Add(new VirmanFisiKalemModel
                    {
                        CariKartKodu = alacakCariId,
                        DovizAdi = alacakDoviz,
                        Aciklama = aciklama,
                        DovizKuru = 1, // Varsayılan olarak 1, gerekirse güncel kuru alın
                        KurFarkiAlacak = 0,
                        KurFarkiBorc = 0,
                        MakbuzNo = "",
                        CariKart = alacakCariId,
                        CariUnvan = alacakCariAdi,
                        _key_scf_carikart = int.Parse(alacakCariId),
                        Vade = tarih,
                        Alacak = alacakTutar,
                        BankaHesapKodu = ""
                    });
                }

                // Borç kaydı
                if (!string.IsNullOrEmpty(borcCariId) && borcTutar > 0)
                {
                    virmanFisi.Kalemler.Add(new VirmanFisiKalemModel
                    {
                        CariKartKodu = borcCariId,
                        DovizAdi = borcDoviz,
                        Aciklama = aciklama,
                        DovizKuru = 1, // Varsayılan olarak 1, gerekirse güncel kuru alın
                        KurFarkiAlacak = 0,
                        KurFarkiBorc = 0,
                        MakbuzNo = "",
                        CariKart = borcCariId,
                        CariUnvan = borcCariAdi,
                        _key_scf_carikart = int.Parse(borcCariId),
                        Vade = tarih,
                        Borc = borcTutar,
                        BankaHesapKodu = ""
                    });
                }

                var response = await _diaService.SendVirmanFisiAsync(virmanFisi, userId);

                string message;
                string alertType;

                if (response.Code == "200") // Başarılı yanıt kodu
                {
                    message = response.Message;
                    alertType = "success";
                }
                else
                {
                    message = "Virman fişi kaydedilirken bir hata oluştu: " + response.Message;
                    alertType = "error";
                }

                // Mesajı JSON serialize et
                string jsonMessage = JsonConvert.SerializeObject(message);

                TempData["SweetAlertMessage"] = message;
                TempData["SweetAlertType"] = alertType;
            }
            catch (Exception ex)
            {
                string errorMessage = $"Beklenmeyen bir hata oluştu: {ex.Message}";
                string jsonErrorMessage = JsonConvert.SerializeObject(errorMessage);

                TempData["SweetAlertMessage"] = jsonErrorMessage;
                TempData["SweetAlertType"] = "error";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<ActionResult> GelenHavaleKayit(string bankaHesapId, string cariId, string tarih, string tutar, string doviz, string aciklama)
        {
            try
            {
                int userId = await GetCurrentUserIdAsync();

                var result = await _diaService.GelenHavaleFisiKayit(
                    userId: userId,
                    cariId: cariId,
                    bankahesabiKey: bankaHesapId,
                    tutar: decimal.Parse(tutar),
                    tarih: DateTime.Parse(tarih),
                    doviz: doviz,
                    aciklama: aciklama
                );

                bool success;
                string message;

                (success, message) = result;

                // Mesajı JSON serialize et
                string jsonMessage = JsonConvert.SerializeObject(message);

                if (success)
                {
                    TempData["SweetAlertMessage"] = jsonMessage;
                    TempData["SweetAlertType"] = "success";
                }
                else
                {
                    TempData["SweetAlertMessage"] = jsonMessage;
                    TempData["SweetAlertType"] = "error";
                }
                return RedirectToAction("Index", "Transfer");
            }
            catch (Exception ex)
            {
                string errorMessage = $"Beklenmeyen bir hata oluştu: {ex.Message}";
                string jsonErrorMessage = JsonConvert.SerializeObject(errorMessage);

                TempData["SweetAlertMessage"] = jsonErrorMessage;
                TempData["SweetAlertType"] = "error";
                return RedirectToAction("Index", "Transfer");
            }
        }

        private async Task<int> GetCurrentUserIdAsync()
        {
            var username = User.Identity.Name;
            if (string.IsNullOrEmpty(username))
            {
                return 0; // User is not logged in
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            return user?.Id ?? 0;
        }
    }
}