using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using KayAyiti.Data;
using KayAyiti.Helpers;
using KayAyiti.Hubs;
using KayAyiti.Services;

var builder = WebApplication.CreateBuilder(args);

// ─── Database ────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ─── Services ────────────────────────────────────────────────────────────────
builder.Services.AddScoped<JwtHelper>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IHouseService, HouseService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IKycService, KycService>();
builder.Services.AddScoped<IReportService, ReportService>();

// ─── JWT + OAuth Authentication ─────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]!;
var cfg    = builder.Configuration;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidIssuer = cfg["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = cfg["Jwt:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
})
.AddCookie("External", o =>
{
    o.Cookie.SameSite     = SameSiteMode.Lax;
    o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    o.ExpireTimeSpan      = TimeSpan.FromMinutes(10);
});

// Google — ajoute sèlman si credentials konfigire
if (!string.IsNullOrEmpty(cfg["OAuth:Google:ClientId"]))
{
    builder.Services.AddAuthentication()
        .AddGoogle("Google", o =>
        {
            o.SignInScheme = "External";
            o.ClientId     = cfg["OAuth:Google:ClientId"]!;
            o.ClientSecret = cfg["OAuth:Google:ClientSecret"]!;
            o.CallbackPath = "/api/auth/callback/google";
            o.Scope.Add("email");
            o.Scope.Add("profile");
        });
}

// Facebook — ajoute sèlman si credentials konfigire
if (!string.IsNullOrEmpty(cfg["OAuth:Facebook:AppId"]))
{
    builder.Services.AddAuthentication()
        .AddFacebook("Facebook", o =>
        {
            o.SignInScheme = "External";
            o.AppId        = cfg["OAuth:Facebook:AppId"]!;
            o.AppSecret    = cfg["OAuth:Facebook:AppSecret"]!;
            o.CallbackPath = "/api/auth/callback/facebook";
            o.Scope.Add("email");
        });
}

// Apple — ajoute sèlman si credentials konfigire
if (!string.IsNullOrEmpty(cfg["OAuth:Apple:ClientId"]))
{
    builder.Services.AddAuthentication()
        .AddApple("Apple", o =>
        {
            o.SignInScheme          = "External";
            o.ClientId              = cfg["OAuth:Apple:ClientId"]!;
            o.TeamId                = cfg["OAuth:Apple:TeamId"]!;
            o.KeyId                 = cfg["OAuth:Apple:KeyId"]!;
            o.CallbackPath          = "/api/auth/callback/apple";
            o.GenerateClientSecret  = true;
            o.PrivateKey            = (_, _) =>
                Task.FromResult((cfg["OAuth:Apple:PrivateKey"] ?? "").AsMemory());
        });
}

builder.Services.AddSignalR();
builder.Services.AddAuthorization();

// ─── CORS ─────────────────────────────────────────────────────────────────────
builder.Services.AddCors(opt =>
    opt.AddPolicy("AllowAll", p => p
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader()));

// ─── Controllers + Swagger ───────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Your API", Version = "v1" });

    // Define the security scheme
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});
var app = builder.Build();

// ─── Migrations automatik (avèk retry pou Docker) ────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db     = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    var retries = 0;
    while (retries < 10)
    {
        try
        {
            logger.LogInformation("Ap eseye konekte ak SQL Server... (tantativ {n})", retries + 1);
            db.Database.Migrate();
            logger.LogInformation("Migration reyisi ✓");
            break;
        }
        catch (Exception ex)
        {
            retries++;
            logger.LogWarning("SQL Server pako prè: {msg}. Tann 5 segonn...", ex.Message);
            Thread.Sleep(5000);
            if (retries >= 10)
            {
                logger.LogError("Echèk koneksyon SQL Server apre 10 tantativ.");
                throw;
            }
        }
    }
}

// ─── Middleware pipeline ──────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "KayAyiti API v1"));
}

app.UseDefaultFiles(); // dwe vini AVAN UseStaticFiles pou / → index.html mache
app.UseStaticFiles();
app.MapGet("/admin", (IWebHostEnvironment env) =>
    Results.File(Path.Combine(env.WebRootPath, "admin.html"), "text/html"));
app.MapGet("/verify", (IWebHostEnvironment env) =>
    Results.File(Path.Combine(env.WebRootPath, "verify.html"), "text/html"));
if (!app.Environment.IsEnvironment("Docker"))
    app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();

app.UseAuthorization();
app.MapControllers();
app.MapHub<CallHub>("/callhub");

app.Run();