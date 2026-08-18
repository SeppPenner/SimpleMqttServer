## Basic usage

### JSON configuration (Adjust this to your needs)
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

### TLS (Optional)

The certificate is the switch, there is no separate flag. Without `CertificatePath` the server
listens on `Port` only and nothing answers on `TlsPort`, which is what every version up to 1.0.9
did. With a certificate the encrypted endpoint on `TlsPort` comes on top, TLS 1.2 and TLS 1.3.

PKCS#12, one file and its password:

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
        "TlsPort": 8883,
        "CertificatePath": "/app/certificate.pfx",
        "CertificatePassword": "YourCertificatePassword"
    }
}
```

PEM, the way Let's Encrypt hands it out. Setting `CertificateKeyPath` is what switches the loading
to PEM, `CertificatePassword` is ignored then:

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
        "TlsPort": 8883,
        "CertificatePath": "/app/fullchain.pem",
        "CertificateKeyPath": "/app/privkey.pem"
    }
}
```

The service refuses to start when the configured certificate file is missing or when `Port` and
`TlsPort` are the same, so a half open endpoint cannot happen. The certificate needs its private
key, a certificate on its own is not enough.

### Run this project in Docker from the command line (Examples for Powershell, but should work in other shells as well):

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
    * `-v "/home/config.json:/app/appsettings.json"` sets the path to the external configuration file (In the example located under `/home/appsettings.json`) to the container internally
    
    ```bash
    docker run -d --name="simplemqttserver" -p 1883:1883 -p 8883:8883 -v "/home/appsettings.json:/app/appsettings.json" --restart=always dockerhubuser/simplemqttserver:1.0.2
    ```

    With TLS the certificate has to reach the container as well, and the path in `appsettings.json`
    is the one inside the container:

    ```bash
    docker run -d --name="simplemqttserver" -p 1883:1883 -p 8883:8883 -v "/home/appsettings.json:/app/appsettings.json" -v "/home/certificate.pfx:/app/certificate.pfx" --restart=always dockerhubuser/simplemqttserver:1.0.2
    ```

6. Check the status of all containers running (Must be root)
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