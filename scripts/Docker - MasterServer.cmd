cd ..

dotnet build --runtime linux-musl-x64 --nologo
docker-compose up master -e MASTER_PORT=5000
PAUSE