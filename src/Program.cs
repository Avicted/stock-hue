using System;
using System.Threading.Tasks;
using Q42.HueApi;
using Q42.HueApi.Interfaces;
using Q42.HueApi.ColorConverters;
using Q42.HueApi.ColorConverters.Gamut;
using Q42.HueApi.ColorConverters.HSB;
using YahooFinanceApi;
using System.Collections.Generic;
using Q42.HueApi.Models.Bridge;
using System.Linq;

namespace stock_hue {
    class Program {
        public static ILocalHueClient client;
        public static IEnumerable<Light> lights;

        public enum Color {
            Red,
            Red_alt,
            Green,
            Green_alt,
        };

        public static Dictionary<Color, RGBColor> colors = new Dictionary<Color, RGBColor>() {
            {Color.Red, new RGBColor(255, 0, 0)},
            {Color.Red_alt, new RGBColor(255, 100, 0)},
            {Color.Green, new RGBColor(0, 255, 0)},
            {Color.Green_alt, new RGBColor(0, 255, 100)},
        };

        static async Task Main(string[] args) {
            await SetupClient();
            await RegisterAppWithBridge();
            await GetAllLights();

            while (true) {
                (double regularMarketOpen, double regularMarketPrice) = await GetStockData("GME");

                // @Test: simulate that the price changes:
                /* Random r = new Random();
                int range = 50;
                double rDouble = r.NextDouble() * range;
                regularMarketPrice = regularMarketPrice - rDouble; */
                // End of test

                if (regularMarketPrice > regularMarketOpen) {
                    await SetLightsToColor(Color.Green);
                } else {
                    await SetLightsToColor(Color.Red);
                }
                await Task.Delay(3000);
            }
        }

        public static async Task SetupClient() {
            Console.WriteLine("stock-hue started");

            IBridgeLocator locator = new HttpBridgeLocator(); // Or: LocalNetworkScanBridgeLocator, MdnsBridgeLocator, MUdpBasedBridgeLocator
	        IEnumerable<LocatedBridge> bridges = await locator.LocateBridgesAsync(TimeSpan.FromSeconds(5));
            string bridgeIpAddress = "";

            foreach (LocatedBridge bridge in bridges) {
                Console.WriteLine($"bridge.IpAddress: {bridge.IpAddress}");
                bridgeIpAddress = bridge.IpAddress;
            }

            if (bridgeIpAddress.Length <= 0) {
                throw new Exception("Could not find any Hue bridge, is this computer on the same LAN as the Hue bridge?");
            }

            client = new LocalHueClient(bridgeIpAddress);
        }

        public static async Task RegisterAppWithBridge() {
            // Make sure the user has pressed the button on the bridge before calling RegisterAsync
            // It will throw an LinkButtonNotPressedException if the user did not press the button
            Console.WriteLine("Press ENTER when you have pressed the button on the bridge.");
            do {
                while (!Console.KeyAvailable) {}       
            } while (Console.ReadKey(true).Key != ConsoleKey.Enter);

            string appKey = await client.RegisterAsync("stock-hue", "stock-hue-client");
            Console.WriteLine($"appKey: {appKey}");
        }

        public static async Task GetAllLights() {
            lights = await client.GetLightsAsync();
            Console.WriteLine($"Lights found:\n====================================");

            foreach (Light light in lights) {
                Console.WriteLine($"id: {light.Id}\nname: {light.Name}\nproductId: {light.ProductId}");
                Console.WriteLine("====================================");
            }
        }

        public static async Task<Tuple<double, double>> GetStockData(string ticker) {
            IReadOnlyDictionary<string, Security> securities = await Yahoo.Symbols(ticker).Fields(Field.Symbol, Field.RegularMarketPrice, Field.RegularMarketOpen).QueryAsync();
            Security stock = securities[ticker];
            double regularMarketOpen = stock.RegularMarketOpen;
            double regularMarketPrice = stock.RegularMarketPrice;
            string color = regularMarketPrice > regularMarketOpen ? "green" : "red";
            Console.WriteLine($"regularMarketOpen: {regularMarketOpen} regularMarketPrice: {regularMarketPrice} color -> {color}");

            return new Tuple<double, double>(regularMarketOpen, regularMarketPrice);
        }

        public static async Task SetLightsToColor(Color color) {
            LightCommand command = new LightCommand();
            command.Effect = Effect.None;
            command.TurnOn();

            int i = 0;
            foreach (Light light in lights) {
                RGBColor chosenColor = new RGBColor();

                if (i % 2 == 0) {
                    if (color == Color.Red) {
                        chosenColor = colors[Color.Red_alt];
                    } else if (color == Color.Green) {
                        chosenColor = colors[Color.Green_alt];
                    }
                } else {
                    if (color == Color.Red) {
                        chosenColor = colors[Color.Red];
                    } else if (color == Color.Green) {
                        chosenColor = colors[Color.Green];
                    }
                }

                command.SetColor(chosenColor);
                List<string> selectedLight = lights.Select(l => l.Id).Where(id => id == light.Id).ToList();
                await client.SendCommandAsync(command, selectedLight);
                i++;
            }
        }
    }
}
