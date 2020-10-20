FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 8080

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY ["Audiophile.Web/Audiophile.Web.csproj", "Audiophile.Web/"]
RUN dotnet restore "Audiophile.Web/Audiophile.Web.csproj"
COPY . .
WORKDIR "/src/Audiophile.Web"
RUN dotnet build "Audiophile.Web.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "Audiophile.Web.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Audiophile.Web.dll"]
