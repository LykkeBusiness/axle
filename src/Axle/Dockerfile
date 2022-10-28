FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
COPY . ./
RUN cp cert/*.crt /usr/local/share/ca-certificates/ 2>/dev/null \
&& update-ca-certificates \
|| echo "No specific certificate given."
WORKDIR /src/Axle
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app
COPY --from=build /app .
RUN cp cert/*.crt /usr/local/share/ca-certificates/ 2>/dev/null \
&& update-ca-certificates \
|| echo "No specific certificate given."
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENTRYPOINT ["dotnet", "Axle.dll"]
