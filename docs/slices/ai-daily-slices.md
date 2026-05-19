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
| S1-S4 backfill / spec reconciliation | 尚未實作 | 依 `AI-Daily-Spec.md` 回補目前 S1-S4 基底缺口，放在 S4 後、S5 前逐項處理 |
| S5 bookmarks/auth/personalization | 已完成初版 | 已有 bookmark mutation、theme preference、本機 local-user strategy 與 bookmarks/settings UI |
| S5-1 negative feedback hidden articles | 已完成初版 | Article card/report actions 可隱藏文章；一般列表會依 local-user hidden preference 排除，並提供 undo/settings restore |

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

## Spec Comparison Summary

- planning_mode: `apply_to_existing_plan`
- reference_plan_usage: 根據 `AI-Daily-Spec.md` 推導 reference coverage，但 reference plan 不保留；只把差異整理回本 artifact。
- existing_plan_summary: 目前 S1-S4 已有基底或已進入實作脈絡；S5 尚未開始。
- matched_coverage:
  - S1 已覆蓋文章列表、filter、pagination 與 Dashboard UI。
  - S2 已規劃 article detail、Report UI 與 RSS metadata import。
  - S3 已規劃 quick summary read/display flow。
  - S4 已規劃 AI deep report generation、SSE 與 provider interface。
- missing_or_underdefined:
  - S1 缺 `GET /stats/today` 與 Dashboard stats 後端契約；recommended_action: `add_slice` -> `S1-1`。
  - S2 缺 article content extraction、content source/fallback、XSS 邊界；recommended_action: `add_slice` -> `S2-1`。
  - S2 缺 cold start feed sync 與「不要用列表查詢偷跑 crawler」的 UX/後端邊界；recommended_action: `add_slice` -> `S2-2`。
  - S2 缺 feed source quality policy、抓取深度、低價值文章過濾與候選排序；recommended_action: `add_slice` -> `S2-3`。
  - S3 缺 quick summary generation、provider metadata、promptVersion、persistence/cache policy；recommended_action: `add_slice` -> `S3-1`。
  - S4 缺 provider/spec deviation 決策、SSE event contract 對齊、rate limit、secret-safe provider errors；recommended_action: `add_slice` -> `S4-1`。
  - S5 缺 `not interested` 負回饋與個人化隱藏規則；recommended_action: `add_slice` -> `S5-1`。
- intentional_deviation:
  - MVP 可用 Gemini/Stub 作 provider，但 spec target 是 Anthropic/Claude；document_or_fix: `document MVP deviation in S4-1, keep interface stable for later fix`。
  - MVP SSE 可暫用 `started/status/report/completed/error`，但 spec target 是 `start/chunk/field_done/done/error`；document_or_fix: `S4-1 must choose versioned MVP contract or fix to spec shape`。

## S1-S4 Backfill / Spec Reconciliation

> 這一區不是重新切一份產品路線，而是根據 `AI-Daily-Spec.md` 與目前 S1-S4 已有基底的落差，整理後續要補的項目。放在 S4 與 S5 之間，是因為目前開發脈絡已推進到 S4；實際執行時仍可依依賴關係調整順序。

## Git Flow Classification Practice

這些 backfill slices 也用來練習 Git Flow 分支分類。後續 agent 在執行任一 slice 前，需先判斷並回報 Git Flow classification，再開始實作。

- `master`: 正式穩定線，不直接承接一般開發。
- `develop`: 日常整合線，feature/release/hotfix 的主要交會點。
- `feature`: 一般功能、UX、資料契約、回補、測試、非緊急修正。
- `release`: 一組功能完成後，用於發版前穩定化、版本文件、最後驗證。
- `hotfix`: 正式版本上的緊急修復，例如 secret leak、production crash、資料破壞、安全漏洞。

除非使用者明確說 production/master 已發生事故，本文件中的一般功能、UX、資料契約、回補與個人化 slice 預設都從 `develop` 開 `feature` branch。

後續 build-and-learn-loop 開始前需先回報：

```yaml
git_flow_classification:
  base_branch:
  branch_type:
  suggested_branch_name:
  reason:
```

### S1-1. Dashboard stats 與文章列表契約回補

