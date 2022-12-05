using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using SB_Parser_API.MicroServices;
using static SB_Parser_API.MicroServices.SearchViaBarcode;
using SB_Parser_API.Models;
using System;
using static SB_Parser_API.MicroServices.WebAccessUtils;
using System.Drawing;
using static System.Net.Mime.MediaTypeNames;
using Image = System.Drawing.Image;

namespace SB_Parser_API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class Controllers : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<Controllers> _logger;

        public Controllers(ILogger<Controllers> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherInfo> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherInfo
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class WeatherCurrentController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherCurrentController> _logger;

        public WeatherCurrentController(ILogger<WeatherCurrentController> logger)
        {
            _logger = logger;
        }

        [HttpGet("{id:int?}"/*, Name = "GetWeatherCurrent"*/)]
        public WeatherInfo Get(int? id = null, string? Name = null)
        {
            var context = Response.HttpContext;
            foreach (var o in Request.RouteValues)
            {
                Debug.WriteLine($"{o.Key}, {o.Value}");
                Console.WriteLine($"{o.Key}, {o.Value}");
            }
            //GetShops(55.911865, 37.734596, 5.5);
            return new WeatherInfo
            {
                Date = DateTime.Now,
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)] + $"... at the moment {Name} id={id}"
            };
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class BarCodeRequestController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<BarCodeRequestController> _logger;

        public BarCodeRequestController(ILogger<BarCodeRequestController> logger)
        {
            _logger = logger;
        }

        [HttpGet("{id:int?}")]
        public WeatherInfo Get(int? id = null, string? Name = null, int? Age = 42)
        {
            var context = Response.HttpContext;
            foreach (var o in Request.RouteValues)
            {
                Debug.WriteLine($"{o.Key}, {o.Value}");
                Console.WriteLine($"{o.Key}, {o.Value}");
            }
            //HttpClient client = new HttpClient();
            return new WeatherInfo
            {
                Date = DateTime.Now,
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)] + $"... at the moment {Name} id={id} (Age={Age})"
            };
        }
    }
    public class ChatHub : Hub
    {
        public async Task Send(string message)
        {
            Console.WriteLine(message);
            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri("https://hidemy.name/ru/proxy-list/"),
                Method = HttpMethod.Get,
            };
            AddUserAgent(request);
            Image im = Image.FromFile(@"D:\BDIF\IG_Parse\Images\Image\IGimg000001.jpg");
            string base64String;
            using (MemoryStream m = new MemoryStream())
            {
                im.Save(m, im.RawFormat);
                byte[] imageBytes = m.ToArray();

                // Convert byte[] to Base64 String
                base64String = Convert.ToBase64String(imageBytes);
            }
            
            //var fB = File.ReadAllBytes(@"D:\BDIF\IG_Parse\Images\Image\IGimg000001.jpg");
            //string encodedFile = Convert.ToBase64String(fB);
            //File.WriteAllBytes(@"D:\", fB);

            await this.Clients.All.SendAsync("Send", message.Replace("Привет", "Салют"), request, base64String);
        }
    }
}