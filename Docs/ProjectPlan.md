# Entertainment Center — Project Plan & Progress

> **Created:** 2026-06-06 | **Last Updated:** 2026-06-10 (late evening)
> **Status:** Phases 1–10 complete (API + MAUI with full UX restructuring). App installed on phone, 32/32 tests green.
> **Next:** User testing of new UI flow, then Phase 10 admin commands (Windows Service + Firewall).

---

##  Quick Status Summary

| Phase | Status | Tests |
|-------|--------|-------|
| Phase 1 — API Setup | ✅ Done | — |
| Phase 2 — API Services | ✅ Done | 12/12 passed |
| Phase 3 — API Controllers + Integration Tests | ✅ Done | 20/20 passed |
| Phase 4 — MAUI Setup | ✅ Done | — |
| Phase 5 — MAUI API Services | ✅ Done | — |
| Phase 6 — MAUI ViewModels | ✅ Done | — |
| Phase 7 — MAUI Views (XAML) | ✅ Done | — |
| Phase 8 — Navigation & App Shell | ✅ Done | — |
| Phase 9 — Final Checks | ✅ Done | — |
| Phase 10 — Deploy as Windows Service | ⚠ Partial | 1/4 tasks |
| **Phase 11 — UI Restructuring** | ✅ Done | — |

**API: 0 errors. MAUI (Release publish): 0 errors. 32/32 tests green.**

---

##  What's Built (complete)

### Infrastructure
- PostgreSQL 17 in Docker container `entertainment-center-pg` on `localhost:5432`
- ASP.NET Core 9 Web API with 14 endpoints
- .NET MAUI 9 Android app — installed and running on Samsung phone
- 32 tests (12 unit + 20 integration), all green

### API Project — 14 endpoints across 5 controllers
```
Controllers/
├── ZonesController.cs       — GET(all), GET(id), POST, PUT, DELETE zones; POST/DELETE tariffs
├── PromotionsController.cs  — GET(all), POST, PUT, DELETE promotions
├── ClientsController.cs     — GET search, GET(id), GET card/{code}, POST
├── SessionsController.cs    — GET active, GET(id), GET check/{code}, GET history, POST start, POST end
└── AdminController.cs       — POST verify-pin, POST change-pin, GET dashboard

Services/
├── ZoneService.cs           — GetAllWithTariffsAsync(includeInactive), GetByIdAsync, Save, Delete
├── TariffService.cs         — SaveTariffAsync (fixed EF tracking bug), DeleteTariffAsync
├── PromotionService.cs      — GetActiveAsync, GetAllAsync, Save, Delete
├── ClientService.cs         — SearchAsync, GetByIdAsync, GetByCardCodeAsync, AddAsync
├── SessionService.cs        — GetAllActive, GetById, GetHistory, StartSession, EndSession
│                             └── DashboardMetrics (VisitsToday, ActiveNow, RevenueToday)
└── AdminService.cs          — VerifyPin, ChangePin
```

