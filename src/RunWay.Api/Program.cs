using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RunWay.Api;
var builder=WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();builder.Logging.AddConsole();
builder.Services.AddDbContext<RaceDb>(o=>{
 if(builder.Configuration["Database:Provider"]=="SqlServer")o.UseSqlServer(builder.Configuration.GetConnectionString("RaceDay"));
 else o.UseSqlite(builder.Configuration.GetConnectionString("RaceDay")??"Data Source=raceday.db");
});
builder.Services.AddIdentityCore<ApplicationUser>(o=>{o.User.RequireUniqueEmail=true;o.Password.RequiredLength=8;o.Lockout.MaxFailedAccessAttempts=5;})
 .AddEntityFrameworkStores<RaceDb>();
builder.Services.AddAuthentication("Bearer").AddScheme<AuthenticationSchemeOptions,TokenHandler>("Bearer",_=>{});
builder.Services.AddAuthorization();
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(builder.Configuration["DataProtection:Path"]??Path.Combine(builder.Environment.ContentRootPath,"App_Data","keys")));
builder.Services.AddScoped<ImageStore>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command",LogLevel.Warning);
var app=builder.Build();
app.Use(async(ctx,next)=>{
 try{await next();}
 catch(ArgumentException e){ctx.Response.StatusCode=400;await ctx.Response.WriteAsJsonAsync(new{message=e.Message});}
 catch(DbUpdateException){ctx.Response.StatusCode=409;await ctx.Response.WriteAsJsonAsync(new{message="This record conflicts with an existing record. Refresh and try again."});}
 catch(Exception e){app.Logger.LogError(e,"Request failed");ctx.Response.StatusCode=500;await ctx.Response.WriteAsJsonAsync(new{message="The service could not complete this request. Please try again."});}
});
app.UseAuthentication();app.UseAuthorization();app.MapControllers();
app.MapGet("/health",()=>Results.Ok(new{status="healthy"}));
app.MapGet("/api/media/{name}",async(string name,ImageStore store)=>{
 var bytes=await store.Read(name);return bytes is null?Results.NotFound():Results.File(bytes,name.EndsWith(".png")?"image/png":"image/jpeg");
});
if(app.Environment.IsDevelopment()){app.UseSwagger();app.UseSwaggerUI();}
using(var scope=app.Services.CreateScope()){
 var db=scope.ServiceProvider.GetRequiredService<RaceDb>();
 // Initial schema only; see docs for the migration policy before changing a deployed schema.
 await db.Database.EnsureCreatedAsync();
}
app.Run();
