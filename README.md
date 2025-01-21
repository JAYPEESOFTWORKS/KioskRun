# Kiosk App

Application for launching and updating custom Windows kiosk applications. The application contains no forms of security. The resource updates are expected to hosted on a fully-opened HTTP server.

## Compiling & Running KioskRun

* Install .NET core (only Windows has been tested)
* Update the appsettings.json and logo.png
* In the project directory, run: `dotnet build` and `dotnet run`

## Building production KioskRun.exe

The following command should build the entire project into one executable:

`dotnet publish -c Release -r win-x64 /p:PublishSingleFile=true /p:PublishTrimmed=true /p:IncludeNativeLibrariesForSelfExtract=true -o ./publish`

*Note: Once KioskRun.exe is build, it will need `appsettings.json` and `logo.png` to run.*

## Setting up a Windows 11 Kiosk

### Configuring Resource Server

Setup a static HTTP server (ea: nginx or apache) to serve files. The strucutre of the root directory should be as follows:

* root
    * {KIOSK_APP_1}
        * version.html
        * {VERSION}
            * bundle.zip
        * {ANOTHER VERSION}
            * bundle.zip

Sample:

https://www.yourdomain.com/kiosk/COOL_KIOSK_APP/version.html
https://www.yourdomain.com/kiosk/COOL_KIOSK_APP/1.0.0/bundle.zip

*When started, KioskApp will attempt to download `version.html` which should contain nothing other than a version string that matches the same of the desired version folder containing bundle.zip. For example, should KioskRun be started with version `1.0.0` specified in the `appsettings.json` config file it would download `version.html` and update appsettings.json if different version is specified within.*

### Configured a newly installed Windows 11 PC

There are various ways to configure the PC, based on specific needs. While Windows 11 contains an excellent "Kiosk Mode", it only works with some programs. Following are steps that can be taken to accomplish the following:

* Prevent PC user from needing to enter a password or PIN. This is necessary to ensure that the PC can lose power, restart, and run the kiosk application. To do this, set the default user's password to blank (empty).
* Set values in appsettings.json, placing the file in the default user's home directory.
* Execute KioskRun.exe and ensure it created the folder containing the extracted bundle.zip.  Copy this folder to a sibling folder named "local".
* Disable system sleep and screensavers.
* Configure the system to use KioskRun.exe as the default shell
    * Press CTRL+ALT+DELETE.
        * Select Task Manager.
        * Run New Task
        * regedit [ENTER]
        * Change HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon\Shell to "DRIVE:\path\to\KioskRun.exe".
        * Restart PC


#### How to restore default shell
* Install a physical keyboard.
* Boot Kiosk.
* Press CTRL+ALT+DELETE.
    * Select Task Manager.
    * Run New Task
    * regedit [ENTER]
    * Change HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon\Shell to "explorer.exe".
    * Restart PC