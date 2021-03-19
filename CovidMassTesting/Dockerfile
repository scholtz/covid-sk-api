#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0-focal AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0-focal AS build
WORKDIR /src
COPY . .
RUN apt-get update && apt dist-upgrade -y && apt-get install -y libgdiplus
RUN dotnet restore "CovidMassTesting.sln"
RUN dotnet build "CovidMassTesting.sln" -c Release -o /app/build
ENV TZ=Europe/Bratislava
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone
RUN dotnet test "CovidMassTesting.sln" -c Release 

FROM build AS publish
RUN dotnet publish "CovidMassTesting.sln" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN apt-get update && apt dist-upgrade -y && apt-get install -y libgdiplus

ENV TZ=Europe/Bratislava
ENV LANG sk_SK.UTF-8
ENV LANGUAGE ${LANG}
ENV LC_ALL ${LANG}
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone

ENTRYPOINT ["dotnet", "CovidMassTesting.dll"]