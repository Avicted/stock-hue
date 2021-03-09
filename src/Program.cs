using System;
using System.Threading.Tasks;
using Q42.HueApi;
using Q42.HueApi.Interfaces;
using Q42.HueApi.ColorConverters;
using Q42.HueApi.ColorConverters.Gamut;
using Q42.HueApi.ColorConverters.HSB;
using YahooFinanceApi;

namespace stock_hue {
    class Program {
        public static ILocalHueClient client;
        static async Task Main(string[] args) {
            Console.WriteLine("stock-hue started");
            IBridgeLocator locator = new HttpBridgeLocator(); // Or: LocalNetworkScanBridgeLocator, MdnsBridgeLocator, MUdpBasedBridgeLocator
	        var bridges = await locator.LocateBridgesAsync(TimeSpan.FromSeconds(5));
            string bridgeIpAddress = "";

            foreach (var bridge in bridges) {
                Console.WriteLine($"bridge.IpAddress: {bridge.IpAddress}");
                bridgeIpAddress = bridge.IpAddress;
            }

            if (bridgeIpAddress.Length <= 0) {
                throw new Exception("Could not find any Hue bridge, is this computer on the same LAN as the Hue bridge?");
            }

            client = new LocalHueClient(bridgeIpAddress);

            // Make sure the user has pressed the button on the bridge before calling RegisterAsync
            // It will throw an LinkButtonNotPressedException if the user did not press the button
            Console.WriteLine("Press ENTER when you have pressed the button on the bridge.");
            do {
                while (!Console.KeyAvailable) {}       
            } while (Console.ReadKey(true).Key != ConsoleKey.Enter);

            var appKey = await client.RegisterAsync("stock-hue", "stock-hue-runner");
            Console.WriteLine($"appKey: {appKey}");

            while (true)
            {
                (double regularMarketOpen, double regularMarketPrice) = await GetStockData("GME");
                if (regularMarketPrice > regularMarketOpen) {
                    await SetLightsToColor(new RGBColor("00FF00"));
                } else {
                    await SetLightsToColor(new RGBColor("FF0000"));
                }
                await Task.Delay(3000);
            }
        }

        public static async Task<Tuple<double, double>> GetStockData(string ticker)
        {
            // You could query multiple symbols with multiple fields through the following steps:
            var securities = await Yahoo.Symbols(ticker).Fields(Field.Symbol, Field.RegularMarketPrice, Field.RegularMarketOpen).QueryAsync();
            var stock = securities[ticker];
            var regularMarketPrice = stock.RegularMarketPrice;
            var regularMarketOpen = stock.RegularMarketOpen;
            string color = regularMarketPrice > regularMarketOpen ? "green" : "red";
            Console.WriteLine($"regularMarketOpen: {regularMarketOpen} regularMarketPrice: {regularMarketPrice} color -> {color}");
            return new Tuple<double, double>(regularMarketOpen, regularMarketPrice);
        }

        public static async Task SetLightsToColor(RGBColor color)
        {
            var command = new LightCommand();
            command.TurnOn().SetColor(color);            
            await client.SendCommandAsync(command);
        }
    }
}
