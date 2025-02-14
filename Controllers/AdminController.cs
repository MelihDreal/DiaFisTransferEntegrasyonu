using DiaFisTransferEntegrasyonu.Data;
using DiaFisTransferEntegrasyonu.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DiaFisTransferEntegrasyonu.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        } 

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(UserCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new User
                {
                    Username = model.Username,
                    Password = model.Password, // Güvenlik için şifrelenmeli
                    FirmaKodu = Convert.ToInt32(model.FirmaKodu),
                    DonemKodu = Convert.ToInt32(model.DonemKodu),
                    ApiKey = model.ApiKey,
                    ApiUrl = model.ApiUrl,
                    SubeKodu = model.SubeKodu
                    // IsAdmin ve SonGuncellenmeTarihi otomatik olarak ayarlanacak
                };

                // UstIslemTuru sadece dolu geldiğinde atanıyor
                if (!string.IsNullOrEmpty(model.UstIslemTuru))
                {
                    user.UstIslemTuru = Convert.ToInt32(model.UstIslemTuru);
                }

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                TempData["SweetAlert"] = System.Text.Json.JsonSerializer.Serialize(new
                {
                    icon = "success",
                    title = "Başarılı!",
                    text = "Kullanıcı başarıyla oluşturuldu."
                });
                return RedirectToAction("Index", "Admin");
            }

            TempData["SweetAlert"] = System.Text.Json.JsonSerializer.Serialize(new
            {
                icon = "error",
                title = "Hata!",
                text = "Kullanıcı oluşturulurken bir hata oluştu."
            });
            return View("Index", model);
        }
    }
}