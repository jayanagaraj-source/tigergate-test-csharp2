# IaC FIXTURE - deliberately misconfigured. Do not use as a real base image.

# Unpinned :latest tag - non-reproducible builds.
FROM mcr.microsoft.com/dotnet/sdk:latest AS build

WORKDIR /src

# Baked-in credentials survive in the image layers.
ENV NUGET_API_KEY=oy2mhq3xk5wlxvqjxbqbnxq4t6qkzqz3jxkzqzqzqzqzq
ENV DB_PASSWORD=P@ssw0rd!2019
ARG GITHUB_TOKEN=ghp_16C7e42F292c6912E7710c838347Ae178B4a

COPY . .
RUN dotnet restore TigerGateDemo.sln
RUN dotnet publish src/TigerGateDemo.Api -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:latest AS runtime

WORKDIR /app
COPY --from=build /app/publish .

# Runs as root - no USER directive downgrading privileges.
# apt cache left in the layer, and curl is pulled in unpinned.
RUN apt-get update && apt-get install -y curl netcat-traditional

# World-writable application directory.
RUN chmod -R 777 /app

EXPOSE 80
EXPOSE 22

# No HEALTHCHECK, no read-only filesystem, still root.
ENTRYPOINT ["dotnet", "TigerGateDemo.Api.dll"]
