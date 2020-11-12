# LogoMqttBinding
Read and write values from Siemens Logo PLC via MQTT.

## This is still a MVP (Minimum viable product)
This software is currently under development. 
Do not (yet) use for productive environment, but feel free to try it.
I appreciate your feedback! The stable version is planned within Q4/2020.

## Planned features in future releases
- MQTT last will
- MQTT status channel
- LOGO password
- better documentation ;)






## What is it?
It is a connector between one or more 
[Siemens Logo PLC](https://de.wikipedia.org/wiki/Logo_(SPS)) to 
[MQTT](https://en.wikipedia.org/wiki/MQTT).
MQTT is widely used for smart home controller (like openHAB or hass.io) or other IoT applications.
 
### Hardware Requirements
A Raspberry Pi computer or the like should be sufficient for smart home usage with moderate value exchange.
The PLC and the MQTT broker should be accessible via wired network.
Wifi connections are not recommended, but should work as well.

### Software Requirements
All operating systems that support docker and docker-compose are supported.







 
 
 
## Get started
Download 
[docker-compose.yml](https://raw.githubusercontent.com/thosch1800/LogoMqttBinding/main/docker-compose.yml) 
(right click -> save link as).

With the first start ```docker-compose up``` the program will create a default configuration. 
It will exit with errors because the default config is just a template for your setup. 
The configuration file resides in smarthome/config in your home folder, 
this is ```C:\Users\<username>\smarthome\config\logo-mqtt.json``` in windows
or ```~/smarthome/config/logo-mqtt.json``` in linux.

Edit this file to meet your setup. 
If you want to return to the default configuration just rename the logo-mqtt.json and restart the application.










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










### How to run a MQTT broker for test purposes
Just fire up a mosquitto instance:
```docker run -d -it --name mosquitto -p 1883:1883 eclipse-mosquitto```




### A picture of my first manual test
On the left you can see the configuration I used, in the middle you can see the container output. 
The program on the right is a MQTT client [MQTT.fx](https://mqttfx.jensd.de/index.php).
I also used a real logo PLC that runs a program with all inputs and outputs connected via network input/output.
![](https://raw.githubusercontent.com/thosch1800/LogoMqttBinding/main/docs/poc.png)
Whenever I set an input to high or low I got a message for the subscribes channels.      
MISSION ACCOMPLISHED
