using System.Text;
using Microsoft.AspNetCore.DataProtection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RunWay.Api.Data;
using RunWay.Api.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(builder.Configuration["DataProtection:Path"] ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys")));

// ---------- Database ----------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
    if (builder.Configuration["Database:Provider"] == "Sqlite") options.UseSqlite(connectionString);
    else options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
});

// ---------- Identity (users + roles only; the API issues JWTs instead of cookies) ----------
builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// ---------- JWT authentication ----------
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key was not found.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents { OnTokenValidated = async context => {
            var users = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByIdAsync(context.Principal?.FindFirst("sub")?.Value ?? "");
            if (user == null || context.Principal?.FindFirst("stamp")?.Value != user.SecurityStamp) context.Fail("Session expired.");
        }};
        options.MapInboundClaims = false; // keep "sub" / "role" claim names as issued
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            NameClaimType = "name",
            RoleClaimType = "role",
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();
if (Encoding.UTF8.GetByteCount(jwtKey) < 32) throw new InvalidOperationException("Configure Jwt:Key with at least 32 bytes.");

// ---------- Services ----------
builder.Services.AddSingleton<ITokenService, TokenService>();
if (builder.Configuration["Storage:Provider"] == "Local")
{
    if (!builder.Environment.IsDevelopment()) throw new InvalidOperationException("Local image storage is development-only. Configure Azure storage for deployment.");
    builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();
}
else builder.Services.AddSingleton<IFileStorageService, AzureBlobStorageService>();

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "RunWay.Api.xml"));
    c.OperationFilter<EndpointDocumentation>();
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "RaceDay API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the token returned by POST /api/auth/login."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();
app.UseMiddleware<ApiErrorMiddleware>();

// ---------- Create database, roles, admin and demo data ----------
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var seedDemo = builder.Configuration.GetValue("Seed:DemoData", false);
    await DbSeeder.InitialiseAsync(scope.ServiceProvider, seedDemo, logger);
}

// ---------- Pipeline ----------
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapControllers();

app.Run();
