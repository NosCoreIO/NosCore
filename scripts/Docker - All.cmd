cd ..

dotnet build --runtime linux-musl-x64 --nologo
docker-compose up -d login master world
PAUSE