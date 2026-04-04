# BUILD
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

ENV CI=true

COPY docs/website/CodeBeam.UltimateAuth.Docs.Wasm/CodeBeam.UltimateAuth.Docs.Wasm/*.csproj CodeBeam.Website/
COPY docs/website/CodeBeam.UltimateAuth.Docs.Wasm/CodeBeam.UltimateAuth.Docs.Wasm.Client/*.csproj CodeBeam.Website.Client/

RUN dotnet restore CodeBeam.Website/CodeBeam.UltimateAuth.Docs.Wasm.csproj

COPY . .

RUN dotnet publish docs/website/CodeBeam.UltimateAuth.Docs.Wasm/CodeBeam.UltimateAuth.Docs.Wasm/CodeBeam.UltimateAuth.Docs.Wasm.csproj \
    -c Release -o /app/publish

# RUNTIME
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "CodeBeam.UltimateAuth.Docs.Wasm.dll"]
