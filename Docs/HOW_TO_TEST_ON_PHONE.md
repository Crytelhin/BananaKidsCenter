# Как проверить проект на телефоне

## Текущее состояние
- API работает: ✅
- PostgreSQL в Docker: нужно проверить
- Твой IP (Ethernet): `192.168.100.128`
- API доступен по адресу: `http://192.168.100.128:5000`

---

## Шаг 1 — Убедись, что всё на одной сети

Телефон и ноутбук должны быть в **одной Wi-Fi сети** (или одной локальной сети).

1. Подключи ноутбук к Wi-Fi (сейчас он на Ethernet `192.168.100.128`)
2. Подключи телефон к **той же Wi-Fi сети**
3. После подключения к Wi-Fi узнай новый IP ноутбука:
   - Открой PowerShell/Terminal
   - Выполни: `ipconfig`
   - Найди раздел "Беспроводная сеть Wi-Fi" → "IPv4-адрес"
   - Например: `192.168.1.105`
4. С телефона открой браузер и перейди на: `http://<ТВОЙ_IP>:5000/api/zones`
   - Должен вернуть JSON: `[{"id":2,"name":"Test Zone",...}]`

Если браузер телефона показывает JSON — API работает и сеть настроена.

---

## Шаг 2 — Открой порт в брандмауэре

Запусти **от имени Администратора** в PowerShell:

```powershell
netsh advfirewall firewall add rule name="EntertainmentCenter API" dir=in action=allow protocol=TCP localport=5000
```

Проверь, что правило добавилось:
```powershell
netsh advfirewall firewall show rule name="EntertainmentCenter API"
```

Если порт уже был открыт, команда скажет об этом — это нормально.

---

## Шаг 3 — Запусти API (на ноутбуке)

**Вариант А — Запуск вручную** (для теста):
```powershell
cd C:\EntertainmentCenter\Api
.\EntertainmentCenter.Api.exe
```

**Вариант Б — Установка как Windows Service** (автозапуск при включении ноутбука).
Запусти в PowerShell **от имени Администратора**:
```powershell
sc create EntertainmentCenterApi binPath="C:\EntertainmentCenter\Api\EntertainmentCenter.Api.exe" start=auto
sc start EntertainmentCenterApi
```

Оставь PowerShell открытым — API должен работать, пока открыто окно.

---

## Шаг 4 — Собери и установи MAUI приложение на телефон

### 4.1 — Настройка телефона
На Android-телефоне:
1. **Настройки → О телефоне → Информация о ПО**
2. Нажми 7 раз на "Номер сборки" — появится меню "Для разработчиков"
3. **Настройки → Для разработчиков → Включить отладку по USB**

### 4.2 — Подключи телефон по USB к ноутбуку
- При подключении телефон спросит "Разрешить отладку USB?" — нажми **Разрешить**
- На ноутбуке выполни: `adb devices`
- Должен появиться твой телефон в списке

*(Если adb не установлен, установи Android SDK Platform Tools)*

### 4.3 — Измени URL API на IP ноутбука
В файле `C:\Work\MothersProject\EntertainmentCenter.MAUI\EntertainmentCenter.MAUI\Services\ApiConstants.cs`:
```csharp
public const string BaseUrl = "http://192.168.X.X:5000";  // замени на свой IP из Шага 1
```

### 4.4 — Собери и установи APK
В PowerShell:
```powershell
cd C:\Work\MothersProject\EntertainmentCenter.MAUI\EntertainmentCenter.MAUI
dotnet build -c Release -f net9.0-android
```

Затем установи APK на телефон (найдёт и установит):
```powershell
adb install bin\Release\net9.0-android\com.companyname.entertainmentcenter-Signed.apk
```

---

## Шаг 5 — Проверка работы

Открой приложение на телефоне:

| Страница | Что проверить |
|----------|--------------|
| **Регистрация** | Две вкладки внизу: "Регистрация" и "Контроль входа" |
| **Добавить клиента** | Нажать "Добавить клиента" — поля ФИО, Телефон, выбор Зоны, Тарифа, Акции |
| **Контроль входа** | Вкладка "Контроль входа" — сканер штрихкода |

Если видишь ошибку "Нет подключения к серверу":
- Проверь, что телефон и ноутбук в одной сети
- Проверь, что порт 5000 открыт в брандмауэре (Шаг 2)
- Проверь IP в ApiConstants.cs (Шаг 4.3)

---

## Быстрый тест всего стека

1. **Запусти PostgreSQL** (если не запущен):
   ```powershell
   docker start entertainment-center-pg
   ```
2. **Запусти API**: `cd C:\EntertainmentCenter\Api && .\EntertainmentCenter.Api.exe`
3. **Открой на телефоне браузер**: `http://<ТВОЙ_IP>:5000/api/zones`
4. Видишь JSON `[{"id":2,...}]` → всё работает, можно собирать MAUI приложение
