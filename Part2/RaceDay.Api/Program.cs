using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RaceDayDb")));

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// PART 3 ADDITION: Image storage for event banners and profile pictures in Azure Blob Storage.
// The connection string is in User Secrets so it doesn't get pushed to GitHub
builder.Services.AddSingleton<IImageStorage, BlobImageStorage>();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".RaceDay.Session";
    options.Cookie.HttpOnly = true;     
    options.Cookie.IsEssential = true;  
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "RESTful API for the RaceDay event management system. "
                    + "Log in with POST /api/auth/login first. The session cookie is then sent "
                    + "automatically with every request made from this page."
    });

    var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile), includeControllerXmlComments: true);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// PART 3 ADDITION: HTTPS redirection is only used outside development so the MVC app
// in Docker can call the API over http without needing the dev certificate
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseSession();

app.UseAuthorization();

app.MapControllers();

app.Run();