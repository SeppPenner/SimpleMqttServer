cd src\SimpleMqttServer
dotnet publish -c Release --output publish/
docker build --tag sepppenner/simplemqttserver:1.0.3 -f Dockerfile .
docker login -u sepppenner -p "%DOCKERHUB_CLI_TOKEN%"
docker push sepppenner/simplemqttserver:1.0.3
@ECHO.Build successful. Press any key to exit.
pause