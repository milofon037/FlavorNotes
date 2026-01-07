# FlavorNotes - Recipe Book REST API

## Описание проекта

FlavorNotes - это REST API сервис для управления рецептами, разработанный на **ASP.NET Core 8.0**. Проект реализует полнофункциональное CRUD приложение с поддержкой аутентификации, авторизации, кэширования и мониторинга.

## Технологический стек

### Backend
- **ASP.NET Core 8.0 Web API** - фреймворк для разработки REST API
- **Entity Framework Core 8.0** - ORM для работы с базой данных
- **Dapper** - микро-ORM для выполнения сложных запросов с транзакциями
- **FluentValidation** - валидация входных данных
- **Swagger/OpenAPI** - документирование API

### База данных
- **PostgreSQL 15** - основная база данных
- **Liquibase** - управление миграциями схемы БД
- **Redis** - кэширование и распределённое хранилище сессий

### Безопасность
- **JWT (Bearer Token)** - аутентификация для пользователей системы
- **API Key** - аутентификация для системных клиентов
- **BCrypt** - хеширование паролей

### Инфраструктура
- **Docker & Docker Compose** - контейнеризация и оркестрация сервисов
- **Serilog** - структурированное логирование
- **prometheus-net** - сбор метрик для Prometheus

### Дополнительные возможности
- **Rate Limiting** - ограничение частоты запросов (100 в минуту)
- **Idempotency** - поддержка идемпотентности через заголовок `Idempotency-Key`
- **Health Checks** - проверка доступности API, PostgreSQL и Redis
- **Caching** - Redis кэширование GET запросов с инвалидацией

## Архитектура

```
/Controllers           - HTTP контроллеры
/Services              - бизнес-логика и сервисы
/Repositories          - слой доступа к данным
/Models/Entities       - сущности EF Core
/DTO                   - данные для запросов/ответов
/Middleware            - custom middleware (логирование, обработка ошибок, идемпотентность)
/Auth                  - компоненты аутентификации
/Validators            - FluentValidation валидаторы
/Data                  - ApplicationDbContext
```

### Архитектурные принципы
- ✅ **Разделение слоёв** - Controllers → Services → Repositories → EF Core/Dapper
- ✅ **Бизнес-логика в Services** - контроллеры только вызывают сервисы
- ✅ **DTO для всех операций** - типизированные запросы/ответы
- ✅ **Асинхронность** - async/await повсюду
- ✅ **Централизованная обработка ошибок** - ErrorHandlingMiddleware

## Аутентификация и авторизация

### JWT Bearer Token
```bash
POST /api/auth/register
POST /api/auth/login
```

**Роли пользователей:**
- `Admin` - полный доступ ко всем операциям
- `Manager` - создание и редактирование рецептов, управление категориями
- `User` - просмотр и создание рецептов

**Матрица доступа:**
| Роль    | Read | Create | Update | Delete |
|---------|------|--------|--------|--------|
| Admin   | ✅   | ✅     | ✅     | ✅     |
| Manager | ✅   | ✅     | ✅     | ❌     |
| User    | ✅   | ✅     | ❌     | ❌     |

### API Key
Передаётся в заголовке:
```
X-API-KEY: your-api-key-here
```

## Структура базы данных

### Основные таблицы
- `users` - пользователи системы
- `categories` - категории рецептов
- `recipes` - рецепты
- `ingredients` - ингредиенты
- `units` - единицы измерения
- `tags` - теги рецептов
- `instruction_steps` - пошаговые инструкции
- `api_keys` - API ключи для системных клиентов

### Связи
- **1-ко-многим**: User → Recipes, Category → Recipes, Recipe → InstructionSteps
- **Многие-ко-многим**: 
  - Recipe ↔ Ingredient (через RecipeIngredient)
  - Recipe ↔ Tag (через RecipeTag)
  - User ↔ Recipe (через UserFavoriteRecipe)

## API Endpoints

### Аутентификация
```
POST   /api/auth/register          - Регистрация пользователя
POST   /api/auth/login             - Вход пользователя
```

### Рецепты
```
GET    /api/recipes?page=1&pageSize=10&search=  - Получение списка с пагинацией
GET    /api/recipes/{id}                        - Получение рецепта по ID
POST   /api/recipes                             - Создание рецепта
PUT    /api/recipes/{id}                        - Обновление рецепта
DELETE /api/recipes/{id}                        - Удаление рецепта
```

### Категории
```
GET    /api/categories             - Получение всех категорий
GET    /api/categories/{id}        - Получение категории по ID
POST   /api/categories             - Создание категории
PUT    /api/categories/{id}        - Обновление категории
DELETE /api/categories/{id}        - Удаление категории
```

### Health Check
```
GET    /health                     - Проверка доступности API, PostgreSQL, Redis
```

### Metrics
```
GET    /metrics                    - Prometheus метрики в формате OpenMetrics
```

