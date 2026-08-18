@ECHO OFF
cd src\SimpleMqttServer
@ECHO.Publishing for win-x64...
@RD /S /Q publish 2>NUL
dotnet publish -c Release --output publish/ -r win-x64 --no-self-contained
@ECHO.Build successful. Press any key to exit.
pause