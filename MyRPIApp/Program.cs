using System;
using System.Device.Gpio;
using System.Net.Http;
using System.Device.Spi;
using Iot.Device.Mfrc522;
using Iot.Device.DHTxx;
using Iot.Device.Rfid;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

const int RED_LED_PIN = 12;
const int YELLOW_LED_PIN = 17;
const int GREEN_LED_PIN = 18;

var controller = new GpioController();

controller.OpenPin(RED_LED_PIN, PinMode.Output);
controller.OpenPin(GREEN_LED_PIN, PinMode.Output);
controller.OpenPin(YELLOW_LED_PIN, PinMode.Output);

bool isGreenLedOn = false;
bool isRedLedOn = false;
bool isYellowLedOn = false;

void ChangeLedsState(bool isGreenOn, bool isRedOn, bool isYellowOn)
{
  isGreenLedOn = isGreenOn;
  isRedLedOn = isRedOn;
  isYellowLedOn = isYellowOn;
  controller.Write(GREEN_LED_PIN, isGreenLedOn);
  controller.Write(RED_LED_PIN, isRedLedOn);
  controller.Write(YELLOW_LED_PIN, isYellowLedOn);
}

ChangeLedsState(false, false, true);

using (Dht11 dht = new Dht11(4))
{
  while (true)
  {
    Console.WriteLine("Reading temperature");
    var temperature = dht.Temperature;
    Console.WriteLine($"Temperature: {temperature.DegreesCelsius:0.#}\u00b0C");
    Thread.Sleep(1000);
  }
}


async void LogCardEvent(string uri, string uuid)
{
  using (var client = new HttpClient())
  {
    var endpoint = new Uri("http://smart-home-web-app.azurewebsites.net/api/card-reader/3ed94e90-4d51-4566-8623-3c7343d64253");
    var result = client.GetAsync(endpoint).Result.Content.ReadAsStringAsync().Result;
    Console.WriteLine(result);

    long unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    var body = "";

    var response = await client.PostAsync(
        uri,
        new StringContent(body, Encoding.UTF8, "application/json"));

    Console.WriteLine(body);
    Console.WriteLine(response);
  }
}

const string smartHomeUrl = "http://smart-home-web-app.azurewebsites.net/api/card-reader";

LogCardEvent(smartHomeUrl, "");

string GetCardId(Data106kbpsTypeA card) => Convert.ToHexString(card.NfcId);

GpioController gpioController = new GpioController();
int pinReset = 21;

SpiConnectionSettings connection = new(0, 0);
connection.ClockFrequency = 10_000_000;

var source = new CancellationTokenSource();
var token = source.Token;

var task = Task.Run(() => ReadData(token), token);

Console.WriteLine("Press any key to close to application.");
Console.ReadKey();
source.Cancel();

await task;

void ReadData(CancellationToken cancellationToken)
{
  Console.WriteLine("Work log is currently running.");
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

        ChangeLedsState(true, false, false);

        if (res)
        {
          ChangeLedsState(false, false, true);
          var cardId = GetCardId(card);
          Console.WriteLine(cardId);
          //Call AP
          Thread.Sleep(2000);
          ChangeLedsState(true, false, false);
        }
      }

      Thread.Sleep(1000);
    }
    catch (System.Exception ex)
    {
      ChangeLedsState(false, true, false);
      Console.WriteLine("Error with device");
      Console.WriteLine(ex.Message);
      throw;
    }
  } while (active);

  Console.WriteLine("Thank you for using work-log");
}

