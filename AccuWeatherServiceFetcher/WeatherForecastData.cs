using System;
using System.IO;
using System.Linq;
using System.Configuration;
using System.Net;
using Newtonsoft.Json;
using AccuWeatherServiceFetcher.Models;
using System.Data;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Collections.Generic;
using Azure.Security.KeyVault.Secrets;
using Azure.Identity;

namespace AccuWeatherServiceFetcher
{
    class WeatherForecastData
    {

        public bool DataFetcher()
        {
            try
            {
                string filePath = string.Empty;
                string webjobPath = Environment.GetEnvironmentVariable("WEBJOBS_PATH");
                string locationkeysecret = string.Empty;
                string tempkeysecret = string.Empty;
                string sendgridAPIKey = string.Empty;

                if (string.IsNullOrWhiteSpace(webjobPath)) // Handle dev environment
                {
                    filePath = ConfigurationManager.AppSettings["LocationsPathLocal"];
                    locationkeysecret = Environment.GetEnvironmentVariable("AccuWeatherKey1MH", EnvironmentVariableTarget.User);
                    tempkeysecret = Environment.GetEnvironmentVariable("AccuWeatherKey2HK", EnvironmentVariableTarget.User);
                    sendgridAPIKey = Environment.GetEnvironmentVariable("SendGridAPIKey", EnvironmentVariableTarget.User);
                }
                else //Handle Azure environment
                {
                    //Keys are retrieved from Azure KeyVault using the concept of Key Vault References
                    filePath = ConfigurationManager.AppSettings["LocationPathAzure"];
                    locationkeysecret = Environment.GetEnvironmentVariable("AccuWeatherKey1MH");
                    tempkeysecret = Environment.GetEnvironmentVariable("AccuWeatherKey2HK");
                    sendgridAPIKey = Environment.GetEnvironmentVariable("SendGridAPIKey");
                }


                using (StreamReader LocationData = new StreamReader(new FileStream(filePath, FileMode.Open, FileAccess.Read)))
                {
                    string line = string.Empty;
                    DataTable dt = new DataTable();
                    dt.Columns.Add("Service Location", typeof(string));
                    dt.Columns.Add("Forecast Low", typeof(string));
                    while ((line = LocationData.ReadLine()) != null)
                    {
                        string[] values = line.Split(',');
                        string locKey = MakeARequestAsync(values, locationkeysecret);
                        string temperature = MakeARequestAsync(values, tempkeysecret, locKey);
                        Console.WriteLine(temperature);
                        DataRow row = dt.NewRow();
                        row[0] = values[0];
                        row[1] = temperature;
                        dt.Rows.Add(row);
                    }
                    SendAnEmail(dt, sendgridAPIKey);
                }
                return true;
            }
            catch(Exception Obj)
            {
                Console.WriteLine(Obj.Message);
                return false;
            }
        }

        private void SendAnEmail(DataTable dt, string sendgridAPIKey)
        {
            try
            {
                var msg = new SendGridMessage();

                msg.SetFrom(new EmailAddress("harshakota@azure.com", "Weather Forecast Team"));

                var recipients = new List<EmailAddress>
                {
                        new EmailAddress("mohanharsha94@gmail.com", "Mohan Harsha Kota"),
                };
                msg.AddTos(recipients);
                string mailBody = "<table style='border-color: #000000; float: left; border-collapse:collapse;'" +
                    "cellpadding='3' border='1'><tr><th>" + "ServiceLocation" + "</th><th>" + "Forecasted Low" + "</th></tr>";
                foreach (DataRow row in dt.Rows)
                {
                    mailBody += "<tr><td>" + row[0] + "</td><td>" + row[1] + "</td></tr>";
                }

                mailBody += "</table>";

                string subj = string.Format("Weather Forecast For {0}", DateTime.Now.Date.ToLongDateString());
                msg.SetSubject(subj);
                msg.AddContent(MimeType.Html, mailBody);

                var client = new SendGridClient(sendgridAPIKey);
                client.SendEmailAsync(msg).Wait();

            }
            catch(Exception Obj)
            {
                Console.WriteLine(Obj.Message);
            }

        }

        private static string MakeARequestAsync(string[] values, string apikey, string locKey = null)
        {
            if (locKey == null)
            {
                string locKeyUrl = string.Format(ConfigurationManager.AppSettings["AccuWeatherURL1"], apikey, values[1], values[2]);
                HttpWebRequest req1 = (HttpWebRequest)WebRequest.Create(locKeyUrl);
                req1.Method = "GET";
                req1.UserAgent = "CompanyOne";
                HttpWebResponse res1 = (HttpWebResponse)req1.GetResponse();
                Stream res1Stream = res1.GetResponseStream();
                StreamReader res1Reader = new StreamReader(res1Stream);
                var res1Result = res1Reader.ReadToEnd();
                var res1Output = JsonConvert.DeserializeObject<LocationKeyClass>(res1Result);
                return res1Output.Key;
            }
            else
            {
                string forecastUrl = string.Format(ConfigurationManager.AppSettings["AccuWeatherURL2"], locKey, apikey);
                HttpWebRequest req2 = (HttpWebRequest)WebRequest.Create(forecastUrl);
                req2.Method = "GET";
                req2.UserAgent = "CompanyTwo";
                HttpWebResponse res2 = (HttpWebResponse)req2.GetResponse();
                Stream res2Stream = res2.GetResponseStream();
                StreamReader res2Reader = new StreamReader(res2Stream);
                var res2Result = res2Reader.ReadToEnd();
                var res2Output = JsonConvert.DeserializeObject<ForecastClass>(res2Result);
                DailyForecast fObj = res2Output.DailyForecasts.FirstOrDefault();
                if(fObj != null)
                {
                    return fObj.Temperature.Minimum.Value.ToString();
                }
                else
                {
                    return "Null";
                }
            }
        }
		
    }
}
