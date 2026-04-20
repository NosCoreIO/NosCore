dotnet build -r win-x64
dotnet build -r linux-x64
dotnet build -r linux-musl-x64
mkdir .\build\Configuration
cd Configuration
copy *.* ..\build\Configuration\
