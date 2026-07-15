using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using EVSwap.API.Infrastructure.Data;
using EVSwap.API.Core.Interfaces.Repositories;
using EVSwap.API.Core.Interfaces.Services;
using EVSwap.API.Infrastructure.Repositories;
using EVSwap.API.Infrastructure.Services;
using EVSwap.API.Middleware;
using EVSwap.API.Infrastructure.Utilities;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<AppConstants>(builder.Configuration.GetSection(AppConstants.SectionName));
var constants = builder.Configuration.GetSection(AppConstants.SectionName).Get<AppConstants>()
    ?? new AppConstants();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(constants.JwtKey)),
        ValidateIssuer = true,
        ValidIssuer = constants.JwtIssuer,
        ValidateAudience = true,
        ValidAudience = constants.JwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IBatteryService, BatteryService>();
builder.Services.AddScoped<IStationService, StationService>();
builder.Services.AddScoped<ISwapService, SwapService>();
builder.Services.AddScoped<ITripService, TripService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IFleetService, FleetService>();
builder.Services.AddScoped<IMaintenanceService, MaintenanceService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IBatteryRepository, BatteryRepository>();
builder.Services.AddScoped<IStationRepository, StationRepository>();
builder.Services.AddScoped<ISwapRepository, SwapRepository>();
builder.Services.AddScoped<ITripRepository, TripRepository>();
builder.Services.AddScoped<IWalletRepository, WalletRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IFleetRepository, FleetRepository>();
builder.Services.AddScoped<IMaintenanceRepository, MaintenanceRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<EVSwap.API.SignalR.BatteryHub>("/hubs/battery");
app.MapHub<EVSwap.API.SignalR.StationHub>("/hubs/station");
app.MapHub<EVSwap.API.SignalR.NotificationHub>("/hubs/notification");
app.MapHub<EVSwap.API.SignalR.DashboardHub>("/hubs/dashboard");

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbInitializer.Initialize(context);
}

app.Run();
