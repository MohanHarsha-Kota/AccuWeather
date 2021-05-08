using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccuWeatherServiceFetcher
{
    class Program
    {
        static void Main(string[] args)
        {
            WeatherForecastData Obj = new WeatherForecastData();
            bool result = Obj.DataFetcher();
            Console.WriteLine(result);
        }

    }
}
