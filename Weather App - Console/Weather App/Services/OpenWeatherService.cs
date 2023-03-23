using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json; 
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Text.Json;

using Weather_Application.Models;
using System.Collections;

namespace Weather_Application.Services
{
    public class OpenWeatherService
    {
        HttpClient httpClient = new HttpClient();

        //Cache declaration
        ConcurrentDictionary<(double, double, string), Forecast> cachedGeoForecasts = new ConcurrentDictionary<(double, double, string), Forecast>();
        ConcurrentDictionary<(string, string), Forecast> cachedCityForecasts = new ConcurrentDictionary<(string, string), Forecast>();

        readonly string apiKey = "459a4733f190f0bda950abaa2f2d5b25";

        //Event declaration
        public event EventHandler<string> WeatherForecastAvailable;
        protected virtual void OnWeatherForecastAvailable(string message)
        {
            WeatherForecastAvailable?.Invoke(this, message);
        }

        public async Task<Forecast> GetForecastAsync(string City)
        {
            //part of cache code here to check if forecast is in Cache
            //generate an event that shows forecast was from cache

            //New key for city dictionary, a City and DateTime
            (string, string) cacheKeyCity = (City, DateTime.Now.ToString("yyyy-MM-dd HH:mm"));

            Forecast cacheCity;

            //Test the key and get a value
            var foundCache = cachedCityForecasts.TryGetValue(cacheKeyCity, out cacheCity);

            //If found, cache will be filled with forecasts
            if (foundCache)
            {
                //If not empty it will send an event message that cache was found
                if (cacheCity.Items.Count > 0)
                {
                    OnWeatherForecastAvailable($"Weather cache available for {City}");
                    return cacheCity;
                }
            }

            //https://openweathermap.org/current
            var language = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var uri = $"https://api.openweathermap.org/data/2.5/forecast?q={City}&units=metric&lang={language}&appid={apiKey}";

            Forecast forecast = await ReadWebApiAsync(uri);

            //part of event and cache code here
            //generate an event with different message if cached data

            //If cache wasn't successful
            if (cachedCityForecasts.TryAdd((City, DateTime.Now.ToString("yyyy-MM-dd HH:mm")), forecast) is not true)
            OnWeatherForecastAvailable("Cache failed, where did it go?");

            //Fire event with message about chosen city forecast
            OnWeatherForecastAvailable($"New weather forecast for {forecast.City} available");

            return forecast;

        }
        public async Task<Forecast> GetForecastAsync(double latitude, double longitude)
        {
            //part of cache code here to check if forecast in Cache
            //generate an event that shows forecast was from cache


            (double, double, string) cacheKeyGeo = (latitude, longitude, DateTime.Now.ToString("yyyy-MM-dd HH:mm"));

            Forecast cacheGeo;

            var foundCache = cachedGeoForecasts.TryGetValue(cacheKeyGeo, out cacheGeo);

            if (foundCache)
            {
                if (cacheGeo.Items.Count > 0)
                {
                    OnWeatherForecastAvailable($"Weather cache available for ({latitude}, {longitude}) ");
                    return cacheGeo;
                }
            }

            //https://openweathermap.org/current
            var language = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var uri = $"https://api.openweathermap.org/data/2.5/forecast?lat={latitude}&lon={longitude}&units=metric&lang={language}&appid={apiKey}";

            Forecast forecast = await ReadWebApiAsync(uri);

            //part of event and cache code here
            //generate an event with different message if cached data

            if (cachedGeoForecasts.TryAdd((latitude, longitude, DateTime.Now.ToString("yyyy-MM-dd HH:mm")), forecast) is not true)
                OnWeatherForecastAvailable("Cache failed");

            OnWeatherForecastAvailable($"New weather forecast for ({latitude}, {longitude}) available");

            return forecast;
        }
        private async Task<Forecast> ReadWebApiAsync(string uri)
        {
            //Read the response from the WebApi
            HttpResponseMessage response = await httpClient.GetAsync(uri);
            response.EnsureSuccessStatusCode();
            WeatherApiData wd = await response.Content.ReadFromJsonAsync<WeatherApiData>();


            //Convert WeatherApiData to Forecast using Linq.
            return new Forecast
            {
                City = wd.city.name,
                Items = wd.list.Select(x => new ForecastItem
                {
                    DateTime = UnixTimeStampToDateTime(x.dt),
                    Temperature = x.main.temp,
                    WindSpeed = x.wind.speed,
                    Description = x.weather.FirstOrDefault().description,
                    Icon = x.weather.FirstOrDefault().icon
                }).ToList(),

            };
        }
        private DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dateTime;
        }
    }
}


