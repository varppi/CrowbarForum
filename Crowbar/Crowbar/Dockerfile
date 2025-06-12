# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Crowbar/Crowbar.csproj", "Crowbar/"]
RUN dotnet restore "./Crowbar/Crowbar.csproj"
COPY . .
WORKDIR "/src/Crowbar"
RUN dotnet build "./Crowbar.csproj" -c $BUILD_CONFIGURATION -o /app/build


# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Crowbar.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY . .
USER root
COPY --from=publish /app/publish .
RUN chown -R app:app .
RUN chmod -R 700 .
USER app
RUN dotnet tool install --global dotnet-ef --version 9.0.5
RUN dotnet tool restore
ENV PATH="$PATH:/home/app/.dotnet/tools"
RUN dotnet ef migrations add ApplicationDbContextMigration; exit 0;
RUN dotnet ef migrations remove; exit 0;
RUN dotnet ef migrations add ApplicationDbContextMigration; exit 0;
RUN dotnet ef database update --project Crowbar
RUN mkdir Database
RUN cd Crowbar
ENTRYPOINT ["dotnet", "Crowbar.dll"]


# Making this Dockerfile work cost me my will to live and probably knocked off 10 years off my life exptectancy.