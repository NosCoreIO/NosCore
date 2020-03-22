set MASTER_PORT=5000
set WORLD_WEBAPI_PORT=5001
set WORLD_PORT=4002
set LOGIN_PORT=1337

cd ..

dotnet build --runtime linux-x64 --nologo
docker-compose up -d login master world
PAUSE