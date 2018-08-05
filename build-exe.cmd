dotnet build -r win10-x64
dotnet build -r ubuntu.14.04-x64
mkdir .\build\Configuration
cd Configuration
copy *.* ..\build\Configuration\