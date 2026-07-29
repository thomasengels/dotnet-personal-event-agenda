# Build context must be the Event/ folder so the referenced Event.Domain project is available.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first to leverage layer caching for restore.
COPY Event.Api/Event.Api.csproj Event.Api/
COPY Event.Domain/Event.Domain.csproj Event.Domain/
RUN dotnet restore Event.Api/Event.Api.csproj

# Copy the rest of the source and publish.
COPY . .
RUN dotnet publish Event.Api/Event.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# ASP.NET Core container images listen on port 8080 by default.
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Event.Api.dll"]
