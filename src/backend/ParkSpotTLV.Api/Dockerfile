# ------------ build stage ------------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# copy solution + csproj first (for better layer caching)
COPY ParkSpotTLV.sln ./
COPY src/backend/ParkSpotTLV.Api/ParkSpotTLV.Api.csproj src/backend/ParkSpotTLV.Api/
COPY src/backend/ParkSpotTLV.Infrastructure/ParkSpotTLV.Infrastructure.csproj src/backend/ParkSpotTLV.Infrastructure/
COPY src/backend/ParkSpotTLV.Contracts/ParkSpotTLV.Contracts.csproj src/backend/ParkSpotTLV.Contracts/
RUN dotnet restore src/backend/ParkSpotTLV.Api/ParkSpotTLV.Api.csproj

# copy the rest and publish
COPY . .
RUN dotnet publish src/backend/ParkSpotTLV.Api/ParkSpotTLV.Api.csproj -c Release -o /app/publish

# ------------ runtime stage ------------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# API listens on 8080 inside the container
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "ParkSpotTLV.Api.dll"]
