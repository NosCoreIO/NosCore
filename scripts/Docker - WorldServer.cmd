set WORLD_WEBAPI_PORT=5001
set WORLD_PORT=4002

cd ..

dotnet build --runtime linux-x64 --nologo
docker-compose up world
PAUSE