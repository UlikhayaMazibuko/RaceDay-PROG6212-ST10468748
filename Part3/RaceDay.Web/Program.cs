using RaceDay.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// The website has its own session that stores the user's id, role, name and the API session cookie
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".RaceDay.Web.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

builder.Services.AddHttpContextAccessor();

// All the data comes from the Part 2 API. The API address is in appsettings.json so it can be
// changed for Docker without changing the code
builder.Services.AddHttpClient<ApiClient>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"] ?? "https://localhost:7015/");
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    // UseCookies is off because ApiClient adds each user's API cookie itself
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseCookies = false });

var app = builder.Build();

// Shows our own error page instead of the raw error
app.UseExceptionHandler("/Home/Error");
app.UseStatusCodePagesWithReExecute("/Home/Error", "?code={0}");

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
