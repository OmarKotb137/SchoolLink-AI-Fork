using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Project.API.Extensions;
using Project.API.Middleware;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting SchoolLink API");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, config) =>
        config.ReadFrom.Configuration(context.Configuration)
              .ReadFrom.Services(services)
              .Enrich.FromLogContext()
              .WriteTo.Console()
              .WriteTo.File("logs/schooLink-.log",
                  rollingInterval: RollingInterval.Day,
                  retainedFileCountLimit: 30));

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddApplicationServices(builder.Configuration);

    var jwtSettings = builder.Configuration.GetSection("Jwt");
    var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

    builder.Services.AddAuthorization();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAngular", policy =>
            policy.WithOrigins("http://localhost:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod());
    });

    var app = builder.Build();
    ///////////////////////////////////////////////////////////////////////////////
    //await SeedData.Initialize(app.Services);

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseStaticFiles();
    app.UseMiddleware<ExceptionMiddleware>();
    app.UseCors("AllowAngular");
    app.UseHttpsRedirection();
    app.UseAuthentication();

    // =========================================================================
    // TODO: REMOVE THIS MOCK AUTHENTICATION MIDDLEWARE BEFORE PRODUCTION!
    // تمت إضافة هذا الكود مؤقتاً لتخطي نظام تسجيل الدخول (Login) لتسريع اختبار واجهات 
    // النظام الأمامية دون الحاجة لربط الـ JWT Tokens.
    // يقوم هذا الكود بإيهام السيرفر أن هناك مستخدم رقمه (1) يمتلك كافة الصلاحيات.
    // =========================================================================
    app.Use(async (context, next) =>
    {
        var claims = new List<System.Security.Claims.Claim>
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "1"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "Mock Tester"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Admin"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Teacher"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Student"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Parent")
        };
        var identity = new System.Security.Claims.ClaimsIdentity(claims, "MockAuthType");
        context.User = new System.Security.Claims.ClaimsPrincipal(identity);
        
        await next();
    });
    // =========================================================================

    app.UseAuthorization();
    app.MapControllers();

    Log.Information("SchoolLink API is ready");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
