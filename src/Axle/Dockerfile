FROM microsoft/dotnet:2.1.5-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/dotnet:2.1.403-sdk AS build
WORKDIR /src
COPY . ./
WORKDIR /src/Axle
RUN dotnet build -c Release -r linux-x64 -o /app

FROM build AS publish
RUN dotnet publish -c Release -r linux-x64 -o /app

FROM publish AS final
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Axle.dll"]