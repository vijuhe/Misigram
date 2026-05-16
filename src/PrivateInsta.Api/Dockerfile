FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/PrivateInsta.Api/PrivateInsta.Api.csproj", "src/PrivateInsta.Api/"]
COPY ["nuget.config", "."]
RUN dotnet restore "src/PrivateInsta.Api/PrivateInsta.Api.csproj"

COPY src/PrivateInsta.Api/ src/PrivateInsta.Api/
WORKDIR /src/src/PrivateInsta.Api
RUN dotnet publish -c Release -o /app/publish --nologo

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "PrivateInsta.Api.dll"]
