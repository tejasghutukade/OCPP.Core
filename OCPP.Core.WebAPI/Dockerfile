FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["OCPP.Core.WebAPI/OCPP.Core.WebAPI.csproj", "OCPP.Core.WebAPI/"]
RUN dotnet restore "OCPP.Core.WebAPI/OCPP.Core.WebAPI.csproj"
COPY . .
WORKDIR "/src/OCPP.Core.WebAPI"
RUN dotnet build "OCPP.Core.WebAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OCPP.Core.WebAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OCPP.Core.WebAPI.dll"]
