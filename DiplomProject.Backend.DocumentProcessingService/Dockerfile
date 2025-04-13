# Используем корень решения в качестве контекста сборки
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем файлы csproj для обоих проектов
COPY ["DiplomProject.Backend.DocumentProcessingService/DiplomProject.Backend.DocumentProcessingService.csproj", "DiplomProject.Backend.DocumentProcessingService/"]
COPY ["DiplomProject.DTOLibrary/DiplomProject.DTOLibrary.csproj", "DiplomProject.DTOLibrary/"]

# Восстанавливаем зависимости для проекта DocumentProcessingService
RUN dotnet restore "DiplomProject.Backend.DocumentProcessingService/DiplomProject.Backend.DocumentProcessingService.csproj"

# Копируем все файлы решения
COPY . .

# Переходим в папку проекта и собираем его
WORKDIR "/src/DiplomProject.Backend.DocumentProcessingService"
RUN dotnet build "DiplomProject.Backend.DocumentProcessingService.csproj" -c Release -o /app/build

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/build .
ENTRYPOINT ["dotnet", "DiplomProject.Backend.DocumentProcessingService.dll"]