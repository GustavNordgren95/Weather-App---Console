using System;
using System.Collections.Generic;

namespace Weather_Application.Models
{
    public class Forecast
    {
        public string City { get; set; }
        public List<ForecastItem> Items { get; set; }
    }
}
