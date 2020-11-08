# LogoMqttBinding
Supports to synconize values from and to Siemens Logo PLC via MQTT.

## What is it?
It is a connector between one or more [Siemens Logo PLC](https://de.wikipedia.org/wiki/Logo_(SPS)) to [MQTT](https://en.wikipedia.org/wiki/MQTT).
MQTT is widely used for smart home controller (like openHAB or hass.io) or other IoT applications.
 
## Hardware Requirements
The hardware requirements starting with Raspberry Pi computer or the like. The PLC and the MQTT broker should be accessible via wired network.

## Software Requirements
The host should have the latest version of docker and docker-compose installed.

 
 
 
## docker-compose.yml
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

### Volume mapping
The docker-compose.yml volume path ```~/smarthome/config:/app/config``` is mapped to ```C:\Users\<username>\smarthome\config``` in windows or ```~/smarthome/config``` in linux.

## Run a MQTT broker for test purposes
Just fire up a mosquitto instance:
```docker run -d -it --name mosquitto -p 1883:1883 eclipse-mosquitto```




