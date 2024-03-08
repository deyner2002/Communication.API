
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["APICommunication.csproj", "./"]
RUN dotnet restore "./APICommunication.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "APICommunication.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "APICommunication.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "APICommunication.dll"]