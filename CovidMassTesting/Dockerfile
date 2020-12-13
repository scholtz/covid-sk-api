#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:3.1 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/aspnet:3.1 AS build
WORKDIR /src
COPY ["CovidMassTesting/CovidMassTesting.csproj", "CovidMassTesting/"]
RUN dotnet restore "CovidMassTesting/CovidMassTesting.csproj"
COPY . .
WORKDIR "/src/CovidMassTesting"
RUN dotnet build "CovidMassTesting.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CovidMassTesting.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CovidMassTesting.dll"]