- backfills: `S1. 可閱讀的文章列表`
- source_gap: Spec 有 `GET /stats/today`、今日文章總數、已 AI 總結數、tag/source breakdown；目前 S1 主要完成 article list 與 Dashboard UI，stats row 尚未有獨立 API 契約。
- why_now: S4 已開始產生 AI report，Dashboard 的「今日文章 / 已總結」若仍靠前端推算，後續會和持久化與快取狀態不一致。
- git_flow_classification:
  ```yaml
  base_branch: develop
  branch_type: feature
  suggested_branch_name: feature/s1-1-dashboard-stats
  reason: "Adds missing stats API contract and Dashboard UX; this is planned backfill, not a production emergency."
  ```
- user_flow: 使用者打開 dashboard，可以看到由後端統計 API 回傳的今日總文章數、AI 已總結數、熱門 tags 與 top sources；統計失敗時 article feed 仍可使用。
- files_or_areas:
  - `backend/src/AiDaily.API/Controllers/StatsController.cs`
  - `backend/src/AiDaily.Application/Stats`
  - `frontend/src/views/Dashboard.vue`
  - `frontend/src/stores/articleStore.ts`
  - `frontend/src/services/apiClient.ts`
- acceptance:
  - `GET /api/v1/stats/today` 使用既有 response envelope 回傳 totalArticles、aiSummarizedCount、tagBreakdown、topSources。
  - Dashboard stats row 改讀 stats endpoint；endpoint 失敗時顯示 fallback/empty state，不阻斷文章列表。
  - Dashboard 顯示資料新鮮度與來源同步狀態，例如 `Updated 8 min ago`、`Syncing sources...`、`6 sources synced · 1 source failed`。
  - Sync 進行中不得清空既有文章列表；source partial failure 不得讓整個 dashboard 進入錯誤畫面。
  - 文章列表排序明確依 `publishedAt` desc，並維持 cursor pagination、keyword、tag、source、date filter。
  - 至少包含一個 stats query test 與一個前端 stats loading/error state 測試。
- not_included:
  - Redis-backed stats cache。
  - 進階 PostgreSQL full-text ranking。
  - observability dashboard。
- reason_first: S1 的核心閱讀流程已成立，但 spec 要求的 Dashboard 統計尚未成為後端契約；先補這層可讓後續持久化與 AI 狀態有一致來源。

### S2-1. Article content enrichment 與來源匯入回補

- backfills: `S2. 文章詳情與來源匯入`
- source_gap: Spec 的 article detail、資料表與 AI prompt 都假設有 `content`；目前 S2 只規劃 RSS metadata import，尚未定義 sourceUrl 原文擷取、clean text、fallback 與 XSS 邊界。
- why_now: S4 的 AI report 若只吃 seed/RSS summary，只能算 fallback report；要往 spec 的深度分析靠近，需要先補 article content 生命週期。
- git_flow_classification:
  ```yaml
  base_branch: develop
  branch_type: feature
  suggested_branch_name: feature/s2-1-article-content-enrichment
  reason: "Adds article content extraction and AI input quality backfill; this expands planned functionality and is not a hotfix."
  ```
- user_flow: 使用者打開 report 或觸發 AI generation 時，系統優先使用已擷取、清理並儲存的文章內容；若來源抓取失敗，仍 fallback 到 RSS summary/source metadata。
- files_or_areas:
  - `backend/src/AiDaily.Domain/Entities/Article.cs`
  - `backend/src/AiDaily.Application/Articles`
  - `backend/src/AiDaily.Infrastructure/FeedCrawler`
  - `backend/src/AiDaily.Infrastructure/ContentExtraction`
  - `backend/src/AiDaily.Infrastructure/Persistence`
  - `backend/tests/AiDaily.UnitTests`
