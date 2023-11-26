# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY tai.sln .
COPY *.csproj ./
COPY *.json ./
COPY Program.cs ./
RUN dotnet restore tai.sln

# copy everything else and build app
COPY routes routes
COPY utilities utilities
RUN dotnet publish -c release -o /app --no-restore tai.sln
RUN dotnet dev-certs https --clean
RUN dotnet dev-certs https --trust

FROM alpine:3.18.4 AS tools
WORKDIR /tools
RUN apk --no-cache add curl
RUN apk --no-cache add unzip
RUN curl https://www.bok.net/Bento4/binaries/Bento4-SDK-1-6-0-641.x86_64-unknown-linux.zip > Bento4.zip
RUN unzip Bento4.zip -d /tools/Bento4

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app ./
#COPY --from=build /root/.dotnet/corefx/cryptography/x509stores/my/* /root/.dotnet/corefx/cryptography/x509stores/my/
COPY --from=tools /tools ./tools
ENTRYPOINT ["dotnet", "TAIBackend.dll"]
