# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

COPY . .

RUN dotnet restore  GH2-Main.sln

RUN dotnet publish GH2-Main/GH2-Main.csproj -c Release -o /app/publish


# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:7144

EXPOSE 7144

ENTRYPOINT ["dotnet", "GH2-Main.dll"]