# AI Daily 切片規劃

## 目標

把 AI Daily 建成一個可實際使用的每日 AI 新聞閱讀器。系統包含 Vue 3 前端、ASP.NET Core 8 API、PostgreSQL 持久化、必要時使用 Redis 快取、RSS 匯入流程，以及由 AI 產生的文章摘要與深度報告。

這份規劃偏向「垂直切片」：每個切片都要交付一個使用者看得到、可以測試的流程，而不是一開始就先把所有基礎設施全部做完。

## 假設

- 目標 repo 範圍是 `AINews/`。
- 目前 repo 起點只有 `AI-Daily-Spec.md`，所以早期切片會包含專案骨架建立。
- MVP 應優先完成每日文章列表、文章詳情、AI 摘要預覽、深度報告產生，以及收藏功能。
- 使用者專屬功能需要時，可以先引入最小可用的本機/JWT 驗證能力；公開唯讀的文章瀏覽可以在完整驗證前先運作。
- RSS 匯入可以先從少量固定來源開始，等 feed pipeline 穩定後再擴充來源。
- AI provider 整合應包在 application interface 後面，讓日後更換模型或供應商時影響範圍集中。

## 目標 Repo 範圍

- 前端 app：Vue 3、Vite、TypeScript、Pinia、Vue Router，以及由 `dashboard.html` / `report.html` 轉入的 custom CSS design tokens。若之後需要成熟元件庫，再另外評估加入。
- 後端 app：ASP.NET Core 8 Web API、EF Core、PostgreSQL、Redis、Hangfire、Serilog、OpenAPI。
- 共用契約：API response envelope、pagination shape、article DTOs、AI summary/report DTOs、error codes。
- 測試：每個交付切片都要有聚焦的後端 unit/integration tests，以及前端 store/component tests。

## 第一個切片

S1. 可閱讀的文章列表

選它作為第一個切片，是因為它能證明產品最核心的使用者價值：使用者打開 app 後，可以快速掃描近期 AI 文章。它也會建立後續切片要沿用的 repo 結構、API 慣例、資料庫模型與前端資料流。

## 目前切片狀態

| 範圍 | 狀態 | 說明 |
|------|------|------|
| S1 Dashboard UI | 已完成初版 | `dashboard.html` 已轉入 `frontend/src/views/Dashboard.vue`，包含 top nav、toolbar、stats、feed 與右側 panels |
| S1 文章列表 API | 已完成初版 | `GET /api/v1/articles` 已可回傳文章列表；目前資料仍來自 in-memory seed |
| S2 Report UI | 已完成初版 | `report.html` 已轉入 `frontend/src/views/Report.vue`，目前路由為 `/report/:id` |
| S2 article detail API / RSS 匯入 | 尚未實作 | `GET /api/v1/articles/{id}`、FeedCrawler、RSS persistence 尚待後續切片 |
| S3/S4 AI summary/report pipeline | 尚未實作 | AI API key、AI provider service、SSE streaming、summary/report persistence 尚待後續切片 |
| S5 bookmarks/auth | 尚未實作 | 目前僅有 UI 顯示與規格規劃，尚未有 mutation 與登入流程 |

## 切片

### S1. 可閱讀的文章列表

- user_flow: 使用者打開 `/`，看到近期 AI 文章，可以用 keyword/tag/source/date 篩選，並透過 cursor pagination 載入更多結果。
- files_or_areas:
  - `backend/AiDaily.sln`
  - `backend/src/AiDaily.API`
  - `backend/src/AiDaily.Application`
  - `backend/src/AiDaily.Domain`
  - `backend/src/AiDaily.Infrastructure`
  - `backend/tests/AiDaily.UnitTests`
  - `frontend`
  - `frontend/src/views/Dashboard.vue`
  - `frontend/src/components/article/ArticleFeed.vue`
  - `frontend/src/components/article/ArticleCard.vue`
  - `frontend/src/components/common/TagFilter.vue`
  - `frontend/src/stores/articleStore.ts`
  - `frontend/src/services/apiClient.ts`
  - `backend/src/AiDaily.API/Controllers/ArticlesController.cs`
  - `backend/src/AiDaily.Infrastructure/Persistence/AiDailyDbContext.cs`
  - `backend/src/AiDaily.Domain/Entities/Article.cs`
  - `docker-compose.yml`
