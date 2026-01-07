#r "HashcatGUI/bin/Release/net7.0-windows/HashcatGUI.dll"
using HashcatGUI.Services;

var hash = await BitcoinWalletExtractor.ExtractHashAsync(@"C:\Users\alina\Downloads\btc_test\hashcat-6.2.6\wallet.dat");
Console.WriteLine(hash ?? "Failed to extract hash");
