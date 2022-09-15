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
If no admin app exists already, a new one will be created and the clientId will be printed on the console.

## Create admin clients

Admin clients can be created, listed and removed by the command line in the container. To create one client call

```bash
docker exec -it wekode.mml.identity /bin/bash
root@6712536aabd:/app# admin-create
```

To list all admin clients call:

```bash
docker exec -it wekode.mml.identity /bin/bash
root@6712536aabd:/app# admin-list
```

And to remove one client call:

```bash
docker exec -it wekode.mml.identity /bin/bash
root@6712536aabd:/app# admin-remove <client id>
```
