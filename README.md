SimpleMqttServer
====================================

SimpleMqttServer is a project to run a simple [MQTT server](https://github.com/chkr1011/MQTTnet) from a json config file. The project was written and tested in .NetCore 3.1.

[![Build status](https://ci.appveyor.com/api/projects/status/2a230fy5u5x502tx?svg=true)](https://ci.appveyor.com/project/SeppPenner/simplemqttserver)
[![GitHub issues](https://img.shields.io/github/issues/SeppPenner/SimpleMqttServer.svg)](https://github.com/SeppPenner/SimpleMqttServer/issues)
[![GitHub forks](https://img.shields.io/github/forks/SeppPenner/SimpleMqttServer.svg)](https://github.com/SeppPenner/SimpleMqttServer/network)
[![GitHub stars](https://img.shields.io/github/stars/SeppPenner/SimpleMqttServer.svg)](https://github.com/SeppPenner/SimpleMqttServer/stargazers)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://raw.githubusercontent.com/SeppPenner/SimpleMqttServer/master/License.txt)
[![Known Vulnerabilities](https://snyk.io/test/github/SeppPenner/SimpleMqttServer/badge.svg)](https://snyk.io/test/github/SeppPenner/SimpleMqttServer)

# JSON configuration (Adjust this to your needs):
```json
{
  "Port": 1883,
  "Users": [
    {
      "UserName": "Hans",
      "Password": "Test"
    }
  ]
}
```

Change history
--------------

* **Version 1.0.0.0 (2020-02-13)** : 1.0 release.