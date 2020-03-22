set LOGIN_PORT=1337

cd ..

dotnet build --runtime linux-x64 --nologo
docker-compose up login
PAUSE