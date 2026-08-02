FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY WH_Logistic.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/data
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/WH_Logistic.db"
ENV ASPNETCORE_URLS=http://+:10000
ENV ASPNETCORE_ENVIRONMENT=Production
ENV TZ=Asia/Bangkok
EXPOSE 10000
ENTRYPOINT ["dotnet", "WH_Logistic.dll"]
