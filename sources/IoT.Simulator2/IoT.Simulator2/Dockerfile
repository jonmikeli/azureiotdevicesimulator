FROM mcr.microsoft.com/dotnet/core/runtime:2.2-stretch-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch AS build
WORKDIR /src
COPY ["IoT.Simulator2/IoT.Simulator2.csproj", "IoT.Simulator2/"]
RUN dotnet restore "IoT.Simulator2/IoT.Simulator2.csproj"
COPY . .
WORKDIR "/src/IoT.Simulator2"
RUN dotnet build "IoT.Simulator2.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "IoT.Simulator2.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
#ENTRYPOINT ["dotnet", "IoT.Simulator2.dll"]