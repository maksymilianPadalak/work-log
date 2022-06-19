using System;
using System.Device.Gpio;
using System.Net.Http;
using System.Device.Spi;
using Iot.Device.Mfrc522;
using Iot.Device.Rfid;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

const int RED_LED_PIN = 12;
const int GREEN_LED_PIN = 18;

var controller = new GpioController();

controller.OpenPin(RED_LED_PIN, PinMode.Output);
controller.OpenPin(GREEN_LED_PIN, PinMode.Output);

bool isGreenPinOn = true;
bool isRedPinOn = true;

controller.Write(GREEN_LED_PIN, isGreenPinOn);
controller.Write(RED_LED_PIN, isRedPinOn);


using (var client = new HttpClient())
{
  var endpoint = new Uri("http://smart-home-web-app.azurewebsites.net/api/card-reader/3ed94e90-4d51-4566-8623-3c7343d64253");
  var result = client.GetAsync(endpoint).Result.Content.ReadAsStringAsync().Result;
  Console.WriteLine(result);

  string body = "{'uuid': '','createdAt':13433245}";

  var response = await client.PostAsync(
      "http://smart-home-web-app.azurewebsites.net/api/card-reader",
       new StringContent(body, Encoding.UTF8, "application/json"));

  Console.WriteLine(response);
}

string GetCardId(Data106kbpsTypeA card) => Convert.ToHexString(card.NfcId);

GpioController gpioController = new GpioController();
int pinReset = 21;

SpiConnectionSettings connection = new(0, 0);
connection.ClockFrequency = 10_000_000;

var source = new CancellationTokenSource();
var token = source.Token;

var task = Task.Run(() => ReadData(token), token);

Console.WriteLine("Any key to close.");
Console.ReadKey();

source.Cancel();

await task;

void ReadData(CancellationToken cancellationToken)
{
  Console.WriteLine("Task run.");
  var active = true;

  do
  {
    if (cancellationToken.IsCancellationRequested)
    {
      active = false;
    }

    try
    {
      using (SpiDevice spi = SpiDevice.Create(connection))
      using (MfRc522 mfrc522 = new(spi, pinReset, gpioController, false))
      {
        Data106kbpsTypeA card;
        var res = mfrc522.ListenToCardIso14443TypeA(out card, TimeSpan.FromSeconds(2));

        if (res)
        {
          var cardId = GetCardId(card);
          Console.WriteLine(cardId);
        }
      }

      Thread.Sleep(1000);
    }
    catch (System.Exception ex)
    {
      Console.WriteLine("Error with device");
      Console.WriteLine(ex.Message);
      throw;
    }
  } while (active);

  Console.WriteLine("Task done.");
}