- acceptance:
  - `GET /api/v1/articles` 會用已文件化的 response envelope 回傳文章。
  - Cursor pagination 支援 `cursor`、`limit`、`keyword`、`tags`、`source`、`date`。
  - Dashboard 會渲染 article cards，包含 title、summary、source、tags、published time、AI-summary availability、bookmark state、read time。
  - Dashboard 版型需承接 `dashboard.html` 的主要結構：top nav、toolbar、stats row、feed、右側 quick summary/bookmarks/topic/source panels。
  - Loading、empty、API error states 都要可見，而且不能阻斷整個畫面。
  - 後端至少包含一個 repository/query handler test，覆蓋 filtering 與 pagination。
  - 前端至少包含一個 store 或 component test，覆蓋 article list loading/rendering。
- not_included:
  - RSS crawler automation。
  - AI summary generation。
  - Bookmark mutation。
  - Login 或 user profiles。
  - Production deployment。
- reason_first: 用最小但有用的前後端契約，建立端到端 app skeleton 與主要閱讀流程。

### S2. 文章詳情與來源匯入

- user_flow: 使用者選取一篇文章，打開 `/report/:id`，看到由 `report.html` 範本轉入的報告頁；系統也可以從設定好的 RSS sources 匯入新文章。
- files_or_areas:
  - `frontend/src/views/Report.vue`
  - `frontend/src/router`
  - `frontend/src/composables/useArticle.ts`
  - `backend/src/AiDaily.API/Controllers/ArticlesController.cs`
  - `backend/src/AiDaily.Application/Articles`
  - `backend/src/AiDaily.Infrastructure/FeedCrawler`
  - `backend/src/AiDaily.Domain/Entities/FeedSource.cs`
  - `backend/src/AiDaily.Infrastructure/Persistence/Configurations`
  - `backend/src/AiDaily.Infrastructure/Migrations`
- acceptance:
  - `GET /api/v1/articles/{id}` 會回傳完整文章詳情，或回傳 `ARTICLE_NOT_FOUND`。
  - `frontend/src/router` 需保留 `/report/:id` route，並由 `Report.vue` 承接頁面。
  - Report 版型需承接 `report.html` 的主要結構：header、origin card、TL;DR、key points、pros/cons、timeline、impact meter、editor opinion、related tags、actions。
  - Feed sources 會被持久化表示，並且可以 seed/configure。
  - Crawler job 可以抓取至少一個 RSS source，正規化 article data，依 `source_url` 去重，並儲存文章。
  - Crawler 失敗時要記錄 log，且不能讓 API crash。
  - Article detail view 要處理 loading、not found、success states。
- not_included:
  - AI-generated summary/report。
  - 使用者專屬 bookmarks。
  - 完整 source list 調校與只能靠 scraping 的來源。
- reason_first: 在保持使用者流程簡單的前提下，把 app 從靜態/手動文章資料推進成可持續更新的新聞產品。

### S3. AI 摘要預覽

- user_flow: 使用者掃描 article card 或 detail page，打開 AI summary panel，當摘要存在時可以看到 highlights、impact scope、controversy、editor view。
- files_or_areas（planned）:
  - `frontend/src/components/ai/AiSummaryPanel.vue`
  - `frontend/src/stores/aiSummaryStore.ts`
  - `frontend/src/services/aiSummaryApi.ts`
  - `backend/src/AiDaily.API/Controllers/AiSummaryController.cs`
  - `backend/src/AiDaily.Application/AiSummaries`
  - `backend/src/AiDaily.Domain/Entities/AiSummary.cs`
  - `backend/src/AiDaily.Infrastructure/AI`
  - `backend/src/AiDaily.Infrastructure/Cache`
- acceptance:
  - `GET /api/v1/articles/{id}/ai-summary` 會用已文件化的 envelope 回傳快速預覽欄位。
  - 摘要不存在時，會回傳前端可辨識的文件化錯誤或 empty state。
  - AI summary persistence 支援快速預覽欄位，並能連回 articles。
  - 在設定允許時，cached reads 要避免不必要的 database 或 AI provider calls。
  - 前端要清楚顯示 summary fields、loading state、empty state。
