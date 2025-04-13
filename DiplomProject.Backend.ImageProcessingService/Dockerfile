# Используем корень решения в качестве контекста сборки
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем файлы csproj для обоих проектов
COPY ["DiplomProject.Backend.ImageProcessingService/DiplomProject.Backend.ImageProcessingService.csproj", "DiplomProject.Backend.ImageProcessingService/"]
COPY ["DiplomProject.DTOLibrary/DiplomProject.DTOLibrary.csproj", "DiplomProject.DTOLibrary/"]

# Восстанавливаем зависимости для проекта ImageProcessingService
RUN dotnet restore "DiplomProject.Backend.ImageProcessingService/DiplomProject.Backend.ImageProcessingService.csproj"

# Копируем все файлы решения
COPY . .

# Переходим в папку проекта и собираем его
WORKDIR "/src/DiplomProject.Backend.ImageProcessingService"
RUN dotnet build "DiplomProject.Backend.ImageProcessingService.csproj" -c Release -o /app/build

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/build .
ENTRYPOINT ["dotnet", "DiplomProject.Backend.ImageProcessingService.dll"]