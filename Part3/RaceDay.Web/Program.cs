using RaceDay.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// The website keeps its own session: who is logged in, their role, their name, and the
// API's session cookie so every API call is made as that user.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".RaceDay.Web.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

builder.Services.AddHttpContextAccessor();

// All data comes from the Part 2 API. The base address comes from configuration so the same
// code works locally (https://localhost:7015/) and inside Docker (http://host.docker.internal:5106/).
builder.Services.AddHttpClient<ApiClient>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"] ?? "https://localhost:7015/");
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    // Cookies are handled by ApiClient per user, so the shared handler must not keep its own.
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseCookies = false });

var app = builder.Build();

// A friendly error page in every environment, so users never see a raw exception.
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
