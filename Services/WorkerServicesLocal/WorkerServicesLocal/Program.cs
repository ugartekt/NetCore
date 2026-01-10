using Microsoft.EntityFrameworkCore;
using WorkerServicesLocal;
using WorkerServicesLocal.DBContext;
using WorkerServicesLocal.Service;
using WorkerServicesLocal.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = Host.CreateDefaultBuilder(args)
    .UseWindowsService()
    .ConfigureLogging(logging =>
    {
        logging.AddEventLog();
    })
    .ConfigureServices((context, services) =>
    {
        services.AddHostedService<Worker>();

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(context.Configuration.GetConnectionString("DefaultConnection")));

        services.AddSingleton<IBCVTasa, BCVTasa>();
        services.AddSingleton<IFileData, FileData>();
        services.AddSingleton<ILottoActivo, LottoActivo>();
    })
    .Build();

host.Run();