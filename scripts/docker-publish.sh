#!/usr/bin/env bash

echo "$DOCKER_PASSWORD" | docker login -u "$DOCKER_USERNAME" --password-stdin
dotnet build --nologo

DOCKER_ENV=''
DOCKER_TAG=''
export PATH=$PATH:$HOME/.local/bin

case "$TRAVIS_BRANCH" in
  "master")
    DOCKER_ENV=production
    DOCKER_TAG=latest
    ;;  
esac

docker build -f ./deploy/Dockerfile-world -t noscore.worldserver:$DOCKER_TAG --no-cache .
docker tag noscore.worldserver:$DOCKER_TAG noscoreio/noscore.worldserver:$DOCKER_TAG
docker push noscoreio/noscore.worldserver:$DOCKER_TAG

docker build -f ./deploy/Dockerfile-login -t noscore.loginserver:$DOCKER_TAG --no-cache .
docker tag noscore.loginserver:$DOCKER_TAG noscoreio/noscore.loginserver:$DOCKER_TAG
docker push noscoreio/noscore.loginserver:$DOCKER_TAG

docker build -f ./deploy/Dockerfile-master -t noscore.masterserver:$DOCKER_TAG --no-cache .
docker tag noscore.masterserver:$DOCKER_TAG noscoreio/noscore.masterserver:$DOCKER_TAG
docker push noscoreio/noscore.masterserver:$DOCKER_TAG
