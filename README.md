Ссылка на Телеграмм бота: https://t.me/Monsys_Practice_bot
Изменение реквизитов доступа в БД в файле data.json
Для подключения к боту и БД нужно создать конфигурационный файл appsettings.json следующего формата:
```json
{
  "BotToken": "YOUR_TELEGRAM_BOT_TOKEN",
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=YOUR_DATABASE;User=USER;Password=YOUR_PASSWORD;CharSet=utf8mb4;"
  }
}
```
# Telegram Bot with MySQL Integration

This is a Telegram bot implemented in .NET that connects to a MySQL database and sends messages based on specific conditions every 30 minutes.

## Features

- Connects to MySQL database.
- Retrieves records for specific tags.
- Sends Telegram messages based on the values of the records.
- Periodically checks the database every 30 minutes.

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) installed.
- MySQL database setup.
- Telegram bot token.

## Configuration

Create an `appsettings.json` file in the root directory of the project with the following content:

```json
{
  "BotToken": "YOUR_TELEGRAM_BOT_TOKEN",
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=monsys;User=remote_user;Password=YOUR_PASSWORD;CharSet=utf8mb4;"
  }
}
