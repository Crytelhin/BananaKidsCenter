-- =========================================================================
-- БЫСТРЫЕ КОМАНДЫ ДЛЯ ОЧИСТКИ ТЕСТОВЫХ ДАННЫХ У КЛИЕНТА (PostgreSQL)
-- =========================================================================
-- Ниже представлены готовые однострочные команды для запуска в PowerShell
-- на компьютере клиента. Они автоматически находят установленный psql.exe.
--
-- ПРИМЕЧАНИЕ: Если пароль от базы отличается от "postgres123", замените его
-- в части PGPASSWORD="ваш_пароль".
-- =========================================================================

---------------------------------------------------------------------------
-- 1. УДАЛЕНИЕ КЛИЕНТА ЦЕЛИКОМ (Вместе со всеми его сессиями)
---------------------------------------------------------------------------

-- А. Удалить по штрихкоду карты (CardCode):
-- [Копировать в PowerShell]
$env:PGPASSWORD="postgres123"; & (Get-Item "C:\Program Files\PostgreSQL\*\bin\psql.exe").FullName -U postgres -d entertainment_center -c "DELETE FROM `"Clients`" WHERE `"CardCode`" = '4840000422107';"

-- Б. Удалить по ID клиента (например, ID = 5):
-- [Копировать в PowerShell]
$env:PGPASSWORD="postgres123"; & (Get-Item "C:\Program Files\PostgreSQL\*\bin\psql.exe").FullName -U postgres -d entertainment_center -c "DELETE FROM `"Clients`" WHERE `"Id`" = 5;"


---------------------------------------------------------------------------
-- 2. УДАЛЕНИЕ ТОЛЬКО СЕССИЙ КЛИЕНТА (Сам клиент остается в базе)
---------------------------------------------------------------------------

-- А. Удалить сессии по штрихкоду карты (CardCode):
-- [Копировать в PowerShell]
$env:PGPASSWORD="postgres123"; & (Get-Item "C:\Program Files\PostgreSQL\*\bin\psql.exe").FullName -U postgres -d entertainment_center -c "DELETE FROM `"Sessions`" WHERE `"ClientId`" IN (SELECT `"Id`" FROM `"Clients`" WHERE `"CardCode`" = '4840000422107');"

-- Б. Удалить сессии по ID клиента (например, ID = 5):
-- [Копировать в PowerShell]
$env:PGPASSWORD="postgres123"; & (Get-Item "C:\Program Files\PostgreSQL\*\bin\psql.exe").FullName -U postgres -d entertainment_center -c "DELETE FROM `"Sessions`" WHERE `"ClientId`" = 5;"


---------------------------------------------------------------------------
-- 3. ПОЛНЫЙ СБРОС ВСЕХ КЛИЕНТОВ И СЕССИЙ (Очистить базу под ноль)
---------------------------------------------------------------------------
-- Очищает таблицы клиентов и сессий, сбрасывает счетчики автоинкремента ID.
-- Зоны, тарифы, акции и настройки НЕ затрагиваются.

-- [Копировать в PowerShell]
$env:PGPASSWORD="postgres123"; & (Get-Item "C:\Program Files\PostgreSQL\*\bin\psql.exe").FullName -U postgres -d entertainment_center -c "TRUNCATE TABLE `"Sessions`" RESTART IDENTITY CASCADE; TRUNCATE TABLE `"Clients`" RESTART IDENTITY CASCADE;"
