FROM mcr.microsoft.com/dotnet/sdk:8.0.204 AS build
WORKDIR /app
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/published-app

FROM mcr.microsoft.com/dotnet/aspnet:8.0.204
WORKDIR /app
COPY --from=build /app/published-app .
ENTRYPOINT ["dotnet", "BidServiceAPI.dll"]
