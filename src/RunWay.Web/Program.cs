using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using RunWay.Web;
var builder=WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();builder.Logging.AddConsole();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o=>{o.IdleTimeout=TimeSpan.FromHours(2);o.Cookie.Name="RaceDay.Session";o.Cookie.HttpOnly=true;o.Cookie.IsEssential=true;o.Cookie.SameSite=SameSiteMode.Lax;});
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<ApiClient>(c=>{c.BaseAddress=new Uri(builder.Configuration["Api:BaseUrl"]??"http://localhost:5100/");c.Timeout=TimeSpan.FromSeconds(30);})
 .ConfigurePrimaryHttpMessageHandler(()=>new HttpClientHandler{UseCookies=false});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o=>{
 o.Cookie.Name="RaceDay.Auth";o.LoginPath="/Account/Login";o.AccessDeniedPath="/Account/Denied";o.ExpireTimeSpan=TimeSpan.FromHours(2);o.SlidingExpiration=false;
 o.Events.OnValidatePrincipal=async ctx=>{if(string.IsNullOrEmpty(ctx.HttpContext.Session.GetString("ApiToken"))){ctx.RejectPrincipal();await ctx.HttpContext.SignOutAsync();}};
});
builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews(o=>{o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());o.Filters.Add<ApiErrorFilter>();});
var keyFolder=builder.Configuration["DataProtection:Path"];
if(!string.IsNullOrEmpty(keyFolder))builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keyFolder));
var app=builder.Build();
app.UseExceptionHandler("/Home/Error");app.UseStaticFiles();app.UseRouting();app.UseSession();app.UseAuthentication();app.UseAuthorization();
app.MapGet("/health",()=>Results.Ok(new{status="healthy"}));
app.MapControllerRoute("default","{controller=Events}/{action=Index}/{id?}");
app.Run();