## Пагинация и фильтрация

### Рецепты поддерживают пагинацию и поиск:
```bash
GET /api/recipes?page=1&pageSize=20&search=pasta
```

**Параметры:**
- `page` (int) - номер страницы (по умолчанию 1)
- `pageSize` (int) - количество элементов на странице (по умолчанию 10)
- `search` (string) - поиск по названию или описанию

**Ответ:**
```json
{
  "items": [...],
  "total": 100,
  "page": 1,
  "pageSize": 20
}
```

## Кэширование

- **GET /api/recipes** - кэшируется на 5 минут
- **GET /api/recipes/{id}** - кэшируется на 10 минут
- **GET /api/categories** - кэшируется на 15 минут
- Кэш инвалидируется при создании/обновлении/удалении данных

## Rate Limiting

- **100 запросов в минуту** для всех эндпоинтов
- Статус код при превышении: **429 Too Many Requests**

## Idempotency

POST, PUT, PATCH запросы поддерживают идемпотентность:
```bash
POST /api/recipes \
  -H "Idempotency-Key: unique-key-12345" \
  -H "Content-Type: application/json" \
  -d '{"title": "Pasta", ...}'
```

**Особенности:**
- Повторный запрос с тем же ключом вернёт кэшированный ответ вместо создания дубликата
- Ключ должен быть уникальным и содержать только буквы, цифры, дефисы, подчёркивания и точки
- Длина ключа: от 1 до 128 символов (настраивается)
- Кэш хранится 24 часа (настраивается в `appsettings.json`)
- Опциональная проверка соответствия тела запроса для предотвращения конфликтов
- Метрики доступны в Prometheus: `idempotency_requests_total`, `idempotency_request_duration_seconds`

**Пример ответа при повторном запросе:**
Возвращается тот же статус код и тело ответа, что и при первом запросе.

## Логирование

Все события логируются:
- ✅ Входящие HTTP запросы
- ✅ Ошибки и исключения
- ✅ Бизнес-события (создание, обновление, удаление)

Логи структурированы и содержат:
- Timestamp
- Log Level
- Message
- Exception details (если есть)
- Request ID / Trace ID

## Быстрый старт

### Требования
- Docker & Docker Compose
- .NET 8.0 SDK (для локальной разработки)

### Запуск с Docker Compose

1. **Клонируйте репозиторий**
```bash
cd /home/milofon/progr_2_0/dotnet_learn/pr
```

2. **Запустите все сервисы**
```bash
docker-compose up -d
```

Это поднимет:
- PostgreSQL (порт 5432)
- Redis (порт 6379)
- Liquibase (выполнит миграции)
- API (порт 5000)

3. **Проверьте доступность**
```bash
curl http://localhost:5000/health
```

4. **Откройте Swagger UI**
```
http://localhost:5000/swagger
```

### Локальная разработка

1. **Установите зависимости**
```bash
dotnet restore
```

2. **Примените миграции** (вручную, если не используете Liquibase)
```bash
dotnet ef database update
```

3. **Запустите проект**
```bash
dotnet run
```

API будет доступен на `http://localhost:5000`

## Тестирование

### Unit Tests

```bash
cd FlavorNotes.Tests
dotnet test
```

**Покрытие:**
- UserRepository - 8 тестов (CRUD, проверка уникальности)
- CategoryRepository - 8 тестов (полный CRUD цикл)

### Интеграционные тесты

```bash
# Регистрация пользователя
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "email": "test@example.com",
    "password": "Password123!",
    "passwordConfirm": "Password123!"
  }'

# Вход
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "password": "Password123!"
  }'

# Получение рецептов
curl -X GET http://localhost:5000/api/recipes \
  -H "Authorization: Bearer {token}"
```

## Примеры API запросов

### 1. Регистрация и вход

```bash
# Регистрация
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "chef123",
    "email": "chef@example.com",
    "password": "SecurePass123!",
    "passwordConfirm": "SecurePass123!"
  }'

# Ответ
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "expiresIn": 86400,
  "tokenType": "Bearer",
  "userId": 1,
  "username": "chef123",
  "role": "User"
}
```

### 2. Создание рецепта

```bash
curl -X POST http://localhost:5000/api/recipes \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "categoryId": 1,
    "title": "Паста Карбонара",
    "description": "Классический итальянский рецепт",
    "prepTimeMinutes": 10,
    "cookTimeMinutes": 20,
    "servings": 4,
    "ingredients": [
      {
        "ingredientId": 1,
        "unitId": 1,
        "quantity": 400
      }
    ],
    "tagIds": [1, 2],
    "instructions": [
      {
        "stepNumber": 1,
        "instructionText": "Сварите пасту..."
      }
    ]
  }'
```

### 3. Получение рецептов с фильтрацией

```bash
curl -X GET "http://localhost:5000/api/recipes?page=1&pageSize=10&search=паста" \
  -H "Authorization: Bearer {token}"
```

