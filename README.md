## Kiosk App

Application for launching and updating custom kiosk applications

# Compiling & Running

* Install .NET core (only Windows has been tested)
* Update the appsettings.json and logo.png (see publish/README.md)
* In the project directory, run: `dotnet build` and `dotnet run`

# Building production .EXE

`dotnet publish -c Release -r win-x64 /p:PublishSingleFile=true /p:PublishTrimmed=true /p:IncludeNativeLibrariesForSelfExtract=true -o ./publish`