- acceptance:
  - Article detail DTO 明確支援 `content` 或 `contentText`，並能標示內容來源是 full content、RSS summary 或 fallback metadata。
  - Report page 顯示 Source Content 狀態：`Full content ready`、`Extracting source`、`Summary fallback`、`Extraction failed`。
  - AI report 操作需依 content 狀態調整文案與選項：full content 時顯示 `Generate full AI report`；content missing/failed 時顯示 `Generate from summary`；extracting 時允許等待或用 summary fallback。
  - 系統可從 `sourceUrl` 抓取 HTML，擷取 readable text，移除 script/style/navigation 等非正文內容，並限制傳給 AI 的內容長度。
  - Content extraction 失敗時記錄 log，不讓 article detail、crawler 或 AI generation crash。
  - API 回傳內容需避免 XSS；若保存 HTML，需回傳 sanitize 後內容或只回 clean text。
  - 至少包含 extraction success 與 extraction failure fallback 測試。
- not_included:
  - 完整商業級 readability engine 調校。
  - 需要瀏覽器執行 JavaScript 的網站擷取。
  - 反 bot 繞過、付費代理、多來源事實查核。
- reason_first: 這是 S2 對 spec 最大的資料生命週期缺口，也直接決定 S3/S4 的 AI 輸入品質。

### S2-2. Cold start feed sync UX 與同步邊界回補

- backfills: `S2. 文章詳情與來源匯入`
- source_gap: Spec 需要每日自動抓取多來源新聞，但目前專案不一定常駐；若使用者第一次打開時資料庫沒有今日文章，不能長期顯示假資料，也不能讓 `GET /api/v1/articles` 同步阻塞式爬文。
- why_now: 使用者第一屏會先進 Dashboard；如果沒有 cold start sync UX，S1/S2 即使技術可用，產品仍會呈現空白、假資料或長時間 loading。
- git_flow_classification:
  ```yaml
  base_branch: develop
  branch_type: feature
  suggested_branch_name: feature/s2-2-cold-start-feed-sync
  reason: "Adds cold-start sync UX and read/write API boundary; this is planned UX/data-flow work, not a production emergency."
  ```
- user_flow: 使用者第一次打開 Dashboard 時，系統先讀已保存文章；若今日無資料或資料過舊，前端顯示明確同步狀態並觸發一次 today sync，抓取完成後更新列表。
- files_or_areas:
  - `backend/src/AiDaily.API/Controllers/ArticlesController.cs`
  - `backend/src/AiDaily.API/Controllers/FeedCrawlController.cs`
  - `backend/src/AiDaily.Application/FeedCrawler`
  - `backend/src/AiDaily.Infrastructure/FeedCrawler`
  - `frontend/src/views/Dashboard.vue`
  - `frontend/src/stores/articleStore.ts`
  - `frontend/src/services/apiClient.ts`
- acceptance:
  - `GET /api/v1/articles` 只讀已保存文章，不直接觸發 RSS crawler 或 source extraction。
  - 新增明確同步入口，例如 `POST /api/v1/feed-crawl/run?scope=today`，用於 cold start 或手動 refresh 觸發背景/一次性同步。
  - Dashboard 根據資料狀態顯示 `empty_fresh_start`、`stale_with_data`、`ready`、`sync_failed`。
  - `empty_fresh_start` 時不顯示假文章；顯示 `Fetching today's AI news...` 與 skeleton cards。
  - `stale_with_data` 時保留舊文章列表，顯示 `Updating today's feed in the background...`，不得清空列表。
  - Sync 成功後可用 subtle banner 顯示新增文章數；partial source failure 顯示來源狀態，不阻斷閱讀。
  - Sync 失敗時顯示 retry action；若有舊資料，仍允許閱讀舊資料。
  - 至少包含一個 cold start empty sync 測試，以及一個 stale-with-data 不清空列表的前端測試。
- not_included:
  - Hangfire 常駐排程完整化。
  - 完整 10 個來源的 production source tuning。
  - admin feed-source management UI。
  - 需要登入權限的 production crawl trigger。
- reason_first: 這個 slice 定義第一屏資料取得的 UX 邊界，避免 crawler 和 read API 綁死，也避免使用者長期看到 seed/fake data。

### S2-3. Feed source quality 與候選文章篩選回補

