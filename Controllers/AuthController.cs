using DiaFisTransferEntegrasyonu.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using DiaFisTransferEntegrasyonu.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Hangfire;
using DiaFisTransferEntegrasyonu.Services;
using Newtonsoft.Json;

public class AuthController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ICacheService _cacheService;

    public AuthController(
        ApplicationDbContext context,
        IDistributedCache cache,
        ICacheService cacheService)
    {
        _context = context;
        _cache = cache;
        _cacheService = cacheService;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u =>
                    (u.LoginUsername == model.Username && u.LoginPassword == model.Password) ||
                    (u.Username == model.Username && u.Password == model.Password));

                if (user != null)
                {
                    // DiaInfo'yu oluştur
                    var diaInfo = new DiaInfo
                    {
                        Username = user.Username,
                        Password = user.Password,
                        ApiKey = user.ApiKey,
                        ApiUrl = user.ApiUrl
                        // Diğer gerekli alanları da ekleyin
                    };

                    // DiaInfo'yu cookie'ye kaydet
                    var diaInfoJson = JsonConvert.SerializeObject(diaInfo);
                    var cookieOptions = new CookieOptions
                    {
                        Expires = DateTime.Now.AddHours(1),
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict
                    };

                    Response.Cookies.Delete("DiaInfo"); // Önceki cookie'yi temizle
                    Response.Cookies.Append("DiaInfo", diaInfoJson, cookieOptions);

                    // Authentication işlemleri...
                    var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim("UserId", user.Id.ToString())
                };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity));

                    return RedirectToAction("Index", "Transfer");
                }

                ModelState.AddModelError(string.Empty, "Geçersiz kullanıcı adı veya şifre.");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Giriş işlemi sırasında bir hata oluştu.");
            }
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        Response.Cookies.Delete("DiaInfo");
        return RedirectToAction("Login");
    }
}