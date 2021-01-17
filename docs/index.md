# LogoMqttBinding
Read and write values from Siemens Logo PLC via MQTT.

## Planned features in future releases
- better documentation ;)






## What is it?
It is a connector between one or more 
[Siemens Logo PLC](https://de.wikipedia.org/wiki/Logo_(SPS)) to one 
[MQTT](https://en.wikipedia.org/wiki/MQTT) broker.
MQTT is widely used for smart home controller (like openHAB or hass.io) or other IoT applications.
 
### Hardware Requirements
A Raspberry Pi computer or the like should be sufficient for smart home usage with moderate value exchange.
The PLC and the MQTT broker should be accessible via wired network.
Wifi connections are not recommended, but should work as well.

### Software Requirements
All operating systems that support docker and docker-compose are supported.







 
 
 
## Get started
- Download [docker-compose.yml](https://raw.githubusercontent.com/thosch1800/LogoMqttBinding/main/docker-compose.yml) (right click -> save link as).
  
- With the first start ```docker-compose up -d``` the program will create a default configuration. It will throw errors because the default config is just a template for your setup.
- Use ```docker-compose down``` to stop the program.
- Edit config to meet your setup. 
  The configuration file resides in smarthome/config in your home folder, this is 
  ```C:\Users\<username>\smarthome\config\logo-mqtt.json``` in windows or 
  ```~/smarthome/config/logo-mqtt.json``` in linux.
- Start the program with your config ```docker-compose up -d```
- You can check the output of the program using ```docker-compose logs -f``` (Ctrl+C to exit)
- Have fun using MQTT to control your Logo!






## logo-mqtt.json
```
{
  "MqttBrokerUri": "192.168.1.86",
  "MqttBrokerUsername": "user",
  "MqttBrokerPassword": "password",
  "Logos": [
    {
      "IpAddress": "192.168.1.230",
      "MemoryRanges": [
        {
          "LocalVariableMemoryPollingCycleMilliseconds": 100,
          "LocalVariableMemoryStart": 0,
          "LocalVariableMemoryEnd": 150
        }
      ],
      "Mqtt": [
        {
          "ClientId": "switch",
          "Status": {
            "Topic": "switch/logo"
          },
          "Channels": [
            {
              "Topic": "switch/get",
              "LogoAddress": 3,
              "Type": "byte"
            },
            {
              "Topic": "switch/set",
              "LogoAddress": 13,
              "Type": "byte",
              "Action": "set"
            },
            {
              "Topic": "switch/pulse",
              "LogoAddress": 13,
              "Type": "byte",
              "Action": "pulse"
            },
            {
              "Topic": "switch/power",
              "LogoAddress": 101,
              "Type": "byte"
            }
          ]
        },
        {
          "ClientId": "logo",
          "Status": {
            "Topic": "logo"
          },
          "Channels": [
            {
              "Topic": "logo/q1/get",
              "LogoAddress": 101,
              "Type": "byte",
              "Action": "get"
            },
            {
              "Topic": "logo/q1/set",
              "LogoAddress": 111,
              "Type": "byte",
              "Action": "set"
            },

            {
              "Topic": "logo/q2/get",
              "LogoAddress": 102,
              "Type": "byte",
              "Action": "get"
            },
            {
              "Topic": "logo/q2/set",
              "LogoAddress": 112,
              "Type": "byte",
              "Action": "set"
            },

            {
              "Topic": "logo/q3/get",
              "LogoAddress": 103,
              "Type": "byte",
              "Action": "get",
              "QualityOfService": "ExactlyOnce"
            },
            {
              "Topic": "logo/q3/set",
              "LogoAddress": 113,
              "Type": "byte",
              "Action": "set",
              "QualityOfService": "ExactlyOnce"
            },

            {
              "Topic": "logo/q4/get",
              "LogoAddress": 104,
              "Type": "byte",
              "Action": "get",
              "QualityOfService": "AtLeastOnce"
            },
            {
              "Topic": "logo/q4/set",
              "LogoAddress": 114,
              "Type": "byte",
              "Action": "set",
              "QualityOfService": "AtLeastOnce"
            },


            {
              "Topic": "logo/i1/get",
              "LogoAddress": 1,
              "Type": "byte"
            },
            {
              "Topic": "logo/i1/set",
              "LogoAddress": 11,
              "Type": "byte",
              "Action": "set"
            },

            {
              "Topic": "logo/i2/get",
              "LogoAddress": 2,
              "Type": "byte"
            },
            {
              "Topic": "logo/i2/set",
              "LogoAddress": 12,
              "Type": "byte",
              "Action": "set"
            },
            
            {
              "Topic": "logo/i3/get",
              "LogoAddress": 3,
              "Type": "byte"
            },
            {
              "Topic": "logo/i3/set",
              "LogoAddress": 13,
              "Type": "byte",
              "Action": "set"
            },
            
            {
              "Topic": "logo/i4/get",
              "LogoAddress": 4,
              "Type": "byte"
            },
            
            {
              "Topic": "logo/i5/get",
              "LogoAddress": 5,
              "Type": "byte"
            }
          ]
        }
      ]
    }
  ]
}

```






### Manual test setup
- MQTT server ```docker run -d -it --name mosquitto -p 1883:1883 eclipse-mosquitto``` 
- MQTT client [MQTT.fx](https://mqttfx.jensd.de/index.php)
- PLC program [logo-230.lsc](https://raw.githubusercontent.com/thosch1800/LogoMqttBinding/main/docs/logo-230.lsc)
- LogoMqttBinding [docker-compose.yml](https://raw.githubusercontent.com/thosch1800/LogoMqttBinding/main/docker-compose.yml)
- LogoMqttBinding configuration [logo-mqtt.json](https://raw.githubusercontent.com/thosch1800/LogoMqttBinding/main/docs/logo-mqtt.json)

#### Manual test scenarios:
- change value at PLC and MQTT received event: 
  - subscribe to ```logo/#``` with your MQTT client
  - toggle a input
  - MQTT client must show value update for corresponding input
- Set PLC value from MQTT 
  - in MQTT client publish the value ```1``` to ```switch/pulse```
  - the logo output should be set for a short moment (you can hear the relay click or see it in the logo display)
  - you can also subscribe to ```switch/power``` and receive updates for this PLC output 
- Robust connection to PLC
  - disconnect network cable from PLC
  - wait for a random time
  - connect network cable to PLC
  - check if PLC variables can be controlled via MQTT client  
- Robust connection to MQTT server
  - stop mosquitto ```docker stop mosquitto```
  - wait for a random time
  - start mosquitto ```docker start mosquitto```
  - check if PLC variables can be controlled via MQTT client  


             
![](https://raw.githubusercontent.com/thosch1800/LogoMqttBinding/main/docs/poc.png)
