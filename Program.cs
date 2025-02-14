using Microsoft.AspNetCore.Authentication.Cookies;
using DiaFisTransferEntegrasyonu.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using DiaFisTransferEntegrasyonu.Services;
using System.Text.Json;
using System.Text.Unicode;
using Microsoft.Extensions.Configuration.Json;
using System.Text.Encodings.Web;
using Hangfire;
using Hangfire.MemoryStorage;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpClient();
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor(); // Yeni eklenen satır
builder.Services.AddScoped<IDovizKuruService, DovizKuruService>();
builder.Services.AddScoped<IDiaService, DiaService>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddDistributedMemoryCache(); // Veya Redis için: services.AddStackExchangeRedisCache()
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddHangfire(config => config.UseMemoryStorage()); // Veya SQL Server/Redis
builder.Services.AddHangfireServer();


builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions =>
        {
            npgsqlOptions.CommandTimeout(180); // 3 dakika timeout
            npgsqlOptions.EnableRetryOnFailure(3); // 3 kez yeniden deneme
        }
    )
);

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => true;
    options.MinimumSameSitePolicy = SameSiteMode.Strict;
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
       .AddCookie(options =>
       {
           options.LoginPath = "/Auth/Login";
           options.AccessDeniedPath = "/Auth/AccessDenied";
       });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // Bu satırı ekleyin
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();