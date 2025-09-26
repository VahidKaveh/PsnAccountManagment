# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy all .csproj files and restore dependencies
COPY ["src/PsnAccountManager.Admin.Panel/PsnAccountManager.Admin.Panel.csproj", "src/PsnAccountManager.Admin.Panel/"]
COPY ["src/PsnAccountManager.Application/PsnAccountManager.Application.csproj", "src/PsnAccountManager.Application/"]
COPY ["src/PsnAccountManager.Domain/PsnAccountManager.Domain.csproj", "src/PsnAccountManager.Domain/"]
COPY ["src/PsnAccountManager.Infrastructure/PsnAccountManager.Infrastructure.csproj", "src/PsnAccountManager.Infrastructure/"]
COPY ["src/PsnAccountManager.Shared/PsnAccountManager.Shared.csproj", "src/PsnAccountManager.Shared/"]
COPY ["PsnAccountManager.sln", "."]
RUN dotnet restore "PsnAccountManager.sln"

# Copy the rest of the source code
COPY . .
WORKDIR "/src/src/PsnAccountManager.Admin.Panel"
RUN dotnet build "PsnAccountManager.Admin.Panel.csproj" -c Release -o /app/build

# Stage 2: Publish the application
FROM build AS publish
RUN dotnet publish "PsnAccountManager.Admin.Panel.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Create the final image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PsnAccountManager.Admin.Panel.dll"]