### 4. Использование API Key

```bash
curl -X GET http://localhost:5000/api/recipes \
  -H "X-API-KEY: your-api-key-here"
```

## Мониторинг и метрики

### Prometheus

Метрики доступны на `/metrics` в формате Prometheus:
```
http_requests_total - общее количество HTTP запросов
http_request_duration_seconds - время обработки запросов
http_requests_in_progress - активные запросы
```

### Health Check

```bash
curl http://localhost:5000/health
```

**Ответ:**
```json
{
  "status": "Healthy",
  "checks": {
    "PostgreSQL": "Healthy",
    "Redis": "Healthy"
  }
}
```

## Обработка ошибок

Все ошибки возвращаются в единообразном формате:

```json
{
  "error": "BadRequest",
  "message": "Title cannot be empty",
  "traceId": "0HN3QAL4FNJRM:00000001",
  "errors": {
    "Title": ["Title cannot be empty"]
  }
}
```

**Статус коды:**
- `200 OK` - успешная операция
- `201 Created` - ресурс создан
- `204 No Content` - успешное удаление
- `400 Bad Request` - ошибка валидации
- `401 Unauthorized` - не авторизован
- `403 Forbidden` - нет доступа
- `404 Not Found` - ресурс не найден
- `409 Conflict` - конфликт (например, дублирование)
- `429 Too Many Requests` - превышен rate limit
- `500 Internal Server Error` - ошибка сервера

## Конфигурация

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=postgres;Port=5432;Database=flavornotes;Username=postgres;Password=postgres",
    "Redis": "redis:6379"
  },
  "Jwt": {
    "Key": "YourSuperSecretKeyThatShouldBeAtLeast32CharactersLong!",
    "Issuer": "FlavorNotes",
    "Audience": "FlavorNotes"
  },
  "IpRateLimiting": {
    "GeneralRules": [
      {
        "Endpoint": "*",
        "Period": "1m",
        "Limit": 100
      }
    ]
  }
}
```

### Environment Variables (Docker)

Все переменные окружения устанавливаются в `docker-compose.yml`:
- `ASPNETCORE_ENVIRONMENT=Development`
- `ASPNETCORE_URLS=http://+:8080`
- `ConnectionStrings__DefaultConnection=...`
- `ConnectionStrings__Redis=redis:6379`

## Требования к компилированию

Проект использует:
- `.NET 8.0` TFM
- `Nullable reference types` - включены
- `Implicit usings` - включены

## Дополнительные возможности (бонусы)

✅ **Rate Limiting** - 100 запросов в минуту
✅ **Idempotency** - поддержка через заголовок Idempotency-Key
✅ **Dapper с транзакциями** - создание рецептов через Dapper с полной транзакцией
✅ **Prometheus метрики** - /metrics эндпоинт
✅ **Health Checks** - проверка API, PostgreSQL, Redis
✅ **Structured Logging** - Serilog с структурированными логами
✅ **Redis Caching** - кэширование GET запросов с инвалидацией

## Структура проекта

```
pr/
├── FlavorNotes/                  # Основной проект API
│   ├── Controllers/              # HTTP контроллеры
│   ├── Services/                 # Бизнес-логика
│   │   └── Interfaces/          # Интерфейсы сервисов
│   ├── Repositories/             # Слой доступа к данным
│   │   └── Interfaces/          # Интерфейсы репозиториев
│   ├── Models/
│   │   └── Entities/            # EF Core сущности (User, Recipe, Category, etc.)
│   ├── DTO/                      # Модели запросов/ответов
│   ├── Middleware/               # Custom middleware
│   ├── Auth/                     # Аутентификация (JWT, API Key)
│   ├── Validators/               # FluentValidation валидаторы
│   ├── Configuration/           # Конфигурационные классы
│   ├── Swagger/                  # Swagger/OpenAPI настройки
│   ├── Data/                     # DbContext, DataSeeder
│   ├── Program.cs                # Конфигурация приложения
│   ├── appsettings.json          # Параметры конфигурации
│   └── Dockerfile                # Контейнеризация
├── FlavorNotes.Tests/            # Unit тесты
├── liquibase/changelog/          # Миграции БД
├── docker-compose.yml            # Оркестрация сервисов
└── README.md                     # Этот файл
```

## Разработка

### Добавление нового API endpoint'а

1. **Создайте DTO в `/DTO`**
2. **Создайте метод в Service**
3. **Создайте метод в Repository** (если нужно взаимодействие с БД)
4. **Добавьте endpoint в Controller**
5. **Добавьте валидатор в `/Validators`** (если нужно)
6. **Напишите unit тесты**

### Миграции БД

Используется Liquibase. Новые миграции:
1. Создайте XML файл в `liquibase/changelog/`
2. Добавьте include в `changelog-master.xml`
3. Перезапустите docker-compose
