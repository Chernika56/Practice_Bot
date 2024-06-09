# Telegram Bot

Link to the Bot: https://t.me/Monsys_Practice_bot

## Prerequisites

- Changing the database access details in the `data.json` file
- It was developed using .Net 8.0

## Configuration

Create an `appsettings.json` file in the root directory of the project with the following content:

```json
{
  "BotToken": "YOUR_TELEGRAM_BOT_TOKEN",
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=YOUR_DATABASE;User=USER;Password=YOUR_PASSWORD;CharSet=utf8mb4;"
  }
}
```
