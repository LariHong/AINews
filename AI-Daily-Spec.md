# AI Daily — 專案規格文件 (Project Specification)

**文件版本**: v1.0.0
**建立日期**: 2026-05-12
**最後更新**: 2026-05-12
**文件狀態**: Draft
**負責人**: TBD

---

## 目錄

1. [專案概述](#1-專案概述)
2. [技術架構](#2-技術架構)
3. [系統需求](#3-系統需求)
4. [功能規格](#4-功能規格)
5. [API 規格](#5-api-規格)
6. [資料模型](#6-資料模型)
7. [前端規格 (Vue.js)](#7-前端規格-vuejs)
8. [後端規格 (C# Web API)](#8-後端規格-c-web-api)
9. [資料來源整合](#9-資料來源整合)
10. [AI 整合規格](#10-ai-整合規格)
11. [安全性規範](#11-安全性規範)
12. [效能規範](#12-效能規範)
13. [錯誤處理規範](#13-錯誤處理規範)
14. [測試規範](#14-測試規範)
15. [部署規範](#15-部署規範)
16. [程式碼規範](#16-程式碼規範)
17. [Git 工作流程](#17-git-工作流程)
18. [附錄](#18-附錄)

---

## 1. 專案概述

### 1.1 專案背景

AI Daily 是一套 AI 新聞聚合與深度分析平台，每日自動抓取多個主流 AI 媒體與研究機構的最新資訊，透過 AI 進行結構化分析，提供使用者高效率的資訊閱讀體驗。

### 1.2 目標使用者

| 族群 | 需求描述 |
|------|---------|
| AI 工程師 / 開發者 | 快速掌握模型、框架、工具更新 |
| 研究人員 | 追蹤學術論文與研究趨勢 |
| 產品經理 / 決策者 | 了解市場動態、競品動向 |
| AI 愛好者 | 每日掌握 AI 產業新聞 |

### 1.3 核心功能摘要

- 每日自動抓取多來源 AI 新聞（RSS + 爬蟲）
- 關鍵字搜尋與 Tag 分類篩選
- 使用者手動觸發 AI 一鍵深度總結
- 深度報告頁：TL;DR、核心重點、優劣分析、時間軸、影響力評估
- Dark / Light 主題切換
- 文章書籤收藏

### 1.4 非功能性目標

| 指標 | 目標值 |
|------|--------|
| 首頁載入時間 | ≤ 2 秒 (LCP) |
| API 回應時間（一般查詢） | ≤ 300ms (P95) |
| AI 總結生成時間 | ≤ 15 秒 |
| 系統可用性 | ≥ 99.5% |
| 每日資料抓取成功率 | ≥ 95% |

### 1.5 目前實作狀態

本文件描述的是 AI Daily 的目標產品規格；目前 repo 仍處於 MVP 骨架與第一批前端版型落地階段。

| 範圍 | 狀態 | 說明 |
|------|------|------|
| Dashboard UI | 已完成初版 | `dashboard.html` 已轉入 `frontend/src/views/Dashboard.vue` 與 `frontend/src/styles.css` |
| Report UI | 已完成初版 | `report.html` 已轉入 `frontend/src/views/Report.vue`，路由為 `/report/:id` |
| 文章列表 API | 已完成初版 | 目前提供 `GET /api/v1/articles`，支援列表查詢與前端串接 |
| 資料來源 | 暫用 seed data | 目前使用 `InMemoryArticleRepository`，正式 RSS/背景匯入尚未實作 |
| AI 摘要與深度報告 | 尚未實作 | 目前 Report 頁使用文章資料與靜態推導內容，AI API/SSE pipeline 後續切片處理 |
| 書籤 mutation / 登入 | 尚未實作 | 目前只有顯示狀態與規格規劃，尚未接後端寫入流程 |

---

## 2. 技術架構

### 2.1 整體架構圖

```
┌─────────────────────────────────────────────────────────┐
│                      Client Layer                        │
│              Vue.js 3 + Vite + Pinia                     │
└─────────────────────┬───────────────────────────────────┘
                      │ HTTPS / REST + SSE
┌─────────────────────▼───────────────────────────────────┐
│                   API Gateway Layer                      │
│             ASP.NET Core 8 Web API                       │
│          (Authentication / Rate Limiting)                │
└──────┬──────────────┬──────────────────┬────────────────┘
       │              │                  │
┌──────▼─────┐ ┌──────▼──────┐ ┌────────▼────────┐
│  Article   │ │  AI Summary  │ │  Feed Crawler   │
│  Service   │ │   Service    │ │    Service      │
└──────┬─────┘ └──────┬──────┘ └────────┬────────┘
       │              │                  │
┌──────▼──────────────▼──────────────────▼────────┐
│                  Data Layer                      │
│         PostgreSQL  +  Redis Cache               │
└──────────────────────────────────────────────────┘
       │                         │
┌──────▼──────┐         ┌────────▼────────┐
│ External AI │         │  RSS / Scraper  │
│  API (LLM)  │         │    Sources      │
└─────────────┘         └─────────────────┘
```

### 2.2 技術選型

#### 前端

| 項目 | 選用技術 | 版本 | 說明 |
|------|---------|------|------|
| 框架 | Vue.js | 3.x | Composition API |
| 建置工具 | Vite | 5.x | 快速 HMR |
| 狀態管理 | Pinia | 2.x | 取代 Vuex |
| 路由 | Vue Router | 4.x | |
| HTTP 客戶端 | Fetch API / Axios | — / 1.x | 目前以前端 `apiClient.ts` fetch wrapper 為主；若之後需要攔截器再切 Axios |
| UI 元件庫 | Custom Vue + CSS | — | 目前依 `dashboard.html` / `report.html` 範本落成自訂元件與樣式 |
| 樣式 | CSS Design Tokens | — | 目前集中於 `src/styles.css`，支援 Dark/Light theme |
| 型別檢查 | TypeScript | 5.x | strict mode |
| 測試 | Vitest + Vue Test Utils | — | |
| Linter | ESLint + Prettier | — | |

#### 後端

| 項目 | 選用技術 | 版本 | 說明 |
|------|---------|------|------|
| 框架 | ASP.NET Core | 8.0 LTS | Web API |
| ORM | Entity Framework Core | 8.x | Code First |
| 資料庫 | PostgreSQL | 16.x | 主要資料庫 |
| 快取 | Redis | 7.x | 結果快取、Session |
| 排程 | Hangfire | 1.8.x | 背景爬蟲排程 |
| AI 整合 | Anthropic Claude API | claude-3-5-sonnet | LLM 深度總結 |
| RSS 解析 | System.ServiceModel.Syndication | — | 內建 .NET |
| 爬蟲 | HtmlAgilityPack | 1.11.x | HTML 解析 |
| 身份驗證 | JWT Bearer | — | |
| API 文件 | Swagger / Scalar | — | OpenAPI 3.0 |
| 日誌 | Serilog | 3.x | 結構化日誌 |
| 測試 | xUnit + Moq + FluentAssertions | — | |

---

## 3. 系統需求

### 3.1 功能性需求 (Functional Requirements)

#### FR-001 文章列表

| 需求編號 | 描述 | 優先級 |
|---------|------|--------|
| FR-001-01 | 系統每日定時（00:00、06:00、12:00、18:00）自動抓取最新文章 | P0 |
| FR-001-02 | 文章列表依發布時間降序排列 | P0 |
| FR-001-03 | 支援以關鍵字搜尋文章標題與摘要 | P0 |
| FR-001-04 | 支援以 Tag 篩選文章分類（模型、產品、研究、安全） | P0 |
| FR-001-05 | 支援以來源媒體篩選文章 | P1 |
| FR-001-06 | 文章列表支援無限捲動（Infinite Scroll） | P1 |
| FR-001-07 | 顯示今日文章總數及已 AI 總結數量統計 | P1 |

#### FR-002 AI 深度總結

| 需求編號 | 描述 | 優先級 |
|---------|------|--------|
| FR-002-01 | 使用者可手動點擊「AI 一鍵總結」觸發生成，不自動執行 | P0 |
| FR-002-02 | AI 總結結果包含：TL;DR、核心重點（至少 3 點）、優勢與爭議、背景時間軸 | P0 |
| FR-002-03 | 右側 Panel 顯示快速預覽總結（核心亮點、影響範圍、爭議點） | P0 |
| FR-002-04 | 點擊「查看完整報告」進入深度報告頁 | P0 |
| FR-002-05 | 深度報告頁包含影響力評估雷達圖（技術突破性、商業影響、開發者相關性、爭議程度） | P1 |
| FR-002-06 | AI 總結結果永久快取，同一篇文章不重複呼叫 AI API | P0 |
| FR-002-07 | 支援「重新生成」功能，強制覆蓋舊快取 | P2 |
| FR-002-08 | AI 總結生成期間顯示 Streaming 效果（逐字輸出） | P1 |

#### FR-003 書籤與收藏

| 需求編號 | 描述 | 優先級 |
|---------|------|--------|
| FR-003-01 | 使用者可將文章加入書籤 | P1 |
| FR-003-02 | 側邊欄顯示今日書籤清單 | P1 |
| FR-003-03 | 書籤資料儲存於後端（需登入） | P2 |

#### FR-004 主題切換

| 需求編號 | 描述 | 優先級 |
|---------|------|--------|
| FR-004-01 | 支援 Dark / Light 主題切換 | P0 |
| FR-004-02 | 使用者偏好儲存於 localStorage | P1 |
| FR-004-03 | 預設依據系統主題（prefers-color-scheme） | P1 |

#### FR-005 深度報告匯出

| 需求編號 | 描述 | 優先級 |
|---------|------|--------|
| FR-005-01 | 深度報告可匯出為 PDF | P2 |
| FR-005-02 | 深度報告可複製分享連結 | P2 |

### 3.2 非功能性需求 (Non-Functional Requirements)

| 需求編號 | 類別 | 描述 |
|---------|------|------|
| NFR-001 | 效能 | 文章列表 API 回應 ≤ 300ms (P95)，Redis 快取命中率 ≥ 80% |
| NFR-002 | 效能 | AI 總結 API 支援 SSE Streaming，首字元回應 ≤ 3 秒 |
| NFR-003 | 可用性 | 系統月可用性 ≥ 99.5% |
| NFR-004 | 安全性 | 所有 API 需 JWT 驗證（公開查詢除外）；AI API Key 不得暴露於前端 |
| NFR-005 | 可擴展性 | 背景爬蟲服務需可水平擴展，透過 Redis 避免重複抓取 |
| NFR-006 | 可維護性 | 程式碼測試覆蓋率 ≥ 70%（Unit Test） |
| NFR-007 | 相容性 | 支援 Chrome 120+、Firefox 120+、Safari 17+、Edge 120+ |
| NFR-008 | 無障礙 | 符合 WCAG 2.1 AA 等級基本要求 |

---

## 4. 功能規格

### 4.1 頁面架構

```
/                          → Dashboard（文章列表 + 側邊欄）
/report/:id                → 深度報告頁
/bookmarks                 → 書籤管理頁（需登入）
/settings                  → 使用者設定
```

### 4.2 UI 元件清單

| 元件名稱 | 路徑 | 說明 |
|---------|------|------|
| `Dashboard` | `views/Dashboard.vue` | Dashboard shell，整合頂部導覽列、搜尋工具列、文章列表與右側資訊欄 |
| `Report` | `views/Report.vue` | 深度報告頁，目前承接 `report.html` 範本版型 |
| `ArticleCard` | `components/article/ArticleCard.vue` | 文章卡片 |
| `ArticleFeed` | `components/article/ArticleFeed.vue` | 文章列表（含無限捲動） |
| `TagFilter` | `components/common/TagFilter.vue` | Tag 篩選元件 |
| `AiSummaryPanel` | `components/ai/AiSummaryPanel.vue` | 後續抽出的右側 AI 快速預覽元件；目前邏輯內嵌於 `Dashboard.vue` |
| `ImpactMeter` | `components/ai/ImpactMeter.vue` | 後續抽出的影響力評估元件；目前版型內嵌於 `Report.vue` |
| `ThemeToggle` | `components/common/ThemeToggle.vue` | 後續可抽出的主題切換按鈕；目前內嵌於頁面 |
| `BookmarkList` | `components/bookmark/BookmarkList.vue` | 後續可抽出的今日書籤清單；目前內嵌於 Dashboard 右側欄 |
| `StatsCard` | `components/dashboard/StatsCard.vue` | 後續可抽出的統計數字卡片；目前內嵌於 Dashboard |
| `LoadingSkeleton` | `components/common/LoadingSkeleton.vue` | 後續可抽出的載入中骨架屏 |

### 4.3 狀態管理 (Pinia Stores)

```
stores/
├── articleStore.ts        # 文章列表、搜尋、篩選狀態
├── aiSummaryStore.ts      # AI 總結快取、生成狀態
├── bookmarkStore.ts       # 書籤收藏狀態
├── themeStore.ts          # Dark/Light 主題狀態
└── authStore.ts           # 使用者身份狀態
```

---

## 5. API 規格

> 實作狀態：本章多數內容是目標 API 規格。MVP S1 目前已實作 `GET /api/v1/articles`；JWT、SSE、AI summary/report、bookmarks、PostgreSQL/Redis-backed flows 尚待後續切片。

### 5.1 基礎規則

- **Base URL**: `https://api.ai-daily.example.com/api/v1`
- **Protocol**: HTTPS only
- **Format**: JSON (Content-Type: application/json)
- **Authentication**: Bearer Token (JWT)
- **Versioning**: URL Path Versioning (`/api/v1/`, `/api/v2/`)
- **Pagination**: Cursor-based pagination
- **Date Format**: ISO 8601 (`2026-05-12T08:00:00Z`)

### 5.2 通用回應格式

#### 成功回應

```json
{
  "success": true,
  "data": { ... },
  "meta": {
    "timestamp": "2026-05-12T08:00:00Z",
    "requestId": "req_01JXXXXXXXX"
  }
}
```

#### 錯誤回應

```json
{
  "success": false,
  "error": {
    "code": "ARTICLE_NOT_FOUND",
    "message": "找不到指定的文章",
    "details": null
  },
  "meta": {
    "timestamp": "2026-05-12T08:00:00Z",
    "requestId": "req_01JXXXXXXXX"
  }
}
```

#### 分頁回應

```json
{
  "success": true,
  "data": [ ... ],
  "pagination": {
    "cursor": "eyJpZCI6MTAwfQ==",
    "hasMore": true,
    "totalCount": 47
  }
}
```

### 5.3 文章相關 API

#### GET /articles

取得文章列表

**Query Parameters**

| 參數 | 類型 | 必填 | 說明 | 範例 |
|------|------|------|------|------|
| `cursor` | string | 否 | 分頁游標 | `eyJpZCI6MTAwfQ==` |
| `limit` | integer | 否 | 每頁筆數（預設 20，最大 50） | `20` |
| `keyword` | string | 否 | 關鍵字搜尋 | `GPT-5` |
| `tags` | string[] | 否 | Tag 篩選（可多選） | `model,research` |
| `source` | string | 否 | 來源媒體篩選 | `huggingface` |
| `date` | string | 否 | 指定日期 (YYYY-MM-DD)，預設今日 | `2026-05-12` |

**Response 200**

```json
{
  "success": true,
  "data": [
    {
      "id": "art_01JXXXXXXXX",
      "title": "GPT-5 正式發布：多模態推理能力大幅躍升",
      "summary": "OpenAI 今日宣布 GPT-5 正式上線...",
      "sourceUrl": "https://theverge.com/...",
      "sourceName": "The Verge",
      "sourceLogoUrl": "https://...",
      "tags": ["model", "product"],
      "publishedAt": "2026-05-12T06:00:00Z",
      "hasAiSummary": true,
      "isBookmarked": false,
      "readTimeMinutes": 5
    }
  ],
  "pagination": {
    "cursor": "eyJpZCI6OTB9",
    "hasMore": true,
    "totalCount": 47
  }
}
```

---

#### GET /articles/:id

取得單篇文章詳情

**Path Parameters**

| 參數 | 類型 | 說明 |
|------|------|------|
| `id` | string | 文章 ID |

**Response 200**

```json
{
  "success": true,
  "data": {
    "id": "art_01JXXXXXXXX",
    "title": "GPT-5 正式發布：多模態推理能力大幅躍升",
    "summary": "OpenAI 今日宣布 GPT-5 正式上線...",
    "content": "完整文章內文（HTML）...",
    "sourceUrl": "https://theverge.com/...",
    "sourceName": "The Verge",
    "tags": ["model", "product"],
    "publishedAt": "2026-05-12T06:00:00Z",
    "hasAiSummary": true,
    "isBookmarked": false
  }
}
```

---

#### GET /articles/:id/ai-summary

取得文章 AI 快速預覽（右側 Panel 用）

**Response 200**

```json
{
  "success": true,
  "data": {
    "articleId": "art_01JXXXXXXXX",
    "highlights": "多模態推理、程式碼生成提升 40%、支援 128K context",
    "impactScope": "開發者、企業 API 用戶、教育平台",
    "controversy": "定價策略遭批評、部分 benchmark 數據存疑",
    "editorView": "值得追蹤：對 agent 應用架構影響深遠",
    "generatedAt": "2026-05-12T07:30:00Z"
  }
}
```

---

#### POST /articles/:id/ai-summary/generate

觸發 AI 深度總結生成（SSE Streaming）

**Request Headers**

```
Accept: text/event-stream
Authorization: Bearer {token}
```

**Request Body**

```json
{
  "forceRegenerate": false
}
```

**Response: SSE Stream**

```
data: {"type":"start","articleId":"art_01JXXXXXXXX"}

data: {"type":"chunk","field":"tldr","content":"OpenAI 發布 GPT-5，"}

data: {"type":"chunk","field":"tldr","content":"在數學推理..."}

data: {"type":"field_done","field":"tldr"}

data: {"type":"chunk","field":"keyPoints","index":0,"content":"推理能力全面升級"}

data: {"type":"done","summaryId":"sum_01JXXXXXXXX"}
```

---

#### GET /articles/:id/ai-report

取得文章 AI 深度報告（完整結構化資料）

**Response 200**

```json
{
  "success": true,
  "data": {
    "id": "sum_01JXXXXXXXX",
    "articleId": "art_01JXXXXXXXX",
    "tldr": "OpenAI 發布 GPT-5，在數學推理、程式碼生成與視覺理解三大領域全面超越 GPT-4o...",
    "keyPoints": [
      {
        "order": 1,
        "title": "推理能力全面升級",
        "description": "數學推理 Benchmark 達到 92.3%..."
      }
    ],
    "pros": ["推理能力業界領先", "多模態整合更自然"],
    "cons": ["推理模式額外收費", "部分 Benchmark 數據存疑"],
    "timeline": [
      {
        "date": "2023-11",
        "event": "GPT-4 Turbo 發布，首次支援 128K context"
      }
    ],
    "impactScores": {
      "technicalBreakthrough": 9,
      "businessImpact": 8,
      "developerRelevance": 10,
      "controversyLevel": 6
    },
    "relatedTags": ["OpenAI", "LLM", "多模態", "AI Agent"],
    "editorNote": "這篇值得深讀...",
    "editorRating": 5,
    "generatedAt": "2026-05-12T07:30:00Z"
  }
}
```

---

#### POST /articles/:id/bookmark

新增書籤

**Response 200**

```json
{
  "success": true,
  "data": { "bookmarked": true }
}
```

---

#### DELETE /articles/:id/bookmark

移除書籤

**Response 200**

```json
{
  "success": true,
  "data": { "bookmarked": false }
}
```

---

#### GET /stats/today

取得今日統計數字

**Response 200**

```json
{
  "success": true,
  "data": {
    "totalArticles": 47,
    "aiSummarizedCount": 12,
    "tagBreakdown": {
      "model": 34,
      "product": 19,
      "research": 12,
      "safety": 8
    },
    "topSources": [
      { "name": "The Verge", "count": 12 },
      { "name": "Hugging Face Blog", "count": 9 }
    ]
  }
}
```

### 5.4 HTTP 狀態碼規範

| 狀態碼 | 使用時機 |
|--------|---------|
| 200 OK | 查詢、更新成功 |
| 201 Created | 建立資源成功 |
| 204 No Content | 刪除成功 |
| 400 Bad Request | 請求參數錯誤 |
| 401 Unauthorized | 未提供或無效的 Token |
| 403 Forbidden | 無權限存取 |
| 404 Not Found | 資源不存在 |
| 409 Conflict | 資源狀態衝突 |
| 422 Unprocessable Entity | 業務邏輯驗證失敗 |
| 429 Too Many Requests | 超出 Rate Limit |
| 500 Internal Server Error | 伺服器內部錯誤 |
| 503 Service Unavailable | 服務暫時不可用 |

---

## 6. 資料模型

### 6.1 資料庫 Schema (PostgreSQL)

#### Articles 表

```sql
CREATE TABLE articles (
    id              VARCHAR(26)     PRIMARY KEY,        -- ULID
    title           TEXT            NOT NULL,
    summary         TEXT,
    content         TEXT,
    source_url      TEXT            NOT NULL UNIQUE,
    source_name     VARCHAR(100)    NOT NULL,
    source_logo_url TEXT,
    tags            TEXT[]          NOT NULL DEFAULT '{}',
    published_at    TIMESTAMPTZ     NOT NULL,
    fetched_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    has_ai_summary  BOOLEAN         NOT NULL DEFAULT FALSE,
    read_time_min   SMALLINT,
    is_active       BOOLEAN         NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_articles_published_at ON articles (published_at DESC);
CREATE INDEX idx_articles_tags         ON articles USING GIN (tags);
CREATE INDEX idx_articles_source_name  ON articles (source_name);
CREATE INDEX idx_articles_title_search ON articles USING GIN (to_tsvector('english', title || ' ' || COALESCE(summary, '')));
```

#### AiSummaries 表

```sql
CREATE TABLE ai_summaries (
    id              VARCHAR(26)     PRIMARY KEY,        -- ULID
    article_id      VARCHAR(26)     NOT NULL REFERENCES articles(id) ON DELETE CASCADE,
    tldr            TEXT            NOT NULL,
    key_points      JSONB           NOT NULL DEFAULT '[]',
    pros            TEXT[]          NOT NULL DEFAULT '{}',
    cons            TEXT[]          NOT NULL DEFAULT '{}',
    timeline        JSONB           NOT NULL DEFAULT '[]',
    impact_scores   JSONB           NOT NULL,
    related_tags    TEXT[]          NOT NULL DEFAULT '{}',
    editor_note     TEXT,
    editor_rating   SMALLINT        CHECK (editor_rating BETWEEN 1 AND 5),
    -- Quick preview fields
    highlights      TEXT,
    impact_scope    TEXT,
    controversy     TEXT,
    editor_view     TEXT,
    -- Metadata
    model_used      VARCHAR(100)    NOT NULL,
    prompt_version  VARCHAR(20)     NOT NULL,
    generated_at    TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX idx_ai_summaries_article_id ON ai_summaries (article_id);
```

#### FeedSources 表

```sql
CREATE TABLE feed_sources (
    id              SERIAL          PRIMARY KEY,
    name            VARCHAR(100)    NOT NULL,
    display_name    VARCHAR(100)    NOT NULL,
    feed_url        TEXT            NOT NULL UNIQUE,
    source_type     VARCHAR(20)     NOT NULL CHECK (source_type IN ('rss', 'atom', 'scraper')),
    default_tags    TEXT[]          NOT NULL DEFAULT '{}',
    is_active       BOOLEAN         NOT NULL DEFAULT TRUE,
    last_fetched_at TIMESTAMPTZ,
    fetch_interval  INTERVAL        NOT NULL DEFAULT '6 hours',
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);
```

#### Bookmarks 表

```sql
CREATE TABLE bookmarks (
    id          SERIAL          PRIMARY KEY,
    user_id     VARCHAR(26)     NOT NULL,
    article_id  VARCHAR(26)     NOT NULL REFERENCES articles(id) ON DELETE CASCADE,
    created_at  TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    UNIQUE (user_id, article_id)
);

CREATE INDEX idx_bookmarks_user_id ON bookmarks (user_id, created_at DESC);
```

### 6.2 JSONB 欄位結構

#### key_points JSONB

```json
[
  {
    "order": 1,
    "title": "推理能力全面升級",
    "description": "數學推理 Benchmark 達到 92.3%..."
  }
]
```

#### impact_scores JSONB

```json
{
  "technicalBreakthrough": 9,
  "businessImpact": 8,
  "developerRelevance": 10,
  "controversyLevel": 6
}
```

#### timeline JSONB

```json
[
  {
    "date": "2023-11",
    "event": "GPT-4 Turbo 發布，首次支援 128K context"
  }
]
```

---

## 7. 前端規格 (Vue.js)

> 實作狀態：本章目錄樹混合「目前已存在」與「後續計畫」。目前已存在的核心檔案包含 `Dashboard.vue`、`Report.vue`、`ArticleCard.vue`、`ArticleFeed.vue`、`TagFilter.vue`、`articleStore.ts`、`apiClient.ts`、`router/index.ts`、`styles.css`；其他 AI、bookmark、theme、layout 抽出元件屬後續切片。

### 7.1 專案目錄結構

```
frontend/
├── public/
│   └── favicon.ico
├── src/
│   ├── components/
│   │   ├── ai/
│   │   │   ├── AiSummaryPanel.vue    # 後續從 Dashboard 抽出
│   │   │   └── ImpactMeter.vue       # 後續從 Report 抽出
│   │   ├── article/
│   │   │   ├── ArticleCard.vue
│   │   │   └── ArticleFeed.vue
│   │   ├── bookmark/
│   │   │   └── BookmarkList.vue      # 後續從 Dashboard 抽出
│   │   ├── common/
│   │   │   ├── AppButton.vue
│   │   │   ├── AppBadge.vue
│   │   │   ├── LoadingSkeleton.vue
│   │   │   └── TagFilter.vue
│   │   ├── dashboard/
│   │   │   └── StatsCard.vue         # 後續從 Dashboard 抽出
│   │   └── layout/                   # 後續需要共用 layout 時再建立
│   ├── composables/
│   │   ├── useArticles.ts            # 後續可抽出：文章列表邏輯
│   │   ├── useAiSummary.ts           # 後續實作：AI 總結邏輯（含 SSE）
│   │   ├── useBookmarks.ts           # 後續實作
│   │   ├── useInfiniteScroll.ts      # 後續實作
│   │   └── useTheme.ts               # 後續可抽出
│   ├── router/
│   │   └── index.ts
│   ├── services/
│   │   ├── apiClient.ts              # 目前 fetch-based API client
│   │   ├── articleService.ts         # 後續擴充
│   │   ├── aiSummaryService.ts
│   │   └── bookmarkService.ts
│   ├── stores/
│   │   ├── articleStore.ts
│   │   ├── aiSummaryStore.ts
│   │   ├── bookmarkStore.ts
│   │   ├── themeStore.ts
│   │   └── authStore.ts
│   ├── types/
│   │   ├── article.types.ts
│   │   ├── aiSummary.types.ts
│   │   └── api.types.ts
│   ├── utils/
│   │   ├── dateFormatter.ts
│   │   ├── textHelper.ts
│   │   └── constants.ts
│   ├── views/
│   │   ├── Dashboard.vue             # 由 dashboard.html 範本轉入
│   │   ├── Report.vue                # 由 report.html 範本轉入
│   │   ├── BookmarksView.vue
│   │   └── SettingsView.vue
│   ├── styles.css                    # 目前集中式 design tokens 與頁面樣式
│   ├── env.d.ts
│   ├── App.vue
│   └── main.ts
├── tests/
│   ├── unit/
│   └── e2e/
├── .env.example
├── .eslintrc.cjs
├── .prettierrc
├── tsconfig.json
├── vite.config.ts
└── package.json
```

### 7.2 環境變數

```bash
# .env.example
VITE_API_BASE_URL=https://api.ai-daily.example.com/api/v1
VITE_APP_TITLE=AI Daily
VITE_APP_VERSION=1.0.0
```

### 7.3 TypeScript 型別定義

```typescript
// types/article.types.ts

export interface Article {
  id: string
  title: string
  summary: string
  sourceUrl: string
  sourceName: string
  sourceLogoUrl?: string
  tags: ArticleTag[]
  publishedAt: string
  hasAiSummary: boolean
  isBookmarked: boolean
  readTimeMinutes?: number
}

export type ArticleTag = 'model' | 'product' | 'research' | 'safety'

export interface ArticleListParams {
  cursor?: string
  limit?: number
  keyword?: string
  tags?: ArticleTag[]
  source?: string
  date?: string
}

export interface PaginatedArticles {
  data: Article[]
  pagination: {
    cursor: string | null
    hasMore: boolean
    totalCount: number
  }
}
```

```typescript
// types/aiSummary.types.ts

export interface AiQuickSummary {
  articleId: string
  highlights: string
  impactScope: string
  controversy: string
  editorView: string
  generatedAt: string
}

export interface KeyPoint {
  order: number
  title: string
  description: string
}

export interface TimelineEvent {
  date: string
  event: string
}

export interface ImpactScores {
  technicalBreakthrough: number
  businessImpact: number
  developerRelevance: number
  controversyLevel: number
}

export interface AiDeepReport {
  id: string
  articleId: string
  tldr: string
  keyPoints: KeyPoint[]
  pros: string[]
  cons: string[]
  timeline: TimelineEvent[]
  impactScores: ImpactScores
  relatedTags: string[]
  editorNote?: string
  editorRating?: number
  generatedAt: string
}

export type SseEventType = 'start' | 'chunk' | 'field_done' | 'done' | 'error'

export interface SseEvent {
  type: SseEventType
  field?: string
  content?: string
  index?: number
  articleId?: string
  summaryId?: string
  message?: string
}
```

### 7.4 Composable 範例（後續 S3/S4）

> 實作狀態：以下 `useAiSummary.ts` 是 AI summary/report pipeline 的目標範例，目前尚未落地。現在的 `Report.vue` 使用文章資料與靜態推導內容呈現報告版型。

```typescript
// composables/useAiSummary.ts

import { ref, computed } from 'vue'
import { useAiSummaryStore } from '@/stores/aiSummaryStore'
import { aiSummaryService } from '@/services/aiSummaryService'
import type { AiDeepReport, SseEvent } from '@/types/aiSummary.types'

export function useAiSummary(articleId: string) {
  const store = useAiSummaryStore()
  const isGenerating = ref(false)
  const streamingText = ref('')
  const error = ref<string | null>(null)

  const report = computed(() => store.getReport(articleId))
  const hasReport = computed(() => !!report.value)

  async function generateSummary(forceRegenerate = false): Promise<void> {
    if (isGenerating.value) return
    if (hasReport.value && !forceRegenerate) return

    isGenerating.value = true
    streamingText.value = ''
    error.value = null

    try {
      await aiSummaryService.streamGenerate(
        articleId,
        forceRegenerate,
        (event: SseEvent) => {
          if (event.type === 'chunk' && event.content) {
            streamingText.value += event.content
          }
          if (event.type === 'done' && event.summaryId) {
            store.invalidateCache(articleId)
            loadReport()
          }
          if (event.type === 'error') {
            error.value = event.message ?? '生成失敗，請稍後再試'
          }
        }
      )
    } catch (err) {
      error.value = '連線失敗，請稍後再試'
    } finally {
      isGenerating.value = false
    }
  }

  async function loadReport(): Promise<void> {
    const data = await aiSummaryService.getReport(articleId)
    store.setReport(articleId, data)
  }

  return {
    report,
    hasReport,
    isGenerating,
    streamingText,
    error,
    generateSummary,
    loadReport,
  }
}
```

---

## 8. 後端規格 (C# Web API)

### 8.1 專案目錄結構

```
backend/
├── AiDaily.sln
├── src/
│   ├── AiDaily.API/                          # Web API 進入點
│   │   ├── Controllers/
│   │   │   ├── ArticlesController.cs
│   │   │   ├── AiSummaryController.cs
│   │   │   ├── BookmarksController.cs
│   │   │   └── StatsController.cs
│   │   ├── Middleware/
│   │   │   ├── ExceptionHandlingMiddleware.cs
│   │   │   ├── RequestLoggingMiddleware.cs
│   │   │   └── RateLimitingMiddleware.cs
│   │   ├── Extensions/
│   │   │   ├── ServiceCollectionExtensions.cs
│   │   │   └── WebApplicationExtensions.cs
│   │   ├── appsettings.json
│   │   ├── appsettings.Development.json
│   │   └── Program.cs
│   │
│   ├── AiDaily.Application/                  # 應用層（Use Cases）
│   │   ├── Articles/
│   │   │   ├── Queries/
│   │   │   │   ├── GetArticleListQuery.cs
│   │   │   │   ├── GetArticleListQueryHandler.cs
│   │   │   │   └── GetArticleByIdQuery.cs
│   │   │   └── DTOs/
│   │   │       ├── ArticleDto.cs
│   │   │       └── ArticleListParams.cs
│   │   ├── AiSummary/
│   │   │   ├── Commands/
│   │   │   │   ├── GenerateAiSummaryCommand.cs
│   │   │   │   └── GenerateAiSummaryCommandHandler.cs
│   │   │   ├── Queries/
│   │   │   │   ├── GetAiQuickSummaryQuery.cs
│   │   │   │   └── GetAiDeepReportQuery.cs
│   │   │   └── DTOs/
│   │   │       ├── AiQuickSummaryDto.cs
│   │   │       └── AiDeepReportDto.cs
│   │   └── Common/
│   │       ├── Interfaces/
│   │       │   ├── IArticleRepository.cs
│   │       │   ├── IAiSummaryRepository.cs
│   │       │   ├── IAiService.cs
│   │       │   └── ICacheService.cs
│   │       └── Models/
│   │           ├── ApiResponse.cs
│   │           └── PaginatedResult.cs
│   │
│   ├── AiDaily.Domain/                       # 領域層（Entities）
│   │   ├── Entities/
│   │   │   ├── Article.cs
│   │   │   ├── AiSummary.cs
│   │   │   ├── FeedSource.cs
│   │   │   └── Bookmark.cs
│   │   ├── Enums/
│   │   │   ├── ArticleTag.cs
│   │   │   └── FeedSourceType.cs
│   │   └── ValueObjects/
│   │       └── ImpactScores.cs
│   │
│   ├── AiDaily.Infrastructure/               # 基礎建設層
│   │   ├── Persistence/
│   │   │   ├── AiDailyDbContext.cs
│   │   │   ├── Repositories/
│   │   │   │   ├── ArticleRepository.cs
│   │   │   │   └── AiSummaryRepository.cs
│   │   │   └── Configurations/
│   │   │       ├── ArticleConfiguration.cs
│   │   │       └── AiSummaryConfiguration.cs
│   │   ├── AI/
│   │   │   └── ClaudeAiService.cs
│   │   ├── Cache/
│   │   │   └── RedisCacheService.cs
│   │   ├── FeedCrawler/
│   │   │   ├── RssFeedCrawler.cs
│   │   │   ├── HtmlScraper.cs
│   │   │   └── FeedCrawlerJob.cs             # Hangfire Job
│   │   └── Migrations/
│   │
│   └── AiDaily.Shared/                       # 共用工具
│       ├── Extensions/
│       │   ├── StringExtensions.cs
│       │   └── DateTimeExtensions.cs
│       └── Helpers/
│           └── UlidHelper.cs
│
└── tests/
    ├── AiDaily.UnitTests/
    │   ├── Application/
    │   └── Domain/
    └── AiDaily.IntegrationTests/
        └── API/
```

### 8.2 Controller 規範

```csharp
// Controllers/ArticlesController.cs

[ApiController]
[Route("api/v1/articles")]
[Produces("application/json")]
public class ArticlesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ArticlesController> _logger;

    public ArticlesController(IMediator mediator, ILogger<ArticlesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// 取得文章列表
    /// </summary>
    /// <param name="params">查詢參數</param>
    /// <param name="cancellationToken">取消 Token</param>
    /// <returns>文章列表（分頁）</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ArticleDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetArticles(
        [FromQuery] ArticleListParams @params,
        CancellationToken cancellationToken)
    {
        var query = new GetArticleListQuery(@params);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(ApiResponse<PaginatedResult<ArticleDto>>.Success(result));
    }

    /// <summary>
    /// 取得單篇文章詳情
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ArticleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetArticle(
        [FromRoute] string id,
        CancellationToken cancellationToken)
    {
        var query = new GetArticleByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (result is null)
            return NotFound(ApiResponse<object>.Failure("ARTICLE_NOT_FOUND", "找不到指定的文章"));

        return Ok(ApiResponse<ArticleDto>.Success(result));
    }
}
```

### 8.3 SSE Streaming 規範

```csharp
// Controllers/AiSummaryController.cs

[HttpPost("{id}/ai-summary/generate")]
public async Task GenerateAiSummary(
    [FromRoute] string id,
    [FromBody] GenerateAiSummaryRequest request,
    CancellationToken cancellationToken)
{
    Response.Headers.Append("Content-Type", "text/event-stream");
    Response.Headers.Append("Cache-Control", "no-cache");
    Response.Headers.Append("X-Accel-Buffering", "no");

    await foreach (var sseEvent in _aiService.StreamSummaryAsync(id, request.ForceRegenerate, cancellationToken))
    {
        var json = JsonSerializer.Serialize(sseEvent, _jsonOptions);
        await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
}
```

### 8.4 appsettings.json 結構

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=ai_daily;Username=postgres;Password=yourpassword",
    "Redis": "localhost:6379"
  },
  "Jwt": {
    "Issuer": "ai-daily-api",
    "Audience": "ai-daily-client",
    "SecretKey": "YOUR_SECRET_KEY_MIN_32_CHARS",
    "ExpiresInMinutes": 1440
  },
  "AI": {
    "Provider": "Anthropic",
    "ApiKey": "YOUR_ANTHROPIC_API_KEY",
    "Model": "claude-sonnet-4-20250514",
    "MaxTokens": 2048,
    "PromptVersion": "v1.0"
  },
  "FeedCrawler": {
    "CronExpression": "0 0,6,12,18 * * *",
    "TimeoutSeconds": 30,
    "MaxRetries": 3,
    "RetryDelaySeconds": 60
  },
  "RateLimit": {
    "AiSummaryPerUserPerHour": 10,
    "ApiCallsPerMinute": 60
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

---

## 9. 資料來源整合

### 9.1 RSS 來源清單

| 來源 | RSS URL | 類型 | 預設 Tags | 更新頻率 |
|------|---------|------|----------|---------|
| Hugging Face Blog | `https://huggingface.co/blog/feed.xml` | RSS | model, research | 高 |
| MIT Tech Review AI | `https://www.technologyreview.com/topic/artificial-intelligence/feed` | RSS | safety, policy | 中高 |
| arXiv cs.AI | `http://arxiv.org/rss/cs.AI` | RSS | research | 極高 |
| Google AI Blog | `https://blog.google/technology/ai/rss` | RSS | model, product | 中 |
| OpenAI News | `https://openai.com/news/rss` | RSS | model, product | 中 |
| The Verge AI | `https://www.theverge.com/ai-artificial-intelligence/rss/index.xml` | RSS | product, policy | 極高 |
| TLDR AI | `https://tldr.tech/ai/rss` | RSS | 綜合 | 每日 |
| MarkTechPost | `https://www.marktechpost.com/feed` | RSS | research | 極高 |
| DeepMind Blog | `https://deepmind.google/blog/rss.xml` | RSS | research, model | 低中 |
| Anthropic News | `https://www.anthropic.com/news` | Scraper | safety, model | 低 |

### 9.2 爬蟲策略

```
1. 每 6 小時執行一次 Hangfire 排程 Job
2. 檢查 Redis 中 last_fetched_at，避免重複抓取
3. 對每個 RSS 來源使用 SyndicationFeed 解析
4. 以 source_url 作為去重依據（UNIQUE 約束）
5. 新文章寫入 articles 表，has_ai_summary = false
6. arXiv 每日量大（50-100+），以關鍵字過濾：
   - GPT, Claude, Gemini, LLM, Transformer, Agent, RAG, RLHF
7. 逾時設定：30 秒 / 來源
8. 失敗重試：最多 3 次，間隔 60 秒
9. 錯誤記錄至 Serilog，不中斷其他來源的抓取
```

---

## 10. AI 整合規格

### 10.1 Prompt 設計

#### 快速預覽 Prompt (用於右側 Panel)

```
System:
你是一位專業的 AI 產業分析師，負責為技術讀者提供精準的文章快速預覽。
請以 JSON 格式回應，不要包含任何額外說明。

User:
請分析以下文章，並以 JSON 格式輸出：

文章標題：{title}
文章內容：{content}

請輸出以下 JSON 結構：
{
  "highlights": "3個核心亮點，以逗號分隔，每個不超過15字",
  "impactScope": "影響的受眾族群，2-4個，以逗號分隔",
  "controversy": "主要爭議點或缺點，不超過30字；若無則填「目前無明顯爭議」",
  "editorView": "給技術讀者的一句話建議，不超過30字"
}
```

#### 深度報告 Prompt (用於深度報告頁，Streaming)

```
System:
你是一位專業的 AI 產業深度分析師，為技術讀者撰寫結構化報告。
請嚴格按照指定格式輸出，確保分析客觀、有深度，避免過度樂觀。

User:
請為以下文章產生完整的深度分析報告：

文章標題：{title}
來源媒體：{sourceName}
發布時間：{publishedAt}
文章內容：{content}

請以 JSON 格式輸出以下完整結構：
{
  "tldr": "100-150字的總結，涵蓋最重要的事實與意義",
  "keyPoints": [
    {
      "order": 1,
      "title": "重點標題（15字以內）",
      "description": "詳細說明（50-80字）"
    }
    // 3-5個重點
  ],
  "pros": ["優勢1", "優勢2", "優勢3"],  // 3-5個
  "cons": ["爭議1", "爭議2"],            // 2-4個
  "timeline": [
    { "date": "YYYY-MM", "event": "事件描述（30字以內）" }
    // 2-4個背景事件
  ],
  "impactScores": {
    "technicalBreakthrough": 1-10,
    "businessImpact": 1-10,
    "developerRelevance": 1-10,
    "controversyLevel": 1-10
  },
  "relatedTags": ["標籤1", "標籤2"],    // 4-8個
  "editorNote": "給讀者的閱讀建議（50-80字）",
  "editorRating": 1-5
}
```

### 10.2 AI 呼叫成本控制

| 策略 | 說明 |
|------|------|
| 快取永久保存 | AI 總結結果存入 PostgreSQL，同一文章不重複呼叫 |
| 手動觸發 | 不自動生成，由使用者點擊觸發，避免不必要的消耗 |
| Rate Limiting | 每位使用者每小時最多觸發 10 次 AI 總結 |
| Content 截斷 | 傳入 AI 的文章內容最多 4000 tokens，超過則截斷 |
| Prompt 版本控制 | `prompt_version` 欄位記錄版本，方便 A/B 測試 |

---

## 11. 安全性規範

### 11.1 身份驗證

- 使用 JWT Bearer Token，有效期 24 小時
- Refresh Token 機制（有效期 30 天）
- Token 儲存於 `httpOnly` Cookie（非 localStorage）
- 公開 API（文章列表、詳情查詢）無需驗證
- 書籤、AI 生成等寫入操作需驗證

### 11.2 API 安全

| 項目 | 規範 |
|------|------|
| CORS | 僅允許白名單網域 |
| Rate Limiting | AI API: 10 次/使用者/小時；一般 API: 60 次/分鐘 |
| Input Validation | 所有請求參數使用 FluentValidation 驗證 |
| SQL Injection | 全程使用 EF Core Parameterized Query |
| XSS | 文章 Content 輸出需 HTML Encode |
| AI API Key | 僅存於後端環境變數，絕不暴露於前端 |
| HTTPS | 強制 HTTPS，HTTP 自動重導向 |

### 11.3 敏感資料

- 所有 API Key、連線字串存於環境變數，不得 Commit 至 Git
- `.env` 和 `appsettings.*.json`（含機密）加入 `.gitignore`
- 使用 Secret Manager 或 Azure Key Vault 管理生產環境機密

---

## 12. 效能規範

### 12.1 快取策略

| 資料 | 快取位置 | TTL | 失效條件 |
|------|---------|-----|---------|
| 文章列表（今日） | Redis | 5 分鐘 | 新文章寫入時 |
| 文章詳情 | Redis | 1 小時 | 文章更新時 |
| AI 快速預覽 | PostgreSQL + Redis | 永久 / 1 天 | 手動重新生成 |
| AI 深度報告 | PostgreSQL | 永久 | 手動重新生成 |
| 今日統計 | Redis | 10 分鐘 | 定期刷新 |

### 12.2 資料庫效能

- 文章列表查詢使用 Cursor-based Pagination（禁止 OFFSET）
- 全文搜尋使用 PostgreSQL GIN Index + `tsvector`
- 避免 N+1 查詢，使用 EF Core `Include` 或 Split Query
- 複雜查詢優先使用 Raw SQL 或 Dapper

### 12.3 前端效能

- 圖片使用 Lazy Loading
- 文章列表使用 Virtual Scrolling（超過 50 筆時啟用）
- API 呼叫使用 SWR 模式（stale-while-revalidate）
- Bundle 分析：vendor chunk 分離，每個路由獨立 chunk

---

## 13. 錯誤處理規範

### 13.1 錯誤碼清單

| 錯誤碼 | HTTP 狀態 | 說明 |
|--------|---------|------|
| `ARTICLE_NOT_FOUND` | 404 | 文章不存在 |
| `AI_SUMMARY_NOT_FOUND` | 404 | AI 總結不存在，需先生成 |
| `AI_GENERATION_IN_PROGRESS` | 409 | 該文章 AI 總結正在生成中 |
| `AI_RATE_LIMIT_EXCEEDED` | 429 | 超出 AI 生成次數限制 |
| `AI_SERVICE_UNAVAILABLE` | 503 | AI 服務暫時不可用 |
| `INVALID_CURSOR` | 400 | 分頁游標格式錯誤 |
| `INVALID_DATE_FORMAT` | 400 | 日期格式錯誤 |
| `UNAUTHORIZED` | 401 | 未驗證或 Token 過期 |
| `FORBIDDEN` | 403 | 無權限 |
| `INTERNAL_ERROR` | 500 | 內部伺服器錯誤 |

### 13.2 前端錯誤處理原則

- 所有 API 呼叫包裹 try/catch
- 網路錯誤、逾時顯示 Toast 通知
- 404 顯示空白頁佔位元件（非紅色錯誤畫面）
- AI 生成失敗提供「重試」按鈕
- SSE 連線中斷自動重連 3 次

---

## 14. 測試規範

### 14.1 測試範圍

| 類型 | 工具 | 涵蓋範圍 | 覆蓋率目標 |
|------|------|---------|----------|
| 後端 Unit Test | xUnit + Moq | Application Layer（Query/Command Handler） | ≥ 80% |
| 後端 Integration Test | xUnit + WebApplicationFactory | API Controller、Repository | ≥ 60% |
| 前端 Unit Test | Vitest | Composables、Store、Utils | ≥ 70% |
| 前端 Component Test | Vue Test Utils | 核心元件 | ≥ 50% |

### 14.2 測試命名規範

```csharp
// C# 命名格式：MethodName_Scenario_ExpectedResult
[Fact]
public async Task GetArticleList_WhenKeywordProvided_ReturnsFilteredArticles()

[Fact]
public async Task GenerateAiSummary_WhenArticleNotFound_ReturnsNotFound()
```

```typescript
// TypeScript 命名格式：describe > should
describe('useAiSummary', () => {
  it('should not trigger generation if already generating')
  it('should update streamingText on SSE chunk event')
  it('should set error message on SSE error event')
})
```

---

## 15. 部署規範

### 15.1 環境清單

| 環境 | 用途 | 對應 Branch |
|------|------|------------|
| Development | 本地開發 | `feature/*`, `fix/*` |
| Staging | 測試驗收 | `develop` |
| Production | 正式上線 | `main` |

### 15.2 CI/CD 流程

```
Push → GitHub Actions
  ├── Lint & Type Check
  ├── Unit Tests
  ├── Build（前後端）
  ├── (develop branch) → Deploy to Staging
  └── (main branch) → Deploy to Production
```

### 15.3 Docker 規範

- 前端：Multi-stage build（Node build → Nginx serve）
- 後端：Multi-stage build（SDK build → Runtime image）
- 使用 `docker-compose.yml` 管理本地開發環境
- 正式環境使用 Kubernetes 或 Azure Container Apps

---

## 16. 程式碼規範

### 16.1 通用規範

- 禁止 Magic Number，使用具名常數或 Enum
- 每個 Function 不超過 50 行，超過需重構
- 公開方法、類別必須有 XML Doc Comment（後端）或 JSDoc（前端）
- 禁止 `any` 型別（TypeScript），非必要不使用
- 所有非同步操作必須正確處理例外

### 16.2 C# 命名規範

| 類型 | 規則 | 範例 |
|------|------|------|
| Class / Interface | PascalCase | `ArticleService`, `IArticleRepository` |
| Method | PascalCase | `GetArticleListAsync` |
| Property | PascalCase | `PublishedAt` |
| Private Field | camelCase + `_` prefix | `_articleRepository` |
| Local Variable | camelCase | `articleDto` |
| Constant | PascalCase | `MaxRetryCount` |
| Enum | PascalCase | `ArticleTag.Model` |

### 16.3 TypeScript / Vue 命名規範

| 類型 | 規則 | 範例 |
|------|------|------|
| Component | PascalCase | `ArticleCard.vue` |
| Composable | camelCase + `use` prefix | `useArticles.ts` |
| Store | camelCase + `Store` suffix | `articleStore.ts` |
| Type / Interface | PascalCase | `ArticleDto`, `IApiResponse` |
| Variable / Function | camelCase | `fetchArticles`, `isLoading` |
| CSS Class | BEM kebab-case | `.article-card__title--active` |
| Constant | SCREAMING_SNAKE_CASE | `MAX_ARTICLE_LIMIT` |

### 16.4 Vue 元件規範

```vue
<script setup lang="ts">
// 1. 型別匯入
import type { Article } from '@/types/article.types'

// 2. 元件匯入
import ArticleCard from '@/components/article/ArticleCard.vue'

// 3. Composable 匯入
import { useArticles } from '@/composables/useArticles'

// 4. Props 定義
interface Props {
  initialTag?: string
}
const props = withDefaults(defineProps<Props>(), {
  initialTag: 'all',
})

// 5. Emits 定義
const emit = defineEmits<{
  (e: 'article-selected', article: Article): void
}>()

// 6. Composables
const { articles, isLoading, fetchArticles } = useArticles()

// 7. 響應式狀態
const selectedArticleId = ref<string | null>(null)

// 8. Computed
const hasArticles = computed(() => articles.value.length > 0)

// 9. Methods
function handleArticleSelect(article: Article): void {
  selectedArticleId.value = article.id
  emit('article-selected', article)
}

// 10. Lifecycle
onMounted(() => {
  fetchArticles()
})
</script>
```

---

## 17. Git 工作流程

### 17.1 Branch 命名

| 類型 | 格式 | 範例 |
|------|------|------|
| 功能開發 | `feature/FR-XXX-description` | `feature/FR-002-ai-summary-streaming` |
| Bug 修正 | `fix/issue-description` | `fix/article-duplicate-fetch` |
| 緊急修復 | `hotfix/description` | `hotfix/ai-api-timeout` |
| 文件更新 | `docs/description` | `docs/update-api-spec` |
| 重構 | `refactor/description` | `refactor/article-repository` |

### 17.2 Commit Message 規範 (Conventional Commits)

```
<type>(<scope>): <subject>

[optional body]

[optional footer]
```

**Type 清單**

| Type | 說明 |
|------|------|
| `feat` | 新功能 |
| `fix` | Bug 修正 |
| `docs` | 文件更新 |
| `style` | 排版、格式（不影響邏輯） |
| `refactor` | 重構（不修改功能） |
| `perf` | 效能優化 |
| `test` | 新增或修改測試 |
| `chore` | 建置流程、依賴更新 |
| `ci` | CI/CD 設定 |

**範例**

```
feat(ai-summary): add SSE streaming support for deep report generation

Implement Server-Sent Events for real-time AI summary generation.
The client now receives chunk-by-chunk updates instead of waiting
for the full response.

Closes #42
```

### 17.3 Pull Request 規範

- PR 標題需符合 Commit Message 格式
- 必須通過所有 CI 檢查才可合併
- 需至少 1 位 Reviewer 審核通過
- 合併方式使用 Squash Merge（保持 main 分支歷史整潔）
- PR 描述需包含：變更說明、測試方式、截圖（UI 變更時）

---

## 18. 附錄

### 18.1 詞彙表

| 術語 | 說明 |
|------|------|
| RSS | Really Simple Syndication，網站內容訂閱格式 |
| SSE | Server-Sent Events，伺服器推送事件（單向串流） |
| ULID | Universally Unique Lexicographically Sortable Identifier |
| TL;DR | Too Long; Didn't Read，摘要說明 |
| Cursor Pagination | 以游標為基礎的分頁，效能優於 OFFSET |
| GIN Index | Generalized Inverted Index，PostgreSQL 全文搜尋索引 |
| LLM | Large Language Model，大型語言模型 |
| Hangfire | .NET 背景排程框架 |

### 18.2 參考資源

- [Vue.js 3 官方文件](https://vuejs.org)
- [ASP.NET Core 8 官方文件](https://docs.microsoft.com/aspnet/core)
- [Anthropic API 文件](https://docs.anthropic.com)
- [Conventional Commits 規範](https://www.conventionalcommits.org)
- [OpenAPI 3.0 規範](https://swagger.io/specification)

### 18.3 文件更新記錄

| 版本 | 日期 | 變更說明 | 作者 |
|------|------|---------|------|
| v1.0.0 | 2026-05-12 | 初版建立 | TBD |

---

*本文件為 AI Daily 專案的正式規格文件，所有開發人員應以本文件為實作依據。如有疑問或需要修改，請提交 PR 並知會相關成員。*
