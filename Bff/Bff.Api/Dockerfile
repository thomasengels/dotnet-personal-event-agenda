# Build context must be the Bff/ folder so the referenced Bff.Domain project is available.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first to leverage layer caching for restore.
COPY Bff.Api/Bff.Api.csproj Bff.Api/
COPY Bff.Domain/Bff.Domain.csproj Bff.Domain/
RUN dotnet restore Bff.Api/Bff.Api.csproj

# Copy the rest of the source and publish.
COPY . .
RUN dotnet publish Bff.Api/Bff.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# ASP.NET Core container images listen on port 8080 by default.
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Bff.Api.dll"]
