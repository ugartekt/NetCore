using Microsoft.EntityFrameworkCore;
using WebLottoActivo.DBContext;
using WebLottoActivo.Interfaces;
using WebLottoActivo.Service;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<IBCVTasa, BCVTasa>();

builder.Services.AddSingleton<IFileData, FileData>();

builder.Services.AddSingleton<ILottoActivo, LottoActivo>();

var app = builder.Build();

// Crea la tabla de patrones si no existe (la base no usa migraciones)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS ""LottoActivoPatron"" (
        ""id""     INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
        ""codigo"" TEXT NOT NULL UNIQUE,
        ""horas""  TEXT NOT NULL
    );");
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
