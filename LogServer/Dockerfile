# Étape de compilation
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY *.csproj ./
RUN dotnet restore
COPY . ./
# Génère l'exécutable
RUN dotnet publish -c Release -o out

# Étape d'exécution (Image allégée)
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/out .

# On expose le port 8080 pour que EasySave puisse s'y connecter
EXPOSE 8080

# Lancement du serveur
ENTRYPOINT ["dotnet", "LogServer.dll"]