FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

COPY build-an-app/v14-cqrs/src/TaskTracker.Api/TaskTracker.Api.csproj build-an-app/v14-cqrs/src/TaskTracker.Api/
RUN dotnet restore build-an-app/v14-cqrs/src/TaskTracker.Api/TaskTracker.Api.csproj

COPY build-an-app/v14-cqrs/src/TaskTracker.Api/ build-an-app/v14-cqrs/src/TaskTracker.Api/
RUN dotnet publish build-an-app/v14-cqrs/src/TaskTracker.Api/TaskTracker.Api.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ConnectionStrings__TaskTracker="Data Source=/data/tasktracker.db"

EXPOSE 8080
VOLUME ["/data"]

ENTRYPOINT ["dotnet", "TaskTracker.Api.dll"]