- backfills: `S2. 文章詳情與來源匯入`
- source_gap: 目前只規劃從少量固定 RSS source 匯入文章，但尚未定義哪些 feed URL 適合、每個 source 應抓多少候選、如何排除低價值文章，以及「只取前 10 筆」導致全是無意義文章時的補救策略。
- why_now: Dashboard 的文章品質取決於 feed pipeline；若來源 URL 與候選篩選不穩，後續 AI summary/report 會把成本花在低價值文章上，使用者也會一直看到不想讀的內容。
- git_flow_classification:
  ```yaml
  base_branch: develop
  branch_type: feature
  suggested_branch_name: feature/s2-3-feed-quality-filtering
  reason: "Adds feed source quality rules, candidate depth, and low-value article filtering; this is ingestion quality work, not a production emergency."
  ```
- user_flow: 使用者打開 Dashboard 時，看到的是經過來源品質規則與候選篩選後的 AI 新聞；系統會避免把來源首頁公告、活動頁、廣告、職缺、純 release note index 或與 AI 無關的短文長期排在前面。
- files_or_areas:
  - `backend/src/AiDaily.Domain/Entities/FeedSource.cs`
  - `backend/src/AiDaily.Application/FeedCrawler`
  - `backend/src/AiDaily.Infrastructure/FeedCrawler`
  - `backend/src/AiDaily.Infrastructure/ContentExtraction`
  - `backend/src/AiDaily.Application/Articles`
  - `backend/tests/AiDaily.UnitTests`
  - `docs/feed-source-policy.md`
- acceptance:
  - Feed source 設定需記錄 source type、topic scope、default candidate limit、enabled state、quality notes；不要只保存 URL。
  - 每個 source 先抓取超過畫面需要量的候選，例如 `candidateLimit`，再依品質規則篩選後才保存或顯示；不得把「RSS 前 10 筆」直接視為今日最佳 10 筆。
  - 候選文章需有 deterministic pre-filter：標題/摘要/source metadata 至少命中 AI 相關關鍵詞或 allowlist topic；明顯的 job、event-only、sponsor、newsletter housekeeping、index page、過短內容要標記或排除。
  - Article 需保存 ingestion metadata，例如 `ingestionScore`、`rejectionReason`、`matchedKeywords`、`sourceQualityTier` 或等價欄位，方便之後調整規則。
  - `GET /api/v1/articles` 預設只回傳未被 rejection 的文章，並依 `publishedAt` 與 ingestion quality 排序；debug/admin 模式才看被排除候選。
  - 若某個 source 前 N 筆都被排除，crawler 應繼續掃描到 `candidateLimit` 或停止條件，不因前 10 筆品質差就產生空白首頁。
  - Feed source policy 文件需說明「如何選 URL」：優先 RSS/Atom、AI topic/tag feed、官方 blog news feed、研究 lab news；避免 homepage、search result HTML、需要 JS 的頁面、商業列表頁。
  - 至少包含一個「前 10 筆低價值但後續有 AI 文章」的 crawler/filter 測試，以及一個 rejected article 不出現在一般列表的 query test。
- not_included:
  - Admin feed-source management UI。
  - LLM-based relevance ranking。
  - 付費 search/news API。
  - 反 bot 繞過或需要瀏覽器執行 JavaScript 的來源。
- reason_first: 這不是單純多加幾個 RSS URL，而是先把來源選擇、候選深度與 deterministic quality gate 定義清楚，避免產品一直餵給使用者低價值文章。

### S3-1. AI quick summary 生成、持久化與快取回補

- backfills: `S3. AI 摘要預覽`
- source_gap: Spec 要右側 quick summary 由 AI 產生並永久快取；目前 S3 主要是讀取/展示 quick summary，缺 prompt version、provider metadata、持久化與 cache policy。
- why_now: S4 已接 provider 後，quick summary 不應長期停留在 in-memory seed/read-only 狀態，否則 Dashboard 和 report 的 AI 狀態會分裂。
- git_flow_classification:
  ```yaml
  base_branch: develop
  branch_type: feature
  suggested_branch_name: feature/s3-1-ai-summary-persistence
  reason: "Adds AI quick summary generation, persistence, and cache behavior; this is planned feature/backfill work."
  ```
