SimpleMqttServer
====================================

SimpleMqttServer is a project to run a simple [MQTT server](https://github.com/chkr1011/MQTTnet) from a json config file. The project was written and tested in .Net 6.0.

[![Build status](https://ci.appveyor.com/api/projects/status/2a230fy5u5x502tx?svg=true)](https://ci.appveyor.com/project/SeppPenner/simplemqttserver)
[![GitHub issues](https://img.shields.io/github/issues/SeppPenner/SimpleMqttServer.svg)](https://github.com/SeppPenner/SimpleMqttServer/issues)
[![GitHub forks](https://img.shields.io/github/forks/SeppPenner/SimpleMqttServer.svg)](https://github.com/SeppPenner/SimpleMqttServer/network)
[![GitHub stars](https://img.shields.io/github/stars/SeppPenner/SimpleMqttServer.svg)](https://github.com/SeppPenner/SimpleMqttServer/stargazers)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://raw.githubusercontent.com/SeppPenner/SimpleMqttServer/master/License.txt)
[![Known Vulnerabilities](https://snyk.io/test/github/SeppPenner/SimpleMqttServer/badge.svg)](https://snyk.io/test/github/SeppPenner/SimpleMqttServer)

# JSON configuration (Adjust this to your needs)
```json
{
    "AllowedHosts": "*",
    "SimpleMqttServer": {
        "Port": 1883,
        "Users": [
            {
                "UserName": "Hans",
                "Password": "Test"
            }
        ],
        "DelayInMilliSeconds": 30000,
        "TlsPort":  8883 
    }
}
```

# Run this project in Docker from the command line (Examples for Powershell, but should work in other shells as well):

1. Change the directory
    ```bash
    cd ..\src\SimpleMqttServer
    ```

2. Publish the project
    ```bash
    dotnet publish -c Release --output publish/
    ```

3. Build the docker file:
    * `dockerhubuser` is a placeholder for your docker hub username, if you want to build locally, just name the container `simplemqttserver`
    * `1.0.2` is an example version tag, use it as you like
    * `-f Dockerfile .` (Mind the `.`) is used to specify the dockerfile to use

    ```bash
    docker build --tag dockerhubuser/simplemqttserver:1.0.2 -f Dockerfile .
    ```

4. Push the project to docker hub (If you like)
    * `dockerhubuser` is a placeholder for your docker hub username, if you want to build locally, just name the container `simplemqttserver`
    * `1.0.2` is an example version tag, use it as you like

    ```bash
    docker push dockerhubuser/simplemqttserver:1.0.2
    ```

5. Run the container:
    * `-d` runs the docker container detached (e.g. no logs shown on the console, is needed if run as service)
    * `--name="simplemqttserver"` gives the container a certain name
    * `-p 1883:1883` opens the internal container port 1883 (Default MQTT without TLS) to the external port 1883
    * `-p 8883:8883` opens the internal container port 8883 (Default MQTT with TLS) to the external port 8883
    * `-v "/home/config.json:/app/config.json"` sets the path to the external configuration file (In the example located under `/home/config.json`) to the container internally
    
    ```bash
    docker run -d --name="simplemqttserver" -p 1883:1883 -p 8883:8883 -v "/home/config.json:/app/config.json" --restart=always dockerhubuser/simplemqttserver:1.0.2
    ```

6. Check the status of all containers running (MUst be root)
    ```bash
    docker ps -a
    ```

7. Stop a container
    * `containerid` is the id of the container obtained from the `docker ps -a` command
    ```bash
    docker stop containerid
    ```

8. Remove a container
    * `containerid` is the id of the container obtained from the `docker ps -a` command
    ```bash
    docker rm containerid
    ```

Change history
--------------

See the [Changelog](https://github.com/SeppPenner/SimpleMqttServer/blob/master/Changelog.md).