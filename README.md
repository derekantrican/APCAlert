# APCAlert

A Discord alert event for APC branded UPS battery backups.

*Note that while .NET 6 & the APC UPS Daemon are both cross-platform, this project is built with Windows in mind as it creates `.bat` files and uses the Windows apcupsd installation directory. For Linux servers, check out https://github.com/bp1313/apcupsd-discord, which inspired this project.*

![image](https://user-images.githubusercontent.com/1558019/191369609-73603587-bdee-4e25-bd2b-0db42c977461.png)

![image](https://user-images.githubusercontent.com/1558019/191369632-87787193-b950-495e-bf33-26037d003886.png)

## Installation

1. First, make sure you have the prerequisites downloaded & installed:
    - .NET 6 Runtime https://dotnet.microsoft.com/en-us/download/dotnet/6.0
    - APCUPSD http://www.apcupsd.org *(also follow the installation instructions to install the driver & make sure the daemon is working with your UPS http://www.apcupsd.org/manual/manual.html#windows-usb-configuration)*
2. Either clone the repo with git or download as a zip
3. Open `Program.cs` and change the values of `DISCORD_WEBHOOK_URL` and, optionally, `BATTERY_STATUS_CHECK_FREQUENCY`
4. Open command prompt, `cd` to the location of the `.csproj` and run `dotnet build`. This should build the `.exe` and place it (along with the `onbattery.bat` and `offbattery.bat` event handlers) in the apcupsd installation directory: `C:\apcupsd\etc\apcupsd`
5. You're all set! Hopefully this never runs to alert you that the power's out 😊
