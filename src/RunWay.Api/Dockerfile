FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY NuGet.Config ./
COPY src/RunWay.Contracts/RunWay.Contracts.csproj src/RunWay.Contracts/
COPY src/RunWay.Api/RunWay.Api.csproj src/RunWay.Api/
RUN dotnet restore src/RunWay.Api/RunWay.Api.csproj
COPY src/RunWay.Contracts/ src/RunWay.Contracts/
COPY src/RunWay.Api/ src/RunWay.Api/
RUN dotnet publish src/RunWay.Api/RunWay.Api.csproj -c Release -o /publish --no-restore /p:UseAppHost=false
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
RUN mkdir -p /data /keys && chown -R $APP_UID:$APP_UID /data /keys
COPY --from=build /publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "RunWay.Api.dll"]
