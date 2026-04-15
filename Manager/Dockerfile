FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["Manager/Manager.csproj", "Manager/"]
RUN dotnet restore "Manager/Manager.csproj"

COPY . .
WORKDIR "/src/Manager"
RUN dotnet publish "Manager.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Manager.dll"]
