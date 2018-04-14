FROM buildpack-deps:stretch-scm

# Install .NET CLI dependencies
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        libc6 \
        libgcc1 \
        libgssapi-krb5-2 \
        libicu57 \
        liblttng-ust0 \
        libssl1.0.2 \
        libstdc++6 \
        zlib1g \
    && rm -rf /var/lib/apt/lists/*

# Install .NET Core SDK
ENV DOTNET_SDK_VERSION 2.1.300-preview2-008530
ENV ASPNETCORE_VERSION 2.1.0-preview2-final

RUN curl -SL --output dotnet.tar.gz https://dotnetcli.blob.core.windows.net/dotnet/Sdk/$DOTNET_SDK_VERSION/dotnet-sdk-$DOTNET_SDK_VERSION-linux-x64.tar.gz \
    && dotnet_sha512='034863bdb94a4e752d286eeac10638a012c4bae94a9bff46ee96fb7ea733554f0083d989ecf983274fcbe5c27974e16a7287c3bcca98626380b12e811fdd9174' \
    && echo "$dotnet_sha512 dotnet.tar.gz" | sha512sum -c - \
    && mkdir -p /usr/share/dotnet \
    && tar -zxf dotnet.tar.gz -C /usr/share/dotnet \
    && rm dotnet.tar.gz \
    && ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet

FROM microsoft/dotnet:2.1-runtime-deps-stretch-slim

RUN curl -SL --output aspnetcore.tar.gz https://dotnetcli.blob.core.windows.net/dotnet/aspnetcore/Runtime/$ASPNETCORE_VERSION/aspnetcore-runtime-$ASPNETCORE_VERSION-linux-x64.tar.gz \
    && aspnetcore_sha512='4bbc0f25623947048430f5e44a0d3dc444f13fb8fd0058b148f86ef31a0167c35c72accf6c713c92762840bd0059890417e5ebed0c408e5f7d4f25ea2e3844c1' \
    && echo "$aspnetcore_sha512  aspnetcore.tar.gz" | sha512sum -c - \
    && mkdir -p /usr/share/dotnet \
    && tar -zxf aspnetcore.tar.gz -C /usr/share/dotnet \
    && rm aspnetcore.tar.gz \
&& ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet
	
WORKDIR /app

COPY . .

RUN dotnet restore

RUN dotnet build

WORKDIR /app/build/netcoreapp2.0
EXPOSE 5000 6969

ENTRYPOINT ["dotnet", "NosCore.MasterServer.dll"]