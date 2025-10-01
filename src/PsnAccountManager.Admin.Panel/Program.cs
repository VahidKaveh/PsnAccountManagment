using Microsoft.EntityFrameworkCore;
using PsnAccountManager.Application.Interfaces;
using PsnAccountManager.Application.Services;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Infrastructure.BackgroundWorkers;
using PsnAccountManager.Infrastructure.Data;
using PsnAccountManager.Infrastructure.Repositories;
using PsnAccountManager.Infrastructure.Services;
using TL;

var builder = WebApplication.CreateBuilder(args);

// ===================================================================
// 1. Service Registration (Dependency Injection)
// ===================================================================

// --- Infrastructure Layer Services ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<PsnAccountManagerDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), mySqlOptionsAction: sqlOptions =>
    {
        sqlOptions.MigrationsAssembly(typeof(PsnAccountManagerDbContext).Assembly.FullName);
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    }));

// Register Repositories
builder.Services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IChannelRepository, ChannelRepository>();
builder.Services.AddScoped<IGameRepository, GameRepository>();
builder.Services.AddScoped<IGuideRepository, GuideRepository>();
builder.Services.AddScoped<IParsingProfileRepository, ParsingProfileRepository>();
builder.Services.AddScoped<IPurchaseRepository, PurchaseRepository>();
builder.Services.AddScoped<IPurchaseSuggestionRepository, PurchaseSuggestionRepository>();
builder.Services.AddScoped<IRawMessageRepository, RawMessageRepository>();
builder.Services.AddScoped<IRequestRepository, RequestRepository>();
builder.Services.AddScoped<ISettingRepository, SettingRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAdminNotificationRepository, AdminNotificationRepository>();
builder.Services.AddScoped<ILearningDataRepository, LearningDataRepository>();
builder.Services.AddScoped<IMessageParser, MessageParser>();



// --- Application Layer Services ---
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddSingleton<IWorkerStateService, WorkerStateService>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IGuideService, GuideService>();
builder.Services.AddScoped<IMatcherService, MatcherService>();
builder.Services.AddScoped<IMessageParser, MessageParser>();
builder.Services.AddScoped<IPurchaseService, PurchaseService>();
builder.Services.AddScoped<IProcessingService, ProcessingService>();
builder.Services.AddScoped<IScraperService, ScraperService>();

// --- External Service Wrappers & Background Workers ---
builder.Services.AddSingleton<ITelegramClient, TelegramClientWrapper>();
builder.Services.AddHostedService<ScraperWorker>();

// --- Presentation Layer Services ---
builder.Services.AddRazorPages();


// ===================================================================
// 2. Build the Application
// ===================================================================
var app = builder.Build();


// ===================================================================
// 3. Configure the HTTP Request Pipeline (Middleware)
//    NOTE: Order is critical here.
// ===================================================================
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Serves static files like CSS, JS, images from wwwroot

app.UseRouting(); // Marks the position in the middleware pipeline where routing decisions are made.

// app.UseAuthentication(); // Uncomment once you have authentication
app.UseAuthorization(); // Authorizes a user to access secure resources. MUST be after UseRouting.

app.MapRazorPages(); // Configures endpoints for Razor Pages. MUST be at the end.


// ===================================================================
// 4. Run the Application
// ===================================================================
app.Run();