### MAUI Project — full app with Minion design system
```
Converters/
└── BooleanInvertConverter.cs, StringNotEmptyConverter.cs

Services/
├── ApiConstants.cs          — Default base URL
├── ApiService.cs            — HttpClient wrapper, reads URL from Preferences
├── ZoneApiService.cs        — GetAllAsync, SaveZone, DeleteZone, SaveTariff, DeleteTariff
├── PromotionApiService.cs   — GetActive, GetAll, Save, Delete
├── ClientApiService.cs      — Search, GetById, GetByCardCode, Add
├── SessionApiService.cs     — GetAllActive, GetById, CheckEntry, StartSession, GetHistory, EndSession
└── AdminApiService.cs       — VerifyPin, ChangePin, GetDashboard

ViewModels/ (16 total — 8 new + 8 existing, all updated)
├── LoginViewModel.cs           — 3 role cards navigation
├── AdminPinViewModel.cs        — 4-digit PIN pad, auto-submit, error animation
├── AdminDashboardViewModel.cs  — metrics + menu + logout
├── ZonesListViewModel.cs       — zone list with toggles + edit/delete
├── PromotionsListViewModel.cs  — promo list with toggles + edit/delete
├── ClientHistoryViewModel.cs   — search + date filters + sessions
├── ClientDetailViewModel.cs    — session info + countdown timer + end session
├── ServerConnectionViewModel.cs — IP config + connection status
├── ReceptionViewModel.cs       — search + active sessions + settings nav + client detail nav
├── AddClientViewModel.cs       — client form + price calc (phone now required)
├── EntryCheckViewModel.cs      — card check + settings nav
├── AdminLoginViewModel.cs      — PIN verification (kept for backward compat)
├── SettingsViewModel.cs        — language picker only
├── ZoneEditViewModel.cs        — zone edit + active toggle + tariff list
├── PromotionEditViewModel.cs   — promo edit + type/days/active toggle
└── ConnectionSettingsViewModel.cs — (kept for backward compat)

Views/ (18 pages — 8 new + 8 existing (rewritten) + 2 legacy)
├── LoginPage              ← NEW: app entry, 3 role cards
├── AdminPinPage           ← NEW: PIN pad with dots + 3×4 grid
├── AdminDashboardPage     ← NEW: metrics + menu
├── ZonesListPage          ← NEW: zone list with toggles
├── PromotionsListPage     ← NEW: promo list with toggles
├── ClientHistoryPage      ← NEW: search + date filters + sessions
├── ClientDetailPage       ← NEW: session detail + timer + end
├── ServerConnectionPage   ← NEW: IP + port + status
├── ReceptionPage          ← REWRITTEN: TopBar + tap→ClientDetail
├── EntryCheckPage         ← REWRITTEN: TopBar + status banners
├── AddClientPage          ← REWRITTEN: phone required
├── SettingsPage           ← REWRITTEN: language only
├── ZoneEditPage           ← REWRITTEN: active toggle, bottom save
├── PromotionEditPage      ← REWRITTEN: segmented type, day pills, toggle
├── AdminLoginPage         ← Legacy (kept)
└── ConnectionSettingsPage ← Legacy (kept)

AppShell: LoginPage as root, 15 registered routes (no TabBar)
App.xaml: Converters registered as StaticResource
```

---

##  Phone Instructions

### Build and install on phone:
```bash
# 1. Build + publish (MUST use publish, not just build):
cd C:\Work\MothersProject
dotnet publish EntertainmentCenter.MAUI/EntertainmentCenter.MAUI/EntertainmentCenter.csproj -c Release -f net9.0-android -o C:/Work/MothersProject/publish_output

# 2. Install on phone:
adb install -r C:/Work/MothersProject/publish_output/com.companyname.entertainmentcenter-Signed.apk
```

### Set API IP on phone (if laptop IP changed):
1. Open app → Администратор → enter PIN (default: 1234)
2. Admin Dashboard → Подключение к серверу
3. Enter laptop IP (e.g. http://192.168.88.102:5000) → Подключиться
4. Restart app

### Laptop IP lookup:
```cmd
ipconfig
:: Look for Wi-Fi adapter → IPv4 Address
```

---

##  Phase 10 — Pending Admin Commands

User must run these as Administrator:
```powershell
# Register Windows Service:
sc create EntertainmentCenterApi binPath="C:\EntertainmentCenter\Api\EntertainmentCenter.API.exe" start=auto
sc start EntertainmentCenterApi

# Firewall rule:
netsh advfirewall firewall add rule name="EntertainmentCenter API" dir=in action=allow protocol=TCP localport=5000
```

---

##  Key Bug Fixes (history)
1. **EF Core tracking conflict:** `_context.Update(tariff)` → changed to `FindAsync()` + property assignment
2. **Android cleartext:** Added `android:usesCleartextTraffic="true"` in AndroidManifest.xml
3. **Fast Deployment crash:** `dotnet build` produces broken APK → use `dotnet publish -c Release`
4. **Dynamic IP:** `Preferences.Get("ApiBaseUrl", ApiConstants.BaseUrl)` in ApiService — changed via ServerConnectionPage

---

##  Reference: Docker PostgreSQL

```bash
# Start DB:
docker start entertainment-center-pg

# Fresh container:
docker rm -f entertainment-center-pg
docker run -d --name entertainment-center-pg \
  -e POSTGRES_PASSWORD=postgres123 \
  -e POSTGRES_DB=entertainment_center \
  -p 5432:5432 \
  -v pgdata:/var/lib/postgresql/data \
  postgres:17

# Start API:
dotnet run --project EntertainmentCenter.API --urls "http://0.0.0.0:5000"
```
