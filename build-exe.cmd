dotnet build -r win-x64 || exit /b 1
dotnet build -r linux-x64 || exit /b 1
dotnet build -r linux-musl-x64 || exit /b 1
mkdir .\build\Configuration || exit /b 1
cd Configuration || exit /b 1
copy *.* ..\build\Configuration\ || exit /b 1
