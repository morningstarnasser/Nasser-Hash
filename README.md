# Nasser-Hash

A modern WPF GUI for Hashcat with Smart Attack capabilities for Bitcoin wallet recovery.

![.NET](https://img.shields.io/badge/.NET-7.0-blue)
![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey)
![License](https://img.shields.io/badge/License-MIT-green)

## Features

- **Smart Attack**: Intelligent attack profiles based on wallet age analysis
- **Wallet Analyzer**: Analyze Bitcoin wallet.dat files to determine age and extract metadata
- **GPU Optimization**: Multiple GPU profiles (Conservative, Balanced, Performance, Insane)
- **Session Management**: Save and restore attack sessions
- **Live Monitoring**: Real-time GPU temperature, utilization, and speed display
- **Queue Processing**: Batch process multiple wallets
- **Success Probability**: Estimated success rates based on wallet era

## Requirements

- Windows 10/11
- .NET 7.0 Runtime
- Hashcat 6.x
- NVIDIA/AMD GPU with OpenCL support

## Installation

1. Download and install [Hashcat](https://hashcat.net/hashcat/)
2. Download the latest release of Nasser-Hash
3. Configure the Hashcat path in Settings

## Usage

### Smart Attack
1. Go to **Smart Attack** tab
2. Add wallet.dat files to the queue
3. Select GPU profile
4. Click **Start Queue**

### Wallet Analyzer
1. Go to **Wallet Analyzer** tab
2. Select a wallet.dat file
3. View analysis results including:
   - Estimated wallet age
   - Bitcoin addresses
   - Hashcat-ready hash

## GPU Profiles

| Profile | Description | Temp Limit |
|---------|-------------|------------|
| Conservative | Safe for older GPUs | 85°C |
| Balanced | Good performance, safe temps | 90°C |
| Performance | Maximum speed | 95°C |
| Insane | All-out attack | 100°C |

## Attack Profiles

The Smart Attack system automatically selects attack strategies based on wallet age:

- **Very Old (2009-2012)**: 70% success probability - Simple passwords common
- **Old (2012-2014)**: 55% success probability
- **Middle (2014-2017)**: 35% success probability
- **Recent (2017-2020)**: 20% success probability
- **Modern (2020+)**: 10% success probability

## Building from Source

```bash
git clone https://github.com/yourusername/Nasser-Hash.git
cd Nasser-Hash/HashcatGUI
dotnet build --configuration Release
```

## Screenshots

*Coming soon*

## License

MIT License - See LICENSE file for details.

## Disclaimer

This tool is intended for legitimate password recovery purposes only. Only use on wallets you own or have explicit permission to access. The developers are not responsible for any misuse.

## Credits

- Built with [Material Design In XAML](http://materialdesigninxaml.net/)
- Powered by [Hashcat](https://hashcat.net/hashcat/)
