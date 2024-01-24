using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public class FunCaptcha
{
    // Asenkron GET isteği gönderen metot
    static async Task<string> SendGetRequest(string url)
    {
        using (HttpClient client = new HttpClient())
        {
            HttpResponseMessage response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                Console.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                return null;
            }
        }
    }

    // Captcha token'ını almak için ana metot
    public async Task<string> GetCaptcha(string apiKey, string funCaptchaSiteKey)
    {
        string result = "";
        int maxAttempts = 60; // Maksimum deneme sayısı
        int attempts = 0;

        // Belirtilen maksimum deneme sayısına ulaşana kadar veya başarısız olana kadar döngü
        while (attempts < maxAttempts)
        {
            // Captcha görevini oluşturmak için FunCaptcha API'ye GET isteği gönder
            string createTaskingUrl = $"https://fun.vocopus.com/FunCaptcha?key={apiKey}&FunCaptchaSiteKey={funCaptchaSiteKey}";
            string createTaskingResponse = await SendGetRequest(createTaskingUrl);
            JObject jsonResponse = JObject.Parse(createTaskingResponse);

            // Hata kontrolü
            if (jsonResponse["error"] != null && jsonResponse["error"].ToString() == "Yetersiz bakiye")
            {
                Console.WriteLine("Hata: Yetersiz bakiye. İşlemler iptal edildi.");
                return "Yetersiz bakiye";
            }
            if (jsonResponse["error"] != null && jsonResponse["error"].ToString() == "Kullanıcı bulunamadı")
            {
                Console.WriteLine("Hata: Api Key Hatalı");
                return "Api Key Hatalı";
            }

            string orderIDValue = jsonResponse["orderID"]?.ToString();

            // Oluşturulan görev başarılıysa, görevin durumunu kontrol et
            if (!string.IsNullOrEmpty(orderIDValue))
            {
                while (true)
                {
                    string orderID = orderIDValue;
                    string checkTaskingUrl = $"https://fun.vocopus.com/FunCaptcha?key={apiKey}&orderID={orderID}";

                    // Captcha görevinin durumunu kontrol etmek için FunCaptcha API'ye GET isteği gönder
                    string checkTaskingResponse = await SendGetRequest(checkTaskingUrl);
                    JObject checkTaskingJson = JObject.Parse(checkTaskingResponse);

                    string status = checkTaskingJson["order"]?["status"]?.ToString();

                    // Görev başarılıysa, captcha token'ını al ve döngüyü sonlandır
                    if (status == "Success")
                    {
                        string token = checkTaskingJson["order"]?["token"].ToString();
                        result = token;
                        Console.WriteLine("Success! Exiting the loop.");
                        return result;
                    }
                    // Görev başarısızsa, döngüyü sonlandır
                    else if (status == "Fail")
                    {
                        Console.WriteLine("Fail status received. Exiting the loop.");
                        break;
                    }

                    // Belirli bir süre beklet (isteğe bağlı olarak değiştirilebilir)
                    await Task.Delay(1000);
                }
            }

            attempts++;

            // Belirli bir süre beklet (isteğe bağlı olarak değiştirilebilir)
            await Task.Delay(1000);
        }

        // Maksimum deneme sayısına ulaşıldığında veya hata durumunda sonuç
        return result;
    }

    // Uygulamanın giriş noktası
    public static async Task Main(string[] args)
    {
        // API anahtarınızı ve Twitter site anahtarınızı girin
        string apiKey = "Your_Api_Key";
        string twitterSiteKey = "2CB16598-CB82-4CF7-B332-5990DB66F3AB";

        // FunCaptcha sınıfından bir örnek oluşturun
        FunCaptcha funCaptcha = new FunCaptcha();

        // Captcha token'ını al
        string captchaResponse = await funCaptcha.GetCaptcha(apiKey, twitterSiteKey);

        // Captcha token'ını kullanarak başka işlemler yapabilirsiniz
        // ...
    }
}
