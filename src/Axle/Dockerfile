FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
COPY . ./
WORKDIR /src/Axle
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app
COPY --from=build /app .
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENTRYPOINT ["dotnet", "Axle.dll"]