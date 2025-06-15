using Crowbar;
using Crowbar.Actions;
using Crowbar.Captcha;
using Crowbar.Data;
using Crowbar.Encryption;
using Crowbar.Middleware;
using Crowbar.Models;
using Crowbar.Utils;
using Ixnas.AltchaNet;
using Markdig;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using Westwind.AspNetCore.Markdown;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("Settings/appsettings.json");
var encryptionKey = builder.Configuration["encryptionkey"];
if (encryptionKey is null)
    throw new Exception("Please provide a 'encryptionkey' in you appsettings.json");
if (encryptionKey.ToLower().Trim() == "RANDOMIZE_KEY")
    encryptionKey = EncryptionLayer.RandomText(1024);
EncryptionLayer.SetPassword(encryptionKey);
encryptionKey = "0000000000000000";

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options => {
    switch (builder.Configuration["database"]?.ToLower().Trim() ?? "sqlite")
    {
        case "mysql":
            options.UseMySQL(connectionString); break;
        case "postgre":
            options.UseNpgsql(connectionString); break;
        case "sqlite":
            options.UseSqlite(connectionString); break;
        case "mssql":
            options.UseSqlServer(connectionString); break;
    }
    options
    .UseSeeding((context, _) =>
    {
        Forum.SeedForum(context).Wait();
    })
    .UseAsyncSeeding(async (context, _, cancellationToken) => {
        await Forum.SeedForum(context);
    });
 
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<CrowbarUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredUniqueChars = 0;
    options.Password.RequireUppercase = false;
    options.User.RequireUniqueEmail = false;
}).AddEntityFrameworkStores<ApplicationDbContext>()
  .AddDefaultTokenProviders()
  .AddRoles<IdentityRole>();

builder.WebHost.UseKestrel(option => {
    option.AddServerHeader = false;
});

builder.Services.AddMarkdown(config =>
{
    config.ConfigureMarkdigPipeline = builder =>
    {
        builder
            .UseEmphasisExtras(Markdig.Extensions.EmphasisExtras.EmphasisExtraOptions.Default)
            .UsePipeTables()
            .DisableHtml();
    };
});

builder.Services.AddScoped<ForumActions>();

builder.Services.AddSingleton<CaptchaContainer>();

builder.Services.AddRazorPages();


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var pendingMigrations = dbContext.Database.GetPendingMigrations();
    if (pendingMigrations.Any())
        dbContext.Database.Migrate();
};

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseWaf();

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
