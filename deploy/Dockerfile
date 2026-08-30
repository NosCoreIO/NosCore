FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine3.24-amd64 AS build
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props NuGet.config ./
COPY tools/ tools/
COPY src/ src/
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish src/NosCore.MasterServer -c Release -o /out/master \
    && dotnet publish src/NosCore.LoginServer -c Release -o /out/login \
    && dotnet publish src/NosCore.WorldServer -c Release -o /out/world \
    && dotnet publish src/NosCore.Parser -c Release -o /out/parser

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine3.24-amd64 AS base

# runtime-deps sets DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true and ships no ICU data.
# See https://aka.ms/dotnet-globalization-alpine-containers
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
RUN apk add --no-cache icu-libs

COPY configuration /app/configuration
WORKDIR /app/build/net10.0

FROM base AS master
COPY --from=build /out/master .
USER $APP_UID
EXPOSE 5000
ENTRYPOINT ["dotnet", "NosCore.MasterServer.dll"]

FROM base AS login
COPY --from=build /out/login .
USER $APP_UID
EXPOSE 4000
ENTRYPOINT ["dotnet", "NosCore.LoginServer.dll"]

FROM base AS world
COPY --from=build /out/world .
USER $APP_UID
EXPOSE 1337 5001
ENTRYPOINT ["dotnet", "NosCore.WorldServer.dll"]

FROM base AS parser
COPY --from=build /out/parser .
USER $APP_UID
ENTRYPOINT ["dotnet", "NosCore.Parser.dll"]
CMD ["--folder", "/dats"]
