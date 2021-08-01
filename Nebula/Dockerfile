#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim AS base
WORKDIR /app
# use nginx as proxy
EXPOSE 5100

ARG LYRA_NETWORK
ARG LYRA_CAPCHA_SECRET
ARG LYRA_DB_NAME
ARG LYRA_DB_USER
ARG LYRA_DB_PASSWORD
ARG LYRA_IPDB_FN

ENV NEBULA_network $LYRA_NETWORK
ENV NEBULA_reCAPTCHA__Secret $LYRA_CAPCHA_SECRET
ENV NEBULA_Lyra__Mongodb "mongodb://${LYRA_DB_USER}:${LYRA_DB_PASSWORD}@127.0.0.1/${LYRA_DB_NAME}"
ENV NEBULA_ipdb $LYRA_IPDB_FN
ENV ASPNETCORE_ENVIRONMENT "Production"
# ENV DOTNET_PRINT_TELEMETRY_MESSAGE "false"

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["Nebula/Nebula.csproj", "Nebula/"]
RUN dotnet restore "Nebula/Nebula.csproj"
COPY . .
WORKDIR "/src/Nebula"
RUN dotnet build "Nebula.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Nebula.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Nebula.dll", "--urls", "http://*:5100"]