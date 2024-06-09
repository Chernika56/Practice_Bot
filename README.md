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
