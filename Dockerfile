FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Flew2Bits.OrchardCore/Flew2Bits.OrchardCore/Flew2Bits.OrchardCore.csproj", "Flew2Bits.OrchardCore/"]
COPY . .
RUN ls -R
RUN dotnet restore Flew2Bits.OrchardCore/Flew2Bits.OrchardCore.csproj
WORKDIR /src/Flew2Bits.OrchardCore
RUN dotnet build Flew2Bits.OrchardCore.csproj -c Release -o /app/build

FROM build AS publish
RUN dotnet publish Flew2Bits.OrchardCore.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Flew2Bits.OrchardCore.dll"]
