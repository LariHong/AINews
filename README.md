# AI Daily

AI Daily 是一個以 AI 新聞聚合、摘要與深度分析為核心的全端專案。前端使用 Vue 3、Vite、Pinia 與 TypeScript，後端使用 ASP.NET Core 8 Web API，並提供 RSS 抓取、文章瀏覽、收藏、偏好設定、AI 摘要與 AI 深度報告等 MVP 功能。

> 目前專案偏向本機開發與 MVP 驗證狀態。文章、收藏、摘要快取與使用者偏好預設多使用 in-memory repository；可透過設定切換文章資料來源到 PostgreSQL。

## 功能特色

- AI 新聞 Dashboard：顯示今日統計、文章列表、來源與主題篩選。
- 文章搜尋與篩選：支援關鍵字、標籤、來源、日期、分頁游標與單篇文章查詢。
- RSS feed crawl：可從 OpenAI、Google DeepMind、Microsoft AI Blog、Google AI Blog、Hugging Face、InfoQ 等來源抓取 AI 相關文章。
- AI 快速摘要：提供文章 preview 摘要查詢與產生流程。
- AI 深度報告：提供 `/report/:id` 報告頁，後端支援 SSE 串流產生深度報告。
- 收藏功能：可加入、移除與檢視收藏文章。
- 隱藏文章偏好：可隱藏文章、還原文章並查看隱藏清單。
- 主題切換：前端支援亮色/暗色主題。
- 本機 API envelope：後端 API 使用一致的成功/錯誤回應格式。

## 技術架構

```text
AINews/
├─ backend/
│  ├─ src/AiDaily.API             # ASP.NET Core Web API 入口與 Controllers
│  ├─ src/AiDaily.Application     # Application services、DTO、use cases
│  ├─ src/AiDaily.Domain          # Domain entities
│  ├─ src/AiDaily.Infrastructure  # Repositories、RSS crawler、AI provider、cache
│  └─ tests/AiDaily.UnitTests     # 後端測試
├─ frontend/
│  ├─ src/views                   # Dashboard、Report、Bookmarks、Settings
│  ├─ src/components              # Article、AI、Bookmark、Common components
│  ├─ src/stores                  # Pinia stores
│  └─ src/services                # API client
├─ docs/                          # 專案文件
├─ AI-Daily-Spec.md               # 專案規格草稿
└─ docker-compose.yml             # PostgreSQL 與 Redis 本機服務
```

## 需求環境

- Node.js 18+ 或 20+
- npm
- .NET SDK 8.0+
- Docker Desktop 或相容的 Docker runtime

## 快速開始

### 1. 啟動資料服務

```powershell
docker compose up -d
```

這會啟動：

- PostgreSQL：`localhost:5432`
- Redis：`localhost:6379`

### 2. 設定後端

複製本機設定範例：

```powershell
Copy-Item backend\src\AiDaily.API\appsettings.Local.example.json backend\src\AiDaily.API\appsettings.Local.json
```

預設設定使用 in-memory 文章 repository：

```json
{
  "Persistence": {
    "ArticleRepository": "InMemory"
  }
}
```

若要改用 PostgreSQL 儲存文章，可將 `ArticleRepository` 改成 `Postgres`、`PostgreSQL`、`Db` 或 `Database`，並確認 `ConnectionStrings:AiDaily` 指向本機資料庫。

AI 深度報告預設可使用 stub provider。若要使用 Gemini，請在 `appsettings.Local.json` 設定：

```json
{
  "AiProvider": {
    "Mode": "Gemini",
    "ApiKey": "YOUR_GEMINI_API_KEY",
    "Model": "gemini-2.5-flash-lite"
  }
}
```

也可以用環境變數 `GEMINI_API_KEY` 提供 API key。

### 3. 啟動後端 API

```powershell
dotnet run --project backend\src\AiDaily.API\AiDaily.API.csproj
```

開發設定預期前端 proxy 指向：

```text
http://localhost:12718
```

如果後端實際啟動在其他 port，請同步調整前端 `.env` 的 `VITE_API_PROXY_TARGET`。

### 4. 設定並啟動前端

```powershell
cd frontend
npm install
Copy-Item .env.example .env
npm run dev
```

預設前端開發伺服器：

```text
http://localhost:5176
```

## 常用指令

### 前端

```powershell
cd frontend
npm run dev
npm run build
npm run test
```

Windows PowerShell 若因執行原則擋下 `npm.ps1`，可改用 `npm.cmd run dev`、`npm.cmd run build` 與 `npm.cmd run test`。

### 後端

```powershell
dotnet build backend\AiDaily.sln
dotnet test backend\tests\AiDaily.UnitTests\AiDaily.UnitTests.csproj
dotnet run --project backend\src\AiDaily.API\AiDaily.API.csproj
```

### Docker

```powershell
docker compose up -d
docker compose down
```

## 主要 API

| Method | Path | 說明 |
| --- | --- | --- |
| `GET` | `/api/v1/articles` | 查詢文章列表，支援 cursor、limit、keyword、tags、source、date |
| `GET` | `/api/v1/articles/{id}` | 查詢單篇文章 |
| `GET` | `/api/v1/stats/today` | 查詢今日 Dashboard 統計 |
| `POST` | `/api/v1/feed-crawl/run?scope=today` | 執行 RSS crawl |
| `GET` | `/api/v1/articles/{articleId}/ai-summary` | 查詢文章 AI 快速摘要 |
| `POST` | `/api/v1/articles/{articleId}/ai-summary?force=false` | 產生文章 AI 快速摘要 |
| `GET` | `/api/v1/articles/{articleId}/ai-report` | 查詢 AI 深度報告 |
| `POST` | `/api/v1/articles/{articleId}/ai-summary/generate?force=false` | 以 SSE 產生 AI 深度報告 |
| `GET` | `/api/v1/bookmarks` | 查詢收藏文章 |
| `POST` | `/api/v1/articles/{id}/bookmark` | 加入收藏 |
| `DELETE` | `/api/v1/articles/{id}/bookmark` | 移除收藏 |
| `GET` | `/api/v1/user-preferences/hidden-articles` | 查詢隱藏文章 |
| `POST` | `/api/v1/user-preferences/hidden-articles/{id}` | 隱藏文章 |
| `DELETE` | `/api/v1/user-preferences/hidden-articles/{id}` | 還原隱藏文章 |

收藏與隱藏文章 mutation 需要暫時使用者識別 header：

```text
X-AI-Daily-Local-User: local-dev-user
```

## 開發狀態與注意事項

- 此 repo 目前是 MVP 階段，部分資料流仍是 in-memory，重啟 API 後可能會消失。
- `docker-compose.yml` 已提供 PostgreSQL 與 Redis，但目前 Redis 尚不是所有快取流程的必要執行依賴。
- AI 快速摘要使用 stub generator；AI 深度報告可依設定使用 stub 或 Gemini。
- 目前沒有正式驗證、授權與 production deployment 設定；請勿直接視為 production-ready。

## 驗證

建議在提交前至少執行：

```powershell
dotnet build backend\AiDaily.sln
dotnet test backend\tests\AiDaily.UnitTests\AiDaily.UnitTests.csproj
cd frontend
npm run build
npm run test
```