- user_flow: 使用者在 article card/detail 開啟 AI summary panel；若 summary 已存在則快速讀取，若不存在則可由後端生成或回可辨識 empty state。
- files_or_areas:
  - `backend/src/AiDaily.API/Controllers/AiSummaryController.cs`
  - `backend/src/AiDaily.Application/AiSummaries`
  - `backend/src/AiDaily.Domain/Entities/AiSummary.cs`
  - `backend/src/AiDaily.Infrastructure/AI`
  - `backend/src/AiDaily.Infrastructure/Cache`
  - `frontend/src/components/ai/AiSummaryPanel.vue`
  - `frontend/src/stores/aiSummaryStore.ts`
- acceptance:
  - AI quick summary persistence 記錄 articleId、highlights、impactScope、controversy、editorView、provider、promptVersion、generatedAt。
  - 未 forced regeneration 時，同一篇文章不重複呼叫 provider。
  - cached reads 需避免不必要的 DB/provider calls；Redis 可為 optional adapter，但 cache key 與失效條件需文件化。
  - 若 article content 尚未完成，生成可 fallback summary/source metadata，並在 prompt/結果中避免宣稱看過完整原文。
  - DTO 與 spec 差異需標記，例如 `highlights` 是 list 還是 spec 範例中的 string。
- not_included:
  - Deep report streaming。
  - Prompt A/B testing UI。
  - 多 provider routing UI。
- reason_first: S3 是 S4 的輕量 AI 入口；把 quick summary 的生成/持久化補齊，可避免所有 AI 價值都壓到 deep report。

### S4-1. Deep report provider、SSE contract、rate limit 與 spec 偏差回補

- backfills: `S4. 串流深度報告產生`
- source_gap: Spec 目標 provider 是 Anthropic/Claude、SSE event 是 `start/chunk/field_done/done/error`、report score 欄位與目前 MVP shape 不完全一致；目前 S4 也需要明確 secret handling、schema validation、rate limiting 與 fallback 邊界。
- why_now: S4 已經開始接真 provider 與 API key；若不補 contract 與安全邊界，後續 S5/auth 前會累積 provider 成本、資料格式與前端解析風險。
- git_flow_classification:
  ```yaml
  base_branch: develop
  branch_type: feature
  suggested_branch_name: feature/s4-1-report-contract-hardening
  reason: "Hardens planned report provider, SSE, rate-limit, and contract behavior; use hotfix only if fixing a released secret leak, crash, or security incident."
  ```
- user_flow: 使用者在 report page 觸發 AI generation，前端收到可預期的 SSE events；生成成功後讀到固定 DTO，生成失敗時看到可重試且不洩漏 secret 的錯誤。
- files_or_areas:
  - `frontend/src/views/Report.vue`
  - `frontend/src/composables/useAiReportStream.ts`
  - `backend/src/AiDaily.API/Controllers/AiSummaryController.cs`
  - `backend/src/AiDaily.Application/AiSummaries`
  - `backend/src/AiDaily.Infrastructure/AI`
  - `backend/src/AiDaily.API/Middleware`
  - `backend/src/AiDaily.Application/Common/Errors`
- acceptance:
  - Provider choice 文件化為 MVP 偏差：spec target 是 Anthropic/Claude，MVP 可用 Gemini/Stub；document_or_fix: 先文件化為 MVP deviation，並透過 application interface 隔離，後續切回 Claude 或多 provider 不改前端 contract。
  - API key 只能由後端安全設定讀取，不得寫入 repo，不得暴露給前端；錯誤訊息不得包含 key 或 provider raw URL。
  - AI report 必須通過後端 schema/enum/array length/score range 驗證；可修復的缺欄位需 normalizer 補齊，不能修復則回 `AI_REPORT_INVALID_FORMAT`。
  - SSE event shape 需明確決策：採 spec `start/chunk/field_done/done/error`，或將 MVP `started/status/report/completed/error` 文件化為版本化 contract；document_or_fix 必須寫入 API/SSE contract。
  - AI generation 有最小 rate limiting；超過限制回 `AI_RATE_LIMIT_EXCEEDED` 或等價文件化錯誤。
  - 在 S2-1 未完成前，S4 只能宣稱使用 summary/source metadata fallback，不宣稱 full-content deep report。
- not_included:
  - Prompt A/B testing UI。
  - Exporting reports。
  - 完整 Anthropic/OpenAI/provider switching UI。
