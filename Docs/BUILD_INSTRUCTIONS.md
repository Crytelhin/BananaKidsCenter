# Build & Deploy Instructions

## Prerequisites

- **dotnet**: `C:/Program Files/dotnet/dotnet.exe`
- **adb**: `C:/Users/Valeriy/AppData/Local/Android/Sdk/platform-tools/adb.exe`
- **Docker**: PostgreSQL container `entertainment-center-pg` (port 5432, user/pass: postgres/postgres123)
  - Docker CLI path: `C:/Program Files/Docker/Docker/resources/bin/docker.exe` (if not in PATH)
- **Phone IP**: `192.168.100.128:5000` (Ethernet adapter — may change, check with PowerShell below)

## Check current IP

```powershell
powershell -Command "Get-NetIPAddress -AddressFamily IPv4 | Select-Object IPAddress,InterfaceAlias"
```

Look for the `Ethernet` entry (usually `192.168.100.x`).

---

## Stop running API (if locked)

```bash
# Kill by name (preferred):
cmd.exe //c "taskkill /F /IM EntertainmentCenter.API.exe"

# Kill any leftover dotnet processes:
powershell -Command "Get-Process dotnet -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id \$_.Id -Force }"
```

---

## Build API

```bash
"C:/Program Files/dotnet/dotnet.exe" build C:/Work/MothersProject/EntertainmentCenter.API/EntertainmentCenter.API.csproj
```

## Start API (background)

```bash
"C:/Program Files/dotnet/dotnet.exe" run --project C:/Work/MothersProject/EntertainmentCenter.API/EntertainmentCenter.API.csproj --urls "http://0.0.0.0:5000" &
```

## Verify API is running

```bash
curl -s http://localhost:5000/api/sessions/active
```

## Start PostgreSQL (if not running)

```bash
docker start entertainment-center-pg
# Alternative with full path:
"/c/Program Files/Docker/Docker/resources/bin/docker.exe" start entertainment-center-pg
```

## Stop PostgreSQL

```bash
docker stop entertainment-center-pg
```

---

## Build MAUI APK (Release)

```bash
"C:/Program Files/dotnet/dotnet.exe" publish C:/Work/MothersProject/EntertainmentCenter.MAUI/EntertainmentCenter.MAUI/EntertainmentCenter.csproj -c Release -f net9.0-android -o C:/Work/MothersProject/publish_output
```

## Install APK on phone

```bash
"C:/Users/Valeriy/AppData/Local/Android/Sdk/platform-tools/adb.exe" install -r "C:/Work/MothersProject/publish_output/com.companyname.entertainmentcenter-Signed.apk"
```

---

## Run all tests

```bash
"C:/Program Files/dotnet/dotnet.exe" test C:/Work/MothersProject/EntertainmentCenter.sln
```

---

## Quick deploy checklist

1. `docker start entertainment-center-pg`
2. `cmd.exe //c "taskkill /F /IM EntertainmentCenter.API.exe"` (kill old API)
3. `"/c/Program Files/dotnet/dotnet.exe" build EntertainmentCenter.API/EntertainmentCenter.API.csproj`
4. `"/c/Program Files/dotnet/dotnet.exe" build EntertainmentCenter.MAUI/EntertainmentCenter.MAUI/EntertainmentCenter.csproj -f net9.0-android`
5. `"/c/Program Files/dotnet/dotnet.exe" test EntertainmentCenter.sln`
6. `"/c/Program Files/dotnet/dotnet.exe" run --project EntertainmentCenter.API/EntertainmentCenter.API.csproj --urls "http://0.0.0.0:5000" &`
7. `"/c/Program Files/dotnet/dotnet.exe" publish EntertainmentCenter.MAUI/EntertainmentCenter.MAUI/EntertainmentCenter.csproj -c Release -f net9.0-android -o publish_output`
8. `"/c/Users/Valeriy/AppData/Local/Android/Sdk/platform-tools/adb.exe" install -r publish_output/com.companyname.entertainmentcenter-Signed.apk`
9. Tell user: **API is available at `http://KorsysBook.local:5000` (mDNS) or `http://<IP>:5000`**

---

## Подключение клиентов (mDNS и статический IP)

При развертывании приложения у клиента часто меняется IP-адрес сервера из-за DHCP. Чтобы не перенастраивать телефоны каждый раз, используйте один из двух подходов:

### Вариант А: Имя хоста mDNS (Рекомендуемый)
Если компьютер с сервером называется `KorsysBook`, он доступен в локальной сети по адресу `http://KorsysBook.local:5000`.
1. Узнать имя компьютера на сервере:
   ```powershell
   hostname
   ```
2. В мобильном приложении на телефонах указать URL: `http://ИМЯ_ПК.local:5000` (например, `http://KorsysBook.local:5000`).
3. Убедитесь, что служба "Определение сетевого окружения" (Network Discovery) и mDNS включены в Windows 10/11.

### Вариант Б: Статический IP-адрес
Если mDNS не поддерживается сетевым оборудованием клиента, настройте статический IP на ПК с сервером:
1. Откройте "Панель управления" -> "Центр управления сетями и общим доступом" -> "Изменение параметров адаптера".
2. Кликните правой кнопкой мыши по сетевому адаптеру (Ethernet или Wi-Fi), подключенному к локальной сети, и выберите "Свойства".
3. Выберите "IP версии 4 (TCP/IPv4)" и нажмите "Свойства".
4. Установите переключатель в положение "Использовать следующий IP-адрес" и пропишите постоянные IP-адрес, маску подсети и основной шлюз (соответствующие настройкам роутера клиента, например, `192.168.100.200`).
5. В мобильном приложении на телефонах укажите этот статический IP-адрес: `http://192.168.100.200:5000`.
