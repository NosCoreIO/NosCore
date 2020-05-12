cd ..

dotnet build --runtime linux-x64 --nologo
docker-compose up world
PAUSE