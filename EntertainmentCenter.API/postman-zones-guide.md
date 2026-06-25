# Step-by-Step Guide: Testing Zones API with Postman

Below is a step-by-step guide to:

1. Start the API (ensuring it's actually running)
2. Create a Postman workspace
3. Add the full set of API requests (GET, POST, PUT, DELETE) for the Zones resource
4. Verify the responses

---

## 1. Start the API (make sure it’s running)

1. Open a terminal in the `EntertainmentCenter.API` folder:
   ```powershell
   cd C:\Work\MothersProject\EntertainmentCenter.API
   ```
2. Kill any stray instance (just in case) – this removes the lock on the binary:
   ```powershell
   taskkill /F /IM dotnet.exe 2>nul
   ```
3. Re-build (optional, but guarantees the latest code is compiled):
   ```powershell
   dotnet build
   ```
   You should see:
   ```text
   Build succeeded.
   ```
4. Run the API:
   ```powershell
   dotnet run
   ```
   The console will show:
   ```text
   info: Microsoft.Hosting.Lifetime[14]
         Now listening on: http://localhost:5000
   info: Microsoft.Hosting.Lifetime[0]
         Application started. Press Ctrl+C to shut down.
   ```
5. Leave this terminal open – the process stays alive and is the server Postman will call.

> [!TIP]
> If you need to stop the API later, press `Ctrl + C` in this terminal.

---

## 2. Create a Postman Workspace

1. Open Postman (download from [postman.com](https://postman.com) if you don't have it).
2. Click **Create a workspace** → give it a name, e.g. "EntertainmentCenter API" → **Create**.
3. Inside the workspace, click **New** → **Request** to start building the calls.

---

## 3. Add the Requests (all for the Zones resource)

### 3.1 GET – List All Zones

| Field | Value |
| :--- | :--- |
| **Method** | GET |
| **URL** | `http://localhost:5000/api/zones` |
| **Headers** | (none required) |
| **Tests (optional)** | `javascript`<br>`pm.test("Status is 200", () => pm.response.to.have.status(200));`<br>`pm.test("Body is an array", () => pm.expect(pm.response.json()).to.be.an('array'));` |

> [!NOTE]
> **Expected response (when DB is empty):** `[]`

### 3.2 POST – Create a New Zone

| Field | Value |
| :--- | :--- |
| **Method** | POST |
| **URL** | `http://localhost:5000/api/zones` |
| **Headers** | `Content-Type: application/json` |
| **Body (raw JSON)** | `json`<br>`{`<br>`  "name": "PlayStation Arena"`<br>`}` |
| **Tests (optional)** | `javascript`<br>`pm.test("Created (201)", () => pm.response.to.have.status(201));`<br>`pm.test("Has Id", () => pm.expect(pm.response.json().id).to.be.a('number'));` |

> [!NOTE]
> **Expected response (example):**
> ```json
> {
>   "id": 1,
>   "name": "PlayStation Arena",
>   "isActive": true,
>   "tariffs": []
> }
> ```

### 3.3 GET – Retrieve One Zone

| Field | Value |
| :--- | :--- |
| **Method** | GET |
| **URL** | `http://localhost:5000/api/zones/1` *(replace 1 with the actual Id returned by the POST)* |
| **Headers** | (none) |
| **Tests** | `javascript`<br>`pm.test("Status 200", () => pm.response.to.have.status(200));` |

> [!NOTE]
> **Expected response:** Matches the object created earlier.

### 3.4 PUT – Update a Zone

| Field | Value |
| :--- | :--- |
| **Method** | PUT |
| **URL** | `http://localhost:5000/api/zones/1` |
| **Headers** | `Content-Type: application/json` |
| **Body** | `json`<br>`{`<br>`  "id": 1,`<br>`  "name": "PlayStation Arena – Updated",`<br>`  "isActive": true,`<br>`  "tariffs": []`<br>`}` |
| **Tests** | `javascript`<br>`pm.test("Updated – 200 OK", () => pm.response.to.have.status(200));` |

> [!NOTE]
> **Expected response:** Mirrors the updated JSON body.

### 3.5 DELETE – Remove a Zone

| Field | Value |
| :--- | :--- |
| **Method** | DELETE |
| **URL** | `http://localhost:5000/api/zones/1` |
| **Headers** | (none) |
| **Tests** | `javascript`<br>`pm.test("Deleted – 204 No Content", () => pm.response.to.have.status(204));` |

> [!NOTE]
> **Expected response:** Empty body with HTTP 204.

---

## 4. Quick "Collection Runner" Test

1. Select **Save** on each request inside your collection.
2. Hit **Runner** (top-left play icon).
3. Choose your collection → **Run**.
4. You should see all tests pass:
   - ✓ Status is 200 (GET /zones)
   - ✓ Body is an array (GET /zones)
   - ✓ Created (201) (POST /zones)
   - ✓ Has Id (POST /zones)
   - ✓ Status 200 (GET /zones/1)
   - ✓ Updated – 200 OK (PUT /zones/1)
   - ✓ Deleted – 204 No Content (DELETE /zones/1)

---

If any step fails, paste the exact console output or Postman response body here and we'll narrow it down.
