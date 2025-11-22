# StudentSolution

A simple, containerized **student management** sample built with:
- **ASP.NET Core Web API** (`StudentApi`) + **Entity Framework Core** (SQL Server)
- **ASP.NET Core MVC** front end (`StudentMvc`) that calls the API via `HttpClient`
- **SQL Server** (local container) or **Azure SQL Database**
- **Docker & Docker Compose** for local development
- **Azure Container Registry (ACR)** + **Azure Container Instances (ACI)** for deployment
- **Azure Key Vault** for secrets (the API reads its SQL connection string from Key Vault in Azure)
- **CI/CD with Azure DevOps** (YAML pipelines)

---

## Repository structure

```
StudentSolution.sln
StudentApi/
  Controllers/
  Data/
  Models/
  Dockerfile
  appsettings.json
  appsettings.Development.json
StudentMvc/
  Controllers/
  Models/
  Views/
  Dockerfile
  appsettings.json
  appsettings.Development.json
docker-compose.yml
```

---

## Local development (Visual Studio)

### 1) Run API + MVC from VS
- Set **Multiple startup projects** (StudentApi + StudentMvc).
- API will expose `GET /api/students`, `GET /api/students/{id}`, `POST`, `PUT`, `DELETE`.
- MVC shows a table and supports **Create / Update / Delete / Search (by name)**.


```

> For local Docker SQL, `Server=localhost,1433` is correct (Visual Studio on Windows).

### 3) EF Core migrations
```powershell
# from the StudentApi project folder
dotnet tool restore
dotnet ef migrations add Initial
dotnet ef database update
```
(You already have migrations in `StudentApi/Data/Migrations`.)

---

## Local development (Docker Compose)

### Prerequisites
- Docker Desktop installed.

### Compose up
```bash
docker-compose up --build -d
```
This starts three containers:
- `sqlserver` (port 1433)
- `studentapi` (port 8081 → container 8080)
- `studentmvc` (port 8082 → container 8080)

### Test
- API: <http://localhost:8081/api/students>
- MVC: <http://localhost:8082/>

### Persist SQL data
The compose file uses a **named volume** (e.g., `sql_data`) so your data survives container restarts.

---

## Configuration

### MVC → API base URL
`StudentMvc` uses a **named `HttpClient`** configured with a base URL from config:

- `StudentMvc/appsettings.Development.json` (local VS):
```json
{ "StudentApi": { "BaseUrl": "http://localhost:5242/" } }
```
- `StudentMvc/appsettings.json` (Compose):
```json
{ "StudentApi": { "BaseUrl": "http://studentapi:8080/" } }
```

### API → SQL connection string
- Local VS: in `appsettings.Development.json` (above)
- Docker Compose: in `appsettings.json` with `Server=sqlserver;...` (container hostname)

### Environment variables (containers)
- **Both API & MVC (ACI/Compose):**
  - `ASPNETCORE_URLS = http://+:8080`  → Kestrel binds to 8080
- **API (ACI):**
  - `KeyVaultUri = https://<your-vault>.vault.azure.net/`

---

## Azure deployment (manual)

### 1) Build & push images to ACR
You can use **Visual Studio Publish** to push:
- `studentapi:<tag>`
- `studentmvc:<tag>`

### 2) Create ACI for API
- Image: `youracr.azurecr.io/studentapi:<tag>`
- Ports: TCP **8080**
- DNS label: e.g., `studentapi-xyz`
- **Identity**: System-assigned **On**
- **Env vars**:
  - `ASPNETCORE_URLS = http://+:8080`
  - `KeyVaultUri = https://<your-vault>.vault.azure.net/`

### 3) Key Vault
- Secret name: **`ConnectionStrings--DefaultConnection`**
- Value: **ADO.NET connection string** for **Azure SQL** (single line), for example:
```
Server=tcp:<server>.database.windows.net,1433;Initial Catalog=StudentDb;User ID=<user>;Password=<password>;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```
- Access: grant the **API ACI managed identity** **Secrets: Get, List** (Access Policy or RBAC).

### 4) SQL firewall
- Azure SQL Server → **Networking** → **Allow Azure services** = **Yes** (or add ACI outbound IP).

### 5) Create ACI for MVC
- Image: `youracr.azurecr.io/studentmvc:<tag>`
- Ports: TCP **8080**
- DNS label: e.g., `studentmvc-xyz`
- **Env vars**:
  - `ASPNETCORE_URLS = http://+:8080`
  - `StudentApi__BaseUrl = http://<api-dns>.azurecontainer.io:8080/`

### Test in Azure
- API: `http://<api-dns>.azurecontainer.io:8080/api/students`
- MVC: `http://<mvc-dns>.azurecontainer.io:8080/`

---

## Azure DevOps CI/CD (YAML)

A sample pipeline that:
- Builds & pushes both images to ACR
- Creates/updates ACI (API & MVC)
- Passes `KeyVaultUri` to API and points MVC to API’s FQDN

> See `azure-pipelines.yml` in this repo. Update variables:
> - `resourceGroup`, `location`
> - `acrLoginServer` (e.g., `youracr.azurecr.io`)
> - `apiDns`, `mvcDns`
> - `keyVaultUri`

### Service connections
- **Azure Resource Manager** (Service Principal – automatic), name e.g. `sc-azure-sub`
- **Docker/ACR** service connection (created by the Docker task wizard), or use `az acr login`

### First run note
After the pipeline **creates** the API ACI with a **managed identity**, add that identity to **Key Vault** (Secrets: Get, List), then restart the API ACI.

---

## API endpoints

- `GET    /api/students?searchString=<name>`
- `GET    /api/students/{id}`
- `POST   /api/students` (JSON body)
- `PUT    /api/students/{id}` (JSON body)
- `DELETE /api/students/{id}`

### Example student JSON
```json
{
  "id": 1,
  "name": "Alice Johnson",
  "email": "alice@example.com",
  "phone": "0700000000",
  "dateOfBearth": "1984-05-07"
}
```

---

## MVC pages

In `Views/Home`:
- `Index.cshtml` — list + search + Update/Delete buttons
- `Create.cshtml` — add new student
- `Edit.cshtml` — update student
- `Delete.cshtml` — confirm delete

MVC calls the API using a **named `HttpClient`**:

```csharp
builder.Services.AddHttpClient("StudentApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["StudentApi:BaseUrl"]!);
});
```

---

## Troubleshooting

- **HTTP 500 (MVC)**: confirm `StudentApi:BaseUrl` points to a reachable API URL.
- **Connection refused in ACI**: ensure `ASPNETCORE_URLS=http://+:8080` is set and port 8080 is exposed.
- **SQL login error**: check SQL firewall (add client IP or allow Azure services).
- **Key Vault 403**: grant **Get/List** on secrets to the API ACI **managed identity**.
- **EF identity jump (Id column)**:
  ```sql
  DECLARE @maxId INT = (SELECT ISNULL(MAX(Id),0) FROM dbo.Students);
  DBCC CHECKIDENT ('dbo.Students', RESEED, @maxId);
  ```

---

## License

MIT (sample educational project).
