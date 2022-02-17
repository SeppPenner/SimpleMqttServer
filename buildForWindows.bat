cd src\SimpleMqttServer
dotnet publish -c Release --output publish/ -r win-x64 --no-self-contained
@ECHO.Build successful. Press any key to exit.
pause