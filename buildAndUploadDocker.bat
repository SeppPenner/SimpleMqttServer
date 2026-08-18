@ECHO OFF
cd src\SimpleMqttServer
@ECHO.Publishing for linux-x64...
@RD /S /Q publish 2>NUL
dotnet publish -c Release --output publish/ -r linux-x64 --no-self-contained
docker build --platform linux/amd64 --tag sepppenner/simplemqttserver:1.0.10 -f Dockerfile .
@ECHO %DOCKERHUB_CLI_TOKEN%| docker login -u sepppenner --password-stdin
docker push sepppenner/simplemqttserver:1.0.10
@ECHO.Build successful. Press any key to exit.
pause