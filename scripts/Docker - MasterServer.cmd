cd ..

dotnet build --runtime linux-musl-x64 --nologo
docker-compose up --force-recreate --build master
PAUSE