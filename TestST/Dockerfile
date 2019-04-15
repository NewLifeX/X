FROM mcr.microsoft.com/dotnet/core/runtime:2.1-stretch-slim AS base
WORKDIR /app


FROM mcr.microsoft.com/dotnet/core/sdk:2.1-stretch AS build
WORKDIR /src
COPY ["TestST/TestST.csproj", "TestST/"]
COPY ["NewLife.Agent/NewLife.Agent.csproj", "NewLife.Agent/"]
COPY ["NewLife.Core/NewLife.Core.csproj", "NewLife.Core/"]
COPY ["XCode/XCode.csproj", "XCode/"]
RUN dotnet restore "TestST/TestST.csproj"
COPY . .
WORKDIR "/src/TestST"
RUN dotnet build "TestST.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "TestST.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "TestST.dll"]