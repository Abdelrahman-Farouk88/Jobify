using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Jobify.Web.Data;
using Jobify.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));


builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddScoped<ResumeStorageService>();
builder.Services.AddScoped<JobMatchService>();
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddDefaultUI()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        IConfigurationSection googleAuthNSection = builder.Configuration.GetSection("Authentication:Google");
        options.ClientId = googleAuthNSection["ClientId"] ?? "YOUR_GOOGLE_CLIENT_ID";
        options.ClientSecret = googleAuthNSection["ClientSecret"] ?? "YOUR_GOOGLE_CLIENT_SECRET";
    })
    .AddFacebook(options =>
    {
        IConfigurationSection facebookAuthNSection = builder.Configuration.GetSection("Authentication:Facebook");
        options.AppId = facebookAuthNSection["AppId"] ?? "YOUR_FACEBOOK_APP_ID";
        options.AppSecret = facebookAuthNSection["AppSecret"] ?? "YOUR_FACEBOOK_APP_SECRET";
    });

builder.Services.AddControllersWithViews();

var app = builder.Build();

Directory.CreateDirectory(Path.Combine(app.Environment.WebRootPath, "uploads", "resumes"));

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
    await IdentitySeeder.SeedRolesAsync(scope.ServiceProvider);
    await PlatformSeeder.SeedAsync(scope.ServiceProvider);
}

_ = Task.Run(async () =>
{
    try
    {
        await using var scope = app.Services.CreateAsyncScope();
        var jobMatchService = scope.ServiceProvider.GetRequiredService<JobMatchService>();
        await jobMatchService.TrainAsync();
    }
    catch
    {
        // Training is a background optimization; the app should still start if it fails.
    }
});

app.Run();