- reason_first: S4 是成本、安全、contract 漂移最容易放大的地方；這個回補項讓已開始的 provider 串接可被後續 slice 穩定承接。

### S5. 個人化、主題與收藏

- git_flow_classification:
  ```yaml
  base_branch: develop
  branch_type: feature
  suggested_branch_name: feature/s5-personalization
  reason: "Adds bookmarks, theme preferences, minimal personalization/auth behavior, and prepares the same feature branch for S5-1 negative feedback work; this is planned user-facing feature work, not a production emergency."
  ```
- user_flow: 使用者切換 light/dark theme、收藏文章、打開 `/bookmarks`，並讓偏好設定跨 session 保留；後續可延伸為對不感興趣文章的負回饋。
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

### S5-1. 不感興趣文章隱藏與負回饋個人化

- backfills: `S5. 個人化、主題與收藏`
- source_gap: S5 目前只規劃正向收藏與主題偏好，缺少「我不在乎這篇文章」這種負回饋；若沒有隱藏規則，使用者會一直看到已表明不想看的文章。
- why_now: Feed quality 可以先靠 S2-3 做全域過濾，但每個使用者不在乎的文章、來源或 topic 不同；這應該落在個人化層，不應該污染全域 crawler 規則。
- git_flow_classification:
  ```yaml
  base_branch: develop
  branch_type: feature
  suggested_branch_name: feature/s5-personalization
  reason: "Extends the S5 personalization feature branch with negative feedback and hidden article filtering; this is user preference work, not a production emergency."
  ```
- user_flow: 使用者在 article card 或 detail actions 點選 `Not interested`；該文章立即從 Dashboard feed 消失，之後重新整理或再次同步也不會在一般列表中顯示，除非使用者到偏好/隱藏列表復原。
- files_or_areas:
  - `frontend/src/components/article/ArticleCard.vue`
  - `frontend/src/views/Dashboard.vue`
  - `frontend/src/stores/articleStore.ts`
  - `frontend/src/stores/preferenceStore.ts`
  - `frontend/src/services/apiClient.ts`
  - `backend/src/AiDaily.API/Controllers/UserPreferencesController.cs`
  - `backend/src/AiDaily.Application/UserPreferences`
  - `backend/src/AiDaily.Domain/Entities/HiddenArticle.cs`
  - `backend/src/AiDaily.Application/Articles`
- acceptance:
  - Article card 與 detail action 區提供 `Not interested` 操作；操作後該 article 立即從目前 feed 移除，不等重新整理。
  - 後端保存 hidden article preference，至少記錄 user/localUserId、articleId、reason optional、createdAt。
  - `GET /api/v1/articles` 預設排除目前使用者 hidden articles；pagination 不應因 hidden items 造成重複或空頁。
  - 若尚未完成正式 auth，需使用明確的 temporary local-user strategy，不得把所有使用者共用同一份 hidden state。
  - 提供復原入口，例如 settings hidden list 或 toast undo；復原後文章可再次出現在列表。
  - Hidden article 不等於刪除 article，也不影響其他使用者、source statistics 或 AI summary/report persistence。
  - 至少包含一個 backend query test，驗證 hidden article 不出現在一般列表；以及一個前端 store/component test，驗證點選後 feed 立即移除且可 undo。
- not_included:
  - 完整推薦系統。
  - 根據負回饋自動封鎖整個來源或 topic。
  - 多裝置帳號同步超出 S5 auth MVP 的部分。
  - 用 LLM 推論使用者長期偏好。
- reason_first: 這是個人化的負回饋能力，應該和收藏/偏好一起演進；它解決使用者明確說「我不在乎」後 feed 仍反覆顯示同一文章的問題。

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
first_slice: S1-1. Dashboard stats 與文章列表契約回補
s5_handoff:
  execution_rule: "Use the same feature branch, but run one slice per build-and-learn-loop pass; do not merge S5 and S5-1 into one implementation pass."
  shared_branch: feature/s5-personalization
  recommended_order:
    - S5. 個人化、主題與收藏
    - S5-1. 不感興趣文章隱藏與負回饋個人化
  reason: "S5 establishes bookmark/theme and minimal user preference/auth behavior; S5-1 extends that personalization layer with negative feedback and hidden article filtering."
```
