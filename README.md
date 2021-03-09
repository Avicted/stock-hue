# stock-hue

### Changes the color of all Philips Hue lights connected to a bridge when the stock price of GME changes.

Uses the following packages:

-   Q42.HueApi and
-   YahooFinanceApi.

---

## Color logic

```
Green = regularMarketPrice >  regularMarketOpen
Red   = regularMarketPrice <= regularMarketOpen
```

---

## Flow

1. The application will search for the first Hue bridge that it can find in the same LAN as the computer running this program.

2. The application will generate a new appKey with the bridge.

3. The applications checks the price every 3 seconds and compares it to the opening price (regularMarketOpen).

---

## Production build

Requires the dotnet cli or you could start the VSCode development container and build from there.

```bash
# Linux
dotnet publish -r win-x64 -p:PublishSingleFile=true --self-contained true -c Release

# Windows
dotnet publish -r win-x64 -p:PublishSingleFile=true --self-contained true -c Release
```
