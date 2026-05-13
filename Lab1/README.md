# Lab1 — Simple Inventory Service

## Варіант індивідуального завдання

**N = 2**

| Формула | Значення | Що визначає |
|---------|----------|-------------|
| V2 = (2 % 2) + 1 = **1** | 1 | Конфігурація через **аргументи командного рядка**; СУБД — **MariaDB** |
| V3 = (2 % 3) + 1 = **3** | 3 | Тематика застосунку — **Simple Inventory** |
| V5 = (2 % 5) + 1 = **3** | 3 | Порт застосунку — **3000** |

### Конкретне завдання

- Веб-застосунок: **Simple Inventory** — облік обладнання
- Конфігурація: аргументи командного рядка
- Порт: **3000** (прив'язка до 127.0.0.1)
- База даних: **MariaDB**, порт 3306
- Reverse proxy: **Nginx** на порту 80

---

## Призначення застосунку

Simple Inventory — REST API сервіс для обліку інвентарю (обладнання). Дозволяє зберігати предмети з назвою та кількістю, отримувати їх список та детальну інформацію.

Об'єкт інвентарю:

| Поле | Тип | Опис |
|------|-----|------|
| `id` | INT | Унікальний ідентифікатор (auto increment) |
| `name` | VARCHAR(255) | Назва предмету |
| `quantity` | INT | Кількість одиниць |
| `created_at` | DATETIME | Час створення (UTC) |

---

## API Endpoints

### Бізнес-логіка

| Метод | Шлях | Опис |
|-------|------|------|
| `GET` | `/` | Список усіх ендпоінтів (тільки HTML) |
| `GET` | `/items` | Список всіх предметів (id, name) |
| `POST` | `/items` | Створити новий запис |
| `GET` | `/items/{id}` | Повна інформація по запису |

### Health checks (тільки з ВМ, не через nginx)

| Метод | Шлях | Опис |
|-------|------|------|
| `GET` | `/health/alive` | Завжди 200 OK |
| `GET` | `/health/ready` | 200 OK якщо БД доступна, 500 інакше |

### Content negotiation

Усі бізнес-ендпоінти підтримують заголовок `Accept`:

- `Accept: text/html` → проста HTML-сторінка (таблиця для списків)
- `Accept: application/json` → JSON відповідь

### Приклади запитів

```bash
# Список предметів (JSON)
curl -H "Accept: application/json" http://localhost/items

# Список предметів (HTML)
curl -H "Accept: text/html" http://localhost/items

# Створити предмет
curl -X POST http://localhost/items \
     -H "Content-Type: application/json" \
     -H "Accept: application/json" \
     -d '{"name": "Laptop", "quantity": 5}'

# Отримати предмет за ID
curl -H "Accept: application/json" http://localhost/items/1
```

---

## Структура репозиторію

```
Lab1/
├── src/
│   └── MyWebApp/
│       ├── Lab1.csproj
│       ├── Program.cs              # Точка входу, socket activation
│       ├── AppConfig.cs            # Парсинг CLI аргументів
│       ├── Models/
│       │   └── InventoryItem.cs
│       ├── Data/
│       │   ├── Database.cs         # Робота з MariaDB
│       │   └── DbMigrator.cs       # Скрипт міграції БД
│       ├── Helpers/
│       │   └── ContentNegotiator.cs
│       └── Controllers/
│           ├── HealthController.cs
│           ├── ItemsController.cs
│           └── RootController.cs
├── deploy/
│   ├── install.sh                  # Скрипт автоматичного розгортання
│   ├── mywebapp.service            # systemd service unit
│   ├── mywebapp.socket             # systemd socket unit (socket activation)
│   └── nginx.conf                  # Nginx reverse proxy конфіг
└── README.md
```

---

## Налаштування середовища для розробки

### Вимоги

- .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0
- MariaDB 10.6+
- (опційно) Docker для локальної БД

### Запуск MariaDB локально через Docker

```bash
docker run -d \
  --name mariadb-dev \
  -e MYSQL_ROOT_PASSWORD=rootpass \
  -e MYSQL_DATABASE=mywebapp \
  -e MYSQL_USER=mywebapp \
  -e MYSQL_PASSWORD=devpass \
  -p 3306:3306 \
  mariadb:10.11
```

### Налаштування та запуск (розробка)

```bash
# Клонувати репозиторій
git clone https://github.com/besade/KPI-DEPLOY-2026
cd KPI-DEPLOY-2026/Lab1/src/MyWebApp

# Відновити залежності
dotnet restore

# Виконати міграцію БД
dotnet run -- --migrate \
  --db-host 127.0.0.1 --db-port 3306 \
  --db-name mywebapp --db-user mywebapp --db-password devpass

# Запустити застосунок
dotnet run -- \
  --host 127.0.0.1 --port 3000 \
  --db-host 127.0.0.1 --db-port 3306 \
  --db-name mywebapp --db-user mywebapp --db-password devpass
```

### Збірка для продакшну

```bash
dotnet publish src/MyWebApp/Lab1.csproj \
  -c Release \
  --runtime linux-x64 \
  --self-contained false \
  -o /opt/mywebapp
```

---

## Аргументи командного рядка

```
mywebapp [--migrate] [--host HOST] [--port PORT]
         --db-host HOST --db-port PORT
         --db-name NAME --db-user USER --db-password PASS

  --migrate        Виконати міграцію БД та завершити роботу
  --host           Адреса для прослуховування (за замовчуванням: 127.0.0.1)
  --port           Порт для прослуховування  (за замовчуванням: 3000)
  --db-host        Хост MariaDB
  --db-port        Порт MariaDB              (за замовчуванням: 3306)
  --db-name        Назва бази даних
  --db-user        Користувач БД
  --db-password    Пароль БД
```

---

## Розгортання на віртуальній машині

### Базовий образ

Використовуйте офіційний образ **Ubuntu Server 22.04 LTS**:

- [Ubuntu Cloud Images](https://cloud-images.ubuntu.com/releases/22.04/)
- Або офіційний ISO: https://releases.ubuntu.com/22.04/

### Вимоги до ресурсів ВМ

| Ресурс | Мінімум | Рекомендовано |
|--------|---------|---------------|
| CPU | 1 vCPU | 2 vCPU |
| RAM | 1 GB | 2 GB |
| Disk | 10 GB | 20 GB |
| Мережа | 1 мережевий адаптер (NAT або Bridged) | — |

### Налаштування при встановленні ОС

Спеціальні налаштування не потрібні. Стандартне розбиття диску (ext4 або LVM). Встановити SSH-сервер при інсталяції.

### Вхід на ВМ

За замовчуванням Ubuntu Cloud Image або ISO створює користувача `ubuntu`.

```bash
# SSH (з ключем, якщо cloud image)
ssh ubuntu@<VM_IP>

# Або через консоль VirtualBox/VMware
# Логін: ubuntu
# Пароль: (встановлюється при інсталяції або через cloud-init)
```

### Запуск автоматизації

```bash
# На ВМ з правами root:
git clone https://github.com/besade/KPI-DEPLOY-2026
cd KPI-DEPLOY-2026/Lab1
sudo bash deploy/install.sh
```

Скрипт автоматично:
1. Встановить .NET 8, MariaDB, Nginx та інші залежності
2. Створить користувачів (student, teacher, operator, app)
3. Створить БД та згенерує випадковий пароль
4. Зберере та встановить застосунок у `/opt/mywebapp`
5. Налаштує systemd socket activation
6. Запустить сервіс та nginx
7. Заблокує дефолтного користувача ubuntu

### Архітектура системи

```
Internet
    │
    ▼
┌─────────┐  port 80   ┌───────────────────────┐
│  Client │ ─────────▶ │  Nginx (reverse proxy)│
└─────────┘            └──────────┬────────────┘
                                  │ 127.0.0.1:3000
                                  ▼
                        ┌──────────────────┐
                        │   mywebapp app   │  (systemd socket activation)
                        │   (C# .NET 8)    │
                        └──────────┬───────┘
                                   │ 127.0.0.1:3306
                                   ▼
                        ┌──────────────────┐
                        │    MariaDB       │
                        └──────────────────┘
```

### Мережеві обмеження

| Компонент | Адреса | Порт |
|-----------|--------|------|
| Nginx | 0.0.0.0 | 80 |
| mywebapp | 127.0.0.1 | 3000 |
| MariaDB | 127.0.0.1 | 3306 |

### Користувачі системи

| Користувач | Пароль за замовчуванням | Призначення |
|-----------|------------------------|-------------|
| `student` | `12345678` | Розробник |
| `teacher` | `12345678` | Перевірка роботи |
| `operator` | `12345678` | Управління сервісами |
| `app` | — (системний) | Виконання mywebapp |

`operator` має sudo доступ лише до:
- `systemctl start/stop/restart/status mywebapp`
- `systemctl reload nginx`

---

## Інструкція з тестування

### 1. Перевірка сервісів

```bash
# Статус systemd
systemctl status mywebapp.socket
systemctl status mywebapp
systemctl status nginx

# Перевірка прослуховування портів
ss -tlnp | grep -E '80|3000|3306'
```

### 2. Health checks (напряму до застосунку)

```bash
# Alive — має повернути 200 OK
curl -v http://127.0.0.1:3000/health/alive

# Ready — має повернути 200 OK якщо БД підключена
curl -v http://127.0.0.1:3000/health/ready
```

### 3. Бізнес-логіка через nginx

```bash
# Кореневий ендпоінт (HTML)
curl http://localhost/

# Список предметів (JSON)
curl -H "Accept: application/json" http://localhost/items

# Список предметів (HTML)
curl -H "Accept: text/html" http://localhost/items

# Створити предмет
curl -X POST http://localhost/items \
     -H "Content-Type: application/json" \
     -H "Accept: application/json" \
     -d '{"name": "Monitor", "quantity": 3}'

# Отримати деталі предмету
curl -H "Accept: application/json" http://localhost/items/1
```

### 4. Перевірка, що /health недоступний через nginx

```bash
# Має повернути 403
curl -v http://localhost/health/alive
```

### 5. Перевірка socket activation

```bash
# Зупинити сервіс (не сокет!)
sudo systemctl stop mywebapp

# Запит через nginx активує сервіс автоматично
curl http://localhost/items

# Переконатися що сервіс запустився
systemctl is-active mywebapp
```

### 6. Перевірка прав operator

```bash
su - operator
sudo systemctl status mywebapp   # OK
sudo systemctl restart mywebapp  # OK
sudo systemctl reload nginx      # OK
sudo systemctl status nginx      # Заборонено — не в sudoers
```

### 7. Логи

```bash
# Логи застосунку
journalctl -u mywebapp -f

# Логи nginx
tail -f /var/log/nginx/mywebapp.access.log
tail -f /var/log/nginx/mywebapp.error.log
```
