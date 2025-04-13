# Этап сборки
FROM node:18-alpine as build
WORKDIR /app

COPY .env.production .env.production

COPY package*.json ./
RUN npm install
COPY . .

# Здесь переменные внедряются в билд
RUN npm run build

# Этап запуска
FROM nginx:alpine
COPY --from=build /app/build /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]