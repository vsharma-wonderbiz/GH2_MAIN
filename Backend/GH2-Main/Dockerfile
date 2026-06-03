# # Build Stage
# FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# WORKDIR /src

# COPY . .

# RUN dotnet restore  GH2-Main.sln

# RUN dotnet publish GH2-Main/GH2-Main.csproj -c Release -o /app/publish


# # Runtime Stage
# FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

# WORKDIR /app

# COPY --from=build /app/publish .

# ENV ASPNETCORE_URLS=http://+:7144

# EXPOSE 7144

# ENTRYPOINT ["dotnet", "GH2-Main.dll"]



# # Build Stage
# FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# WORKDIR /src

# COPY . .

# RUN dotnet restore GH2-Main.sln

# RUN dotnet publish GH2-Main/GH2-Main.csproj -c Release -o /app/publish

# # Install EF tools and create migration bundle
# RUN dotnet tool install --global dotnet-ef
# ENV PATH="$PATH:/root/.dotnet/tools"

# RUN dotnet ef migrations bundle \
#     --project Infrastructure/Infrastructure.csproj \
#     --startup-project GH2-Main/GH2-Main.csproj \
#     --configuration Release \
#     --self-contained \
#     --runtime linux-x64 \
#     -o /app/publish/efbundle

# # Runtime Stage
# FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

# WORKDIR /app

# COPY --from=build /app/publish .

# COPY entrypoint.sh .
# RUN chmod +x entrypoint.sh

# ENV ASPNETCORE_URLS=http://+:7144

# EXPOSE 7144

# ENTRYPOINT ["./entrypoint.sh"]


# Build Stage
# FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# WORKDIR /src

# COPY . .

# # Restore with linux-x64 runtime so efbundle can target it
# RUN dotnet restore GH2-Main.sln -r linux-x64

# RUN dotnet publish GH2-Main/GH2-Main.csproj -c Release -o /app/publish

# # Install EF tools
# RUN dotnet tool install --global dotnet-ef
# ENV PATH="$PATH:/root/.dotnet/tools"

# # Bundle migrations
# RUN dotnet ef migrations bundle \
#     --project Infrastructure/Infrastructure.csproj \
#     --startup-project GH2-Main/GH2-Main.csproj \
#     --configuration Release \
#     --self-contained \
#     --runtime linux-x64 \
#     --no-build \
#     -o /app/publish/efbundle

# # Runtime Stage
# FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

# WORKDIR /app

# COPY --from=build /app/publish .

# COPY entrypoint.sh .
# RUN chmod +x entrypoint.sh

# ENV ASPNETCORE_URLS=http://+:7144

# EXPOSE 7144

# ENTRYPOINT ["./entrypoint.sh"]


# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

COPY . .

# Restore each project individually with linux-x64 runtime
RUN dotnet restore GH2-Main/GH2-Main.csproj -r linux-x64
RUN dotnet restore Infrastructure/Infrastructure.csproj -r linux-x64

RUN dotnet publish GH2-Main/GH2-Main.csproj -c Release -r linux-x64 --no-self-contained -o /app/publish

# Install EF tools
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"

# Bundle migrations (no --no-build, let it build with correct runtime)
RUN dotnet ef migrations bundle \
    --project Infrastructure/Infrastructure.csproj \
    --startup-project GH2-Main/GH2-Main.csproj \
    --self-contained \
    --runtime linux-x64 \
    -o /app/publish/efbundle

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

WORKDIR /app

RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

COPY entrypoint.sh .
RUN chmod +x entrypoint.sh

ENV ASPNETCORE_URLS=http://+:7144

EXPOSE 7144

ENTRYPOINT ["./entrypoint.sh"]