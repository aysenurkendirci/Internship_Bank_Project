FROM node:22-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npx ng build bank-app


# Static files plus a reverse proxy: the browser talks to one origin, so the API needs no CORS rule in this setup.
FROM nginx:1.27-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/bank-app/browser /usr/share/nginx/html
EXPOSE 80
