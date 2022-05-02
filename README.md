# IdentityService

## Build docker image local

run docker-compose --env-file dev.env up --build -d

## Appsettings

Appsettings template for api can be found in ./Identity.API/default.appsettings.json

## Create first admin user

The first admin user can be created by the command `create` in the docker file. To call it run 

```bash
docker exec -it wekode.mml.identity /bin/bash
root@6712536aabd:/app# create
```
You will be ask for a username and a password. The password must be at least 12 characters long.
If no admin app exists already, a new one will be created and the clientId will be prompt on the console.