- not_included:
  - Streaming generation。
  - Deep report page。
  - PDF/export/share。
- reason_first: 不先引入 streaming 或長篇報告的複雜度，先加入核心 AI 價值。

### S4. 串流深度報告產生

- user_flow: 使用者打開 article report page，觸發 AI report generation，看著內容串流產生，最後看到完整 TL;DR、key points、pros/cons、timeline、scores、related tags、editor note、rating。
- files_or_areas（planned / extend existing `Report.vue`）:
  - `frontend/src/views/Report.vue`
  - `frontend/src/components/ai/ImpactMeter.vue`
  - `frontend/src/composables/useAiReportStream.ts`
  - `backend/src/AiDaily.API/Controllers/AiSummaryController.cs`
  - `backend/src/AiDaily.Application/AiSummaries/GenerateAiSummary`
  - `backend/src/AiDaily.Infrastructure/AI/ClaudeAiService.cs`
  - `backend/src/AiDaily.Application/Common/Errors`
  - `backend/src/AiDaily.API/Middleware`
- acceptance:
  - `POST /api/v1/articles/{id}/ai-summary/generate` 會用已文件化的 event shape 串流 SSE events。
  - `GET /api/v1/articles/{id}/ai-report` 會回傳已完成的 report。
  - Rate limiting 要防止重複觸發昂貴的 generation。
  - 同一篇文章正在 generation 時，再次觸發要回傳 `AI_GENERATION_IN_PROGRESS`。
  - Streaming 失敗時，前端可以 reconnect 或顯示 retry state。
  - 生成後的 report 會被儲存，之後造訪會重用；除非允許 forced regeneration。
- not_included:
  - Multi-provider AI routing。
  - Prompt A/B testing UI。
  - Exporting reports。
- reason_first: 在文章資料與摘要儲存已存在後，完成產品差異化最高的工作流程。

### S5. 個人化、主題與收藏

- user_flow: 使用者切換 light/dark theme、收藏文章、打開 `/bookmarks`，並讓偏好設定跨 session 保留。
- files_or_areas（planned）:
  - `frontend/src/components/common/ThemeToggle.vue`
  - `frontend/src/components/bookmark/BookmarkList.vue`
  - `frontend/src/views/Bookmarks.vue`
  - `frontend/src/views/Settings.vue`
  - `frontend/src/stores/bookmarkStore.ts`
  - `frontend/src/stores/themeStore.ts`
  - `backend/src/AiDaily.API/Controllers/BookmarksController.cs`
  - `backend/src/AiDaily.Domain/Entities/Bookmark.cs`
  - `backend/src/AiDaily.Application/Bookmarks`
  - `backend/src/AiDaily.API/Auth`
- acceptance:
  - `POST /api/v1/articles/{id}/bookmark` 與 `DELETE /api/v1/articles/{id}/bookmark` 會更新 bookmark state。
  - Bookmark list 會顯示已收藏文章與 empty state。
  - Theme 預設跟隨 system preference，也可以手動設定並持久化。
  - 使用者專屬 mutation 需要 authentication，或採用一個範圍明確的暫時 local-user strategy。
  - Bookmark 與 theme 行為要有聚焦的前端測試覆蓋。
- not_included:
  - Social sharing。
  - PDF export。
  - 超出 MVP 最小驗證方案的 multi-device account management。
- reason_first: 在核心閱讀與 AI 工作流程被證明後，加入提升留存與舒適度的功能。

## 延後處理

- Report 的 PDF export。
- 可分享的 report links。
- Admin feed-source management UI。
- Anthropic/OpenAI/provider switching UI。
- 完整 observability dashboards。
- Kubernetes 或 Azure Container Apps deployment。
- Prompt version experiment tooling。
- 超出初始 PostgreSQL search/indexes 的進階 full-text ranking。

## 交接給 Build And Learn Loop

```yaml
next_skill: build-and-learn-loop
artifact_path: docs/slices/ai-daily-slices.md
first_slice: Readable Article Feed
```
