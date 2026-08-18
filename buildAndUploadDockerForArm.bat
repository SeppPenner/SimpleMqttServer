@ECHO OFF
cd src\SimpleMqttServer
@ECHO.Publishing for linux-arm...
@RD /S /Q publish 2>NUL
dotnet publish -c Release --output publish/ -r linux-arm --no-self-contained
docker build --platform linux/arm/v7 --tag sepppenner/simplemqttserver-arm:1.0.9 -f Dockerfile.armv7 .
@ECHO %DOCKERHUB_CLI_TOKEN%| docker login -u sepppenner --password-stdin
docker push sepppenner/simplemqttserver-arm:1.0.9
@ECHO.Build successful. Press any key to exit.
pause