FROM node:20-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .

ARG VITE_API_BASE_URL
ARG VITE_QR_API_BASE_URL
ARG VITE_PLATE_API_BASE_URL
ARG VITE_FACE_API_BASE_URL

ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
ENV VITE_QR_API_BASE_URL=$VITE_QR_API_BASE_URL
ENV VITE_PLATE_API_BASE_URL=$VITE_PLATE_API_BASE_URL
ENV VITE_FACE_API_BASE_URL=$VITE_FACE_API_BASE_URL

RUN npm run build

FROM nginx:1.27-alpine
ENV GO2RTC_PROXY_BASE=http://go2rtc:1984
COPY nginx/default.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
