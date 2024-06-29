![GitHub top language](https://img.shields.io/github/languages/top/we-kode/mml.identity?label=c%23&logo=dotnet&style=for-the-badge) ![GitHub Release Date](https://img.shields.io/github/release-date/we-kode/mml.identity?label=Last%20release&style=for-the-badge) ![Docker Image Version (latest by date)](https://img.shields.io/docker/v/w3kod3/wekode.mml.identity?logo=docker&style=for-the-badge) ![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/we-kode/mml.identity/docker-image.yml?label=Docker%20CI&logo=github&style=for-the-badge) ![GitHub](https://img.shields.io/github/license/we-kode/mml.identity?style=for-the-badge)

# Identity

Identity is part of the [My Media Lib](https://we-kode.github.io/mml.project/) project. This service is responsible for managing [admins](https://we-kode.github.io/mml.project/concepts/admins) and [clients](https://we-kode.github.io/mml.project/concepts/clients).

## Local Development

Setup your local development environment to run [.NET](https://learn.microsoft.com/en-us/dotnet/) and [docker](https://docs.docker.com/).

Clone this repository and configure it, like described in the following sections. Consider that this service does not run standalone. You have to [setup the backend](https://we-kode.github.io/mml.project/setup/backend) to run the [My Media Lib](https://we-kode.github.io/mml.project/) project.

### Appsettings

A template of the appsettings can be found at [./Identity.API/default.appsettings.json](./Identity.API/default.appsettings.json). Create a local copy and rename it how you like. Fill in the configuration. [Check](https://we-kode.github.io/mml.project/setup/backend#configuration-2) the official documentation on how to fill in the documentation.

### Configure .env

Create a local copy of the `.env` file name e.g. `dev.env` and fill in the configuration. Check the [official documentation](https://we-kode.github.io/mml.project/setup/backend) on how to configure the `.env` file.

### Local build the docker image

To build the docker image on your machine run

```
docker-compose --env-file dev.env up --build -d
```

## Deployment
### Releases

New releases will be available if new features or improvements exists. [Check](https://github.com/we-kode/mml.identity/releases) the corresponding release to learn what has changed. Binary releases are only available as docker images on [docker hub](https://hub.docker.com/r/w3kod3/wekode.mml.identity).

### Setup

Check the official documentation on [how to setup](https://we-kode.github.io/mml.project/setup/backend#configure-media-service) the media service.

## Create first admin user

The first admin user can be created by the command `create` by using the cli provided by the service. To call it in docker run 

```bash
docker exec -it wekode.mml.identity /bin/bash
root@6712536aabd:/app# create
```
You will be ask for an username and a password. The password must be at least 12 characters long.
If no admin app client exists already, a new one will be created and the client id will be printed on the console.

## Create admin clients

Admin clients are backend services, which need to validate their requests by the identity service. They can be created, listed and removed by the command line provided by the service. **To create one client call**

```bash
docker exec -it wekode.mml.identity /bin/bash
root@6712536aabd:/app# admin-create
```

**To list all admin clients call:**

```bash
docker exec -it wekode.mml.identity /bin/bash
root@6712536aabd:/app# admin-list
```

**And to remove one client call:**

```bash
docker exec -it wekode.mml.identity /bin/bash
root@6712536aabd:/app# admin-remove <client id>
```

## Contribution

Please check the official documentation on [how to contribute](https://we-kode.github.io/mml.project/contribution).
