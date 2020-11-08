# LogoMqttBinding
Supports to synconize values from and to Siemens Logo PLC via MQTT.

## What is it?
It is a connector between one or more [Siemens Logo PLC](https://de.wikipedia.org/wiki/Logo_(SPS)) to [MQTT](https://en.wikipedia.org/wiki/MQTT).
MQTT is widely used for smart home controller (like openHAB or hass.io) or other IoT applications.
 
### Hardware Requirements
The hardware requirements starting with Raspberry Pi computer or the like. The PLC and the MQTT broker should be accessible via wired network.

### Software Requirements
The host should have the latest version of docker and docker-compose installed.

 
 
 
## Get started
download the file from the repository or create it on your own:
```
version: '3.3'

services:
  logo-mqtt-binding:
    image: ghcr.io/thosch1800/logo-mqtt:latest
    volumes:
      - ~/smarthome/config:/app/config
    restart: always
```

After a first start with ```docker-compose up``` the program will crash because it is using the default configuration. 
The configuration file resides in smarthome/config in your home folder, 
this is ```C:\Users\<username>\smarthome\config\logo-mqtt.json``` in windows
or ```~/smarthome/config/logo-mqtt.json``` in linux.
Edit this file to meet your needs - if you want to go back to the default configuration just rename  or delete your current config file and restart the application.



## logo-mqtt.json
```
{
     "MqttBrokerUri": "192.168.2.23",
     "Logos": [
       {
         "IpAddress": "192.168.2.42",
         "MemoryRanges": [
           {
             "LocalVariableMemoryPollingCycleMilliseconds": 100,
             "LocalVariableMemoryStart": 0,
             "LocalVariableMemoryEnd": 250
           }
         ],
         "Mqtt": [
           {
             "ClientId": "logo",
             "Channels": [

               {
                 "Action": "publish",
                 "Topic": "logo/q1/get",
                 "LogoAddress": 111,
                 "Type": "byte",
                 "QualityOfService": "ExactlyOnce"
               },
               {
                 "Action": "subscribePulse",
                 "Topic": "logo/q1/set",
                 "LogoAddress": 11,
                 "Type": "byte",
                 "QualityOfService": "ExactlyOnce"
               },
   
               {
                 "Action": "publish",
                 "Topic": "logo/q2/get",
                 "LogoAddress": 112,
                 "Type": "byte",
                 "QualityOfService": "AtLeastOnce"
               },
               {
                 "Action": "subscribe",
                 "Topic": "logo/q2/set",
                 "LogoAddress": 12,
                 "Type": "byte",
                 "QualityOfService": "AtLeastOnce"
               },
   
               {
                 "Action": "publish",
                 "Topic": "logo/q3/get",
                 "LogoAddress": 113,
                 "Type": "byte"
               },
               {
                 "Action": "subscribe",
                 "Topic": "logo/q3/set",
                 "LogoAddress": 13,
                 "Type": "byte"
               },
   
               {
                 "Action": "publish",
                 "Topic": "logo/q4/get",
                 "LogoAddress": 114,
                 "Type": "byte"
               },
               {
                 "Action": "subscribe",
                 "Topic": "logo/q4/set",
                 "LogoAddress": 14,
                 "Type": "byte"
               },
   			
   			
               {
                 "Topic": "logo/i1/get",
                 "LogoAddress": 1,
                 "Type": "byte"
               },
               {
                 "Topic": "logo/i2/get",
                 "LogoAddress": 2,
                 "Type": "byte"
               },
               {
                 "Topic": "logo/i3/get",
                 "LogoAddress": 3,
                 "Type": "byte"
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
               },
               {
                 "Topic": "logo/i6/get",
                 "LogoAddress": 6,
                 "Type": "byte"
               },
               {
                 "Topic": "logo/i7/get",
                 "LogoAddress": 7,
                 "Type": "byte"
               },
               {
                 "Topic": "logo/i8/get",
                 "LogoAddress": 8,
                 "Type": "byte"
               }
   			
             ]
           }
         ]
       }
     ]
   }
```


## Run a MQTT broker for test purposes
Just fire up a mosquitto instance:
```docker run -d -it --name mosquitto -p 1883:1883 eclipse-mosquitto```




