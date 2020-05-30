cd ..

docker-compose pull
dotnet build --runtime linux-musl-x64 --nologo
docker-compose up --force-recreate --build reverse-proxy
PAUSE