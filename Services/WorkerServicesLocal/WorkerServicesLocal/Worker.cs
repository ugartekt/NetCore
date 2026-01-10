using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using WorkerServicesLocal.Interfaces;
using WorkerServicesLocal.Models;
using WorkerServicesLocal.Service;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WorkerServicesLocal
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private IFileData _fileData;
        private IBCVTasa _bcvtasa;
        private ILottoActivo _lottoActivo;

        public string MSJ = string.Empty;

        public Worker(ILogger<Worker> logger, IServiceScopeFactory scopeFactory, IFileData fileData, IBCVTasa bcvtasa, ILottoActivo lottoActivo)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _fileData = fileData;
            _bcvtasa = bcvtasa;
            _lottoActivo = lottoActivo;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _fileData.EnsureDocumentDirectoryStructure();

                var ahora = DateTime.Now;

                // Ejecutar BCV a las 7:00 y 19:00
                if ((ahora.Hour == 7 || ahora.Hour == 19) && ahora.Minute >= 0 && ahora.Minute <= 5)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var bcvTasa = scope.ServiceProvider.GetRequiredService<IBCVTasa>();
                    await WebHtmlBCVAsync(bcvTasa);
                    _logger.LogInformation("Ejecutado WebHtmlBCVAsync a las {hora}", ahora);
                }

                if (ahora.Hour >= 8 && ahora.Hour <= 19 && ahora.Minute >= 5 && ahora.Minute <= 30)
                {
                    await WebHtmlLottoActivoAsync();
                    _logger.LogInformation("Ejecutado WebHtmlLottoActivoAsync a las {hora}", ahora);
                }
                // Esperar 1 minuto antes de volver a ejecutar
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
        }

        public async Task<TasaBCV> WebHtmlBCVAsync(IBCVTasa bcvTasa)
        {
            TasaBCV tasaBCV = new TasaBCV();
            List<TasaBCV> ListTasaBCV = new List<TasaBCV>();
            string date = string.Empty;

            try
            {
                HtmlWeb hWeb = new HtmlWeb();
                HtmlDocument hDoc = hWeb.Load("https://www.bcv.org.ve/");

                HtmlDocument doc = new HtmlDocument();

                foreach (var node in hDoc.DocumentNode.QuerySelectorAll(".views-row-last"))
                {
                    doc.LoadHtml(node.InnerHtml);
                    var docList = doc.DocumentNode.QuerySelectorAll(".recuadrotsmc");
                    var dateBCV = doc.DocumentNode.QuerySelectorAll(".dinpro");

                    foreach (var item in dateBCV)
                    {
                        string dateHtml = item.OuterHtml;
                        string buscar = "content=\"";
                        int inicio = dateHtml.IndexOf(buscar);

                        if (inicio != -1)
                        {
                            inicio += buscar.Length;
                            int fin = dateHtml.IndexOf("T", inicio);
                            date = dateHtml.Substring(inicio, fin - inicio);
                        }
                    }

                    tasaBCV = await bcvTasa.SearchTasaAsync(date);

                    if (tasaBCV.id == null)
                    {
                        foreach (var item in docList)
                        {
                            tasaBCV = new TasaBCV();
                            string currencyHtml = item.InnerText;

                            string currencyReplace = currencyHtml.Replace("\n", "").Replace("\t", "").Replace(" ", string.Empty).Trim();

                            tasaBCV.symbol = currencyReplace.Substring(0, 3);
                            tasaBCV.value = currencyReplace.Substring(3).Replace(",", ".").Trim();
                            if (tasaBCV.symbol == "USD" || tasaBCV.symbol == "EUR")
                            {
                                tasaBCV.date = date;

                                bool Success = await bcvTasa.InsertTasaAsync(tasaBCV);
                                if (Success)
                                {
                                    MSJ = $"SE REGISTRO CON EXITO LA TASA BCV MONEDA: {tasaBCV.symbol}; VALOR: {tasaBCV.value}; FECHA: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
                                    _fileData.WriteFileLog("INFO", "InfoBCV", MSJ);
                                }
                                else
                                {
                                    MSJ = $"NO SE PUDO REGISTRAR LA TASA BCV MONEDA: {tasaBCV.symbol}; VALOR: {tasaBCV.value}; FECHA: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
                                    _fileData.WriteFileLog("ERROR", "ErrorBCV", MSJ);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MSJ = $"HA OCURRIDO UIN ERROR INTERNO: {ex.Message} FECHA: {DateTime.Now.ToString("yyyy-MM--dd HH:mm:ss")}";
                _fileData.WriteFileLog("ERROR", "ErrorBCV", MSJ);
            }
            await Task.Delay(TimeSpan.FromMinutes(1));

            return tasaBCV;
        }

        public async Task<bool> WebHtmlLottoActivoAsync()
        {
            string responseBody = string.Empty;
            LottoActivoResultado lottoActivoResultado = new LottoActivoResultado();
            LottoActivoResultado lottoActivoResultadoAnterior = new LottoActivoResultado();
            try
            {
                HtmlWeb hWeb = new HtmlWeb();
                HtmlDocument hDoc = new HtmlDocument();

                hDoc = hWeb.Load($"https://lotoven.com/animalitos/");

                HtmlDocument doc = new HtmlDocument();
                HtmlDocument lottoActivo = new HtmlDocument();

                int[] rueda = { 38, 1, 13, 36, 24, 3, 15, 34, 22, 5, 17, 32, 20, 7, 11, 30, 26, 9, 28, 37, 2, 14, 35, 23, 4, 16, 33, 21, 6, 18, 31, 19, 8, 12, 29, 25, 10, 27 };
                int count = 0;

                var divLottoActivo = hDoc.GetElementbyId("lottoactivo");

                string text = divLottoActivo.InnerText.ToString();

                string[] parte = text.Split(",");
                string raw = parte[1];
                string[] partes = raw.Split(new[] { " de " }, StringSplitOptions.RemoveEmptyEntries);


                string day = partes[0].Trim();
                string monthText = partes[1].Trim().ToLower();
                string year = partes[2].Trim().Substring(0, 4);

                Dictionary<string, string> meses = new Dictionary<string, string>
                    {
                        { "january", "01" }, { "february", "02" }, { "march", "03" },
                        { "april", "04" }, { "may", "05" }, { "june", "06" },
                        { "july", "07" }, { "august", "08" }, { "september", "09" },
                        { "october", "10" }, { "november", "11" }, { "december", "12" }
                    };

                string month = meses.ContainsKey(monthText) ? meses[monthText] : "??";
                string date = $"{year}-{month}-{day.PadLeft(2, '0')}";

                foreach (var node in divLottoActivo.QuerySelectorAll(".invest-table-area"))
                {
                    doc.LoadHtml(node.InnerHtml);
                    var docList = doc.DocumentNode.QuerySelectorAll(".counter-wrapper");

                    foreach (var item in docList)
                    {
                        string valueHtml = item.InnerText;

                        string stringResult = valueHtml.Replace("\n", "").Replace("\t", "").Replace(" ", string.Empty).Trim();

                        var match = Regex.Match(stringResult, @"^(\d+)([A-Za-z]+)(\d{2}:\d{2}[AP]M)$");
                        if (match.Success)
                        {
                            string hora = match.Groups[3].ToString();

                            lottoActivoResultado = await _lottoActivo.SearchResultadosAsync(date, hora);

                            if (lottoActivoResultado.id != null)
                            {
                                continue;
                            }
                            string numeroString = Convert.ToString(match.Groups[1].Value);

                            numeroString = (numeroString == "0") ? "37" : (numeroString == "00") ? "38" : numeroString;

                            int numero = Convert.ToInt32(numeroString);

                            lottoActivoResultadoAnterior = await _lottoActivo.UltimoAnimalitoDesplazamientoAsync();

                            var desplazamiento1 = CalcularDesplazamiento(rueda, (int)lottoActivoResultadoAnterior.lottoActivoAnimalId, numero);

                            lottoActivoResultado = new LottoActivoResultado();

                            lottoActivoResultado.lottoActivoAnimalId = numero;
                            lottoActivoResultado.desplazamiento = desplazamiento1.menor;
                            lottoActivoResultado.fecha = date;
                            lottoActivoResultado.hora = Convert.ToString(match.Groups[3]);

                            bool Success = await _lottoActivo.InsertResultadoAsync(lottoActivoResultado);

                        }

                    }
                }

            }
            catch (Exception ex)
            {
                MSJ = $"HA OCURRIDO UN ERROR INTERNO: {GetFullExceptionDetails(ex)} FECHA: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                _fileData.WriteFileLog("ERROR", "ErrorBCV", MSJ);
            }

            return true;
        }

        public static (int izquierda, int derecha, int menor) CalcularDesplazamiento(int[] rueda, int anterior, int actual)
        {
            int total = rueda.Length;

            int posAnterior = Array.IndexOf(rueda, anterior);
            int posActual = Array.IndexOf(rueda, actual);

            if (posAnterior == -1 || posActual == -1)
                throw new Exception("Animalito no encontrado en la rueda");

            int derecha = (posActual - posAnterior + total) % total;
            int izquierda = (posAnterior - posActual + total) % total;
            int menor = Math.Min(derecha, izquierda);

            return (izquierda, derecha, menor);
        }

        public static string GetFullExceptionDetails(Exception ex)
        {
            var sb = new System.Text.StringBuilder();
            int level = 0;
            while (ex != null)
            {
                sb.AppendLine($"[Excepción nivel {level}]");
                sb.AppendLine($"Mensaje: {ex.Message}");
                sb.AppendLine($"Tipo: {ex.GetType().FullName}");
                sb.AppendLine("StackTrace:");
                sb.AppendLine(ex.StackTrace);
                sb.AppendLine(new string('-', 80));
                ex = ex.InnerException;
                level++;
            }
            return sb.ToString();
        }






    }
}
