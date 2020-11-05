# LogoMqttBinding
MQTT binding for Siemens Logo

Supports to sync values from and to Siemens Logo PLC via MQTT.

## Volume mapping
The docker-compose.yml volume path
```~/smarthome/config:/app/config```
is mapped to 
```C:\Users\<username>\smarthome\config```
when using windows

## Run MQTT broker locally
```docker run -d -it --name mosquitto -p 1883:1883 eclipse-mosquitto```
