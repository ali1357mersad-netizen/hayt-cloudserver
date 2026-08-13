FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# فقط پروژه CloudServer رو کپی کن
COPY ["Hayt-CloudServer/Hayt.CloudServer.csproj", "Hayt-CloudServer/"]
COPY ["Hayt.Shared/Hayt.Shared.csproj", "Hayt.Shared/"]
RUN dotnet restore "Hayt-CloudServer/Hayt.CloudServer.csproj"

# بقیه فایلهای لازم
COPY Hayt-CloudServer/ Hayt-CloudServer/
COPY Hayt.Shared/ Hayt.Shared/

WORKDIR "/src/Hayt-CloudServer"
RUN dotnet build "Hayt.CloudServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Hayt.CloudServer.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Hayt.CloudServer.dll"]
