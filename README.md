# CrackHash (Task 1)

Решение состоит из двух сервисов:

- `Manager` - принимает клиентские запросы, делит пространство перебора и отслеживает статус.
- `Worker` - перебирает свой диапазон слов и возвращает результаты обратно менеджеру.

## Запуск

```bash
docker compose up --build
```

`Manager` будет доступен на `http://localhost:8080`.

## API менеджера

### Создать задачу

`POST /api/hash/crack`

```json
{
  "hash": "e2fc714c4727ee9395f324cd2e7f331f",
  "maxLength": 4
}
```

### Статус задачи

`GET /api/hash/status?requestId=<guid>`

## Внутренние API

- `POST /internal/api/worker/hash/crack/task` - endpoint воркера для задач от менеджера (JSON).
- `PATCH /internal/api/manager/hash/crack/request` - endpoint менеджера для ответов воркера (XML).

## Технические детали

- Алфавит: `abcdefghijklmnopqrstuvwxyz0123456789`.
- Разделение диапазона выполняется через `PartNumber / PartCount`.
- Статусы в памяти: `IN_PROGRESS`, `PARTIAL_READY`, `READY`, `ERROR`.
- После первого ответа воркера запрос переходит в `PARTIAL_READY`; когда отвечают все части - в `READY`.
- Таймаут переводит запрос в `ERROR`, если ни один воркер не успел вернуть результат.
- `Manager` сохраняет снимок состояния в файл `data/manager-state.json` (in-progress запросы и кэш результатов по хэшу).
- При повторном запросе для уже решенного хэша используется кэш результата, повторный брутфорс не запускается.
