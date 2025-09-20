# ------------ build stage ------------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# copy solution + csproj first (for better layer caching)
COPY ParkSpotTLV.sln ./
COPY ParkSpotTLV.Api/ParkSpotTLV.Api.csproj ParkSpotTLV.Api/
COPY ParkSpotTLV.Infrastructure/ParkSpotTLV.Infrastructure.csproj ParkSpotTLV.Infrastructure/
RUN dotnet restore ParkSpotTLV.Api/ParkSpotTLV.Api.csproj

# copy the rest and publish
COPY . .
RUN dotnet publish ParkSpotTLV.Api/ParkSpotTLV.Api.csproj -c Release -o /app/publish

# ------------ runtime stage ------------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# API listens on 8080 inside the container
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "ParkSpotTLV.Api.dll"]
