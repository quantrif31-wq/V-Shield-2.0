FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY API/*.csproj ./API/
RUN dotnet restore ./API/API.csproj

COPY . .
RUN dotnet publish ./API/API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl ffmpeg \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .

EXPOSE 5107
ENTRYPOINT ["dotnet", "API.dll"]
