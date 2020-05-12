cd ..

dotnet build --runtime linux-x64 --nologo
docker-compose up -d login master world
PAUSE