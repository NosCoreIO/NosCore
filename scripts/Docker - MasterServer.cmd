cd ..

dotnet build --runtime linux-x64 --nologo
docker-compose up master -e MASTER_PORT=5000
PAUSE