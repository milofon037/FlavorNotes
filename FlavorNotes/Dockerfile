FROM ubuntu/dotnet-aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM ubuntu:24.04 AS build
WORKDIR /src

RUN apt-get update && apt-get install -y \
    dotnet-sdk-8.0 \
    && rm -rf /var/lib/apt/lists/*

COPY ["FlavorNotes.csproj", "./"]
RUN dotnet restore "FlavorNotes.csproj"

COPY . .
RUN dotnet build "FlavorNotes.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FlavorNotes.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FlavorNotes.dll"]
