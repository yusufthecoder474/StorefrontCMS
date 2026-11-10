FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY . .
RUN dotnet restore StorefrontCMS.slnx
RUN dotnet test StorefrontCMS.slnx -c Release --no-restore
RUN dotnet publish src/StorefrontCMS -c Release --no-restore -o /app
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
ENTRYPOINT ["dotnet","StorefrontCMS.dll"]
