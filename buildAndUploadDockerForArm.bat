cd src\SimpleMqttServer
dotnet publish -c Release --output publish/ -r linux-arm --no-self-contained
docker build --tag sepppenner/simplemqttserver-arm:1.0.4 -f Dockerfile.armv7 .
docker login -u sepppenner -p "%DOCKERHUB_CLI_TOKEN%"
docker push sepppenner/simplemqttserver-arm:1.0.4
@ECHO.Build successful. Press any key to exit.
pause