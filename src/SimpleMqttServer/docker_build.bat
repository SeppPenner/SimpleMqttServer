dotnet publish -c Release
docker build --tag DockerHubUser/SimpleMqttServer:1.0.2 -f Dockerfile .
docker push DockerHubUser/SimpleMqttServer:1.0.2
docker run -v Todo/Todo -p 1883:1883 -p 8883:8883 mqtt-server