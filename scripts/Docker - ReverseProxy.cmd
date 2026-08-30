cd ..

docker-compose pull
docker-compose up --force-recreate --build reverse-proxy
PAUSE