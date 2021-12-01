#!/bin/bash
set -ev

TAG=$1
DOCKER_USERNAME=$2
DOCKER_PASSWORD=$3

# publish docker
# Gandalf node
dotnet publish Gandalf.sln /clp:ErrorsOnly -c Release -o ~/Gandalf/

docker build -t Gandalf/node:${TAG} ~/Gandalf/.
docker tag Gandalf/node:${TAG} Gandalf/node:latest
docker login -u="$DOCKER_USERNAME" -p="$DOCKER_PASSWORD"
docker push Gandalf/node:${TAG}
docker push Gandalf/node:latest
