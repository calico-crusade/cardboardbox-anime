################
### build ng ###
################

# base image
FROM node:16 as build-ng
ARG configuration=production

# set working directory
WORKDIR /app

# add `/app/node_modules/.bin` to $PATH
ENV PATH /app/node_modules/.bin:$PATH

# install and cache app dependencies
COPY ui/package.json .
COPY ui/yarn.lock .
RUN yarn install

# add app
COPY ui/ .

# generate build
RUN ng build -c $configuration --base-href / --source-map=false

#################
### build web ###
#################
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
WORKDIR /app
COPY src/ .
RUN dotnet publish "./CardboardBox.Anime.Web/CardboardBox.Anime.Web.csproj" -c Release -o out

############
### prod ###
############

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build-env /app/out .
COPY --from=build-ng /app/dist/cardboard-box-anime ./wwwroot
ENTRYPOINT ["dotnet", "CardboardBox.Anime.Web.dll"]

# https://docs.docker.com/engine/examples/dotnetcore/
