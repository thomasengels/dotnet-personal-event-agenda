# Build context must be the Agenda/ folder so the referenced Agenda.Domain project is available.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first to leverage layer caching for restore.
COPY Agenda.Api/Agenda.Api.csproj Agenda.Api/
COPY Agenda.Domain/Agenda.Domain.csproj Agenda.Domain/
RUN dotnet restore Agenda.Api/Agenda.Api.csproj

# Copy the rest of the source and publish.
COPY . .
RUN dotnet publish Agenda.Api/Agenda.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# ASP.NET Core container images listen on port 8080 by default.
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Agenda.Api.dll"]
