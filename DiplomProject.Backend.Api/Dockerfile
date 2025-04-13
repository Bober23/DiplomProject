#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.
# Используем корень решения в качестве контекста сборки
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем файлы csproj для обоих проектов
COPY ["DiplomProject.Backend.Api/DiplomProject.Backend.Api.csproj", "DiplomProject.Backend.Api/"]
COPY ["DiplomProject.DTOLibrary/DiplomProject.DTOLibrary.csproj", "DiplomProject.DTOLibrary/"]

# Восстанавливаем зависимости для проекта Api
RUN dotnet restore "DiplomProject.Backend.Api/DiplomProject.Backend.Api.csproj"

# Копируем все файлы решения
COPY . .

# Переходим в папку проекта и собираем его
WORKDIR "/src/DiplomProject.Backend.Api"
RUN dotnet build "DiplomProject.Backend.Api.csproj" -c Release -o /app/build

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/build .
ENTRYPOINT ["dotnet", "DiplomProject.Backend.Api.dll"]