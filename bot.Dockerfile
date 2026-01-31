FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /app

# Copy everything else and build
COPY . ./
RUN dotnet publish "./src/CardboardBox.Anime.Bot.Cli/CardboardBox.Anime.Bot.Cli.csproj" -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "CardboardBox.Anime.Bot.Cli.dll"]

# https://docs.docker.com/engine/examples/dotnetcore/