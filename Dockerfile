# ── Build stage ──────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY KayAyiti.csproj .
RUN dotnet restore KayAyiti.csproj

COPY . .
RUN dotnet publish KayAyiti.csproj -c Release -o /app/publish

# ── Runtime stage ─────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Kreye dosye pou uploads
RUN mkdir -p wwwroot/uploads/houses wwwroot/uploads/payments
RUN mkdir -p private_uploads/kyc-id private_uploads/kyc-selfie private_uploads/house-ownership

COPY --from=build /app/publish .

EXPOSE 80
EXPOSE 443

ENTRYPOINT ["dotnet", "KayAyiti.dll"]