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

## 歷史第一個切片，不是下一步

S1. 可閱讀的文章列表

S1 是專案起步時的第一個切片，因為它能證明產品最核心的使用者價值：使用者打開 app 後，可以快速掃描近期 AI 文章。它也建立了後續切片沿用的 repo 結構、API 慣例、資料模型與前端資料流。

目前不要從 S1 重新開始。新的 implementation 入口以 `Prompt-Ready Next Work` 的 `Recommended Order` 為準。

## 目前切片狀態

Status legend:

- `done`: acceptance 已完成，且有對應測試或可重複驗證。
- `partial`: 功能存在，但仍缺 contract、edge case、測試或 UX 補強。
- `volatile MVP`: 可 demo，但資料在 API 重啟後會消失，或只適合本機驗證。
- `contract risk`: 前後端或文件 contract 已漂移，下一步應先收斂。
- `planned`: 尚未實作。

| Slice | 狀態 | 目前實作事實 | 主要缺口 | 驗證狀態 |
|------|------|------|------|------|
| S1 文章列表 | partial / volatile MVP | `GET /api/v1/articles`、Dashboard UI、filter、pagination 已存在 | 核心資料仍是 in-memory；cursor 是 offset index，不是穩定 keyset cursor；缺 HTTP integration test | 後端 service tests 與前端 store tests 有覆蓋 |
| S1-1 Dashboard stats | partial | `GET /api/v1/stats/today`、Dashboard stats row 與 source sync status 已存在 | 文件狀態落後；AI summary count 依賴 article flag，生成後可能不同步；缺 HTTP/frontend component 測試 | 後端 service test 有覆蓋 |
| S2 Article detail / feed import | partial / volatile MVP | `GET /api/v1/articles/{id}`、RSS crawler、content extraction、cold-start sync 已存在 | RSS/文章資料無 DB persistence；rejected candidates 目前只 log 不保存；reader detail 對 hidden/rejected direct access policy 未定 | 後端 service tests 有覆蓋 |
| S3 AI quick summary | partial / volatile MVP | quick summary read/generate/cache、provider metadata、promptVersion 已存在 | summary persistence/cache 仍 in-memory；`article.hasAiSummary` 不會隨生成同步；quick summary 生成缺並行防重與 provider error mapping | 後端 service tests 與前端 store tests 有覆蓋 |
| S4 AI deep report | partial / contract hardening | report read/generate、Gemini/Stub provider、版本化 MVP SSE contract、schema normalizer/validator、per-user/per-article rate limit 已存在 | persistence 仍是 in-memory；spec target 的 Claude provider 與 `start/chunk/field_done/done/error` SSE shape 仍是已文件化偏差 | 後端 service 與 SSE wire-level tests、前端 SSE parser tests 有覆蓋 |
| S5 bookmarks/theme | partial / volatile MVP | bookmark mutation/list、theme preference、bookmarks/settings UI、local-user strategy 已存在 | bookmark/personalization 仍 in-memory；尚未有正式 auth；mutation error 會污染全域 article error state | 後端 service tests 與前端 store tests 有覆蓋 |
| S5-1 hidden articles | partial / volatile MVP | Article card/report actions 可 hide/restore；一般列表依 local-user hidden preference 排除；提供 undo/settings restore | hidden state 仍 in-memory；detail/report direct access policy 未定；pagination 仍可能因 offset cursor 漏/重複 | 後端 service tests 與前端 store tests 有覆蓋 |

## Current Contract Deviations

- AI provider: spec target 是 Anthropic/Claude；目前 MVP 可用 Gemini 或 Stub。S4-1 預設先文件化 Gemini/Stub MVP deviation，並保持 `IAiReportGenerator` 介面穩定；若要切回 Claude，需由使用者明確指定。
- SSE events: spec target 是 `start/chunk/field_done/done/error`；目前實作是 `started/status/report/completed/error`。S4-1 預設版本化目前 MVP contract；若要改成 spec shape，需先停下來問使用者。
- Persistence: spec target 是 PostgreSQL 與 Redis；目前 core repositories/cache 多為 in-memory，屬於 volatile MVP。
- Rate limit/auth: spec 有 AI/API rate limit 與驗證要求；S4-1 只處理 AI generate rate limit/auth guard。Feed crawl POST guard 屬於獨立 API write guard follow-up，不放進 S4-1。
- Pagination: 文件稱 cursor pagination；目前 cursor 是 base64 offset index，不是 `(publishedAt, ingestionScore, id)` 類 keyset cursor。
- Feed rejected metadata: policy 期望保留 rejected metadata；目前 crawler 對 rejected candidates 只 log 後略過。

## Prompt-Ready Next Work

這一段是給使用者或下一個 agent 直接選任務用的 canonical 入口。若不確定要做什麼，優先照 `Recommended Order` 從上往下做；每次只做一個 item，不要把多個 slice 混在同一輪。

若本段和下方某個 slice 的 `Next actions` 或最後的 handoff YAML 看起來衝突，以本段為準；下方 slice 內容是 reference 與 owner context。

### Recommended Order

| 順序 | Prompt target | Owner slice | Branch suggestion | 做完代表什麼 |
| --- | --- | --- | --- | --- |
| 1 | AI report contract hardening | `S4-1` | `feature/s4-1-report-contract-hardening` | SSE/provider/rate-limit contract 穩定，昂貴 AI 入口不再裸奔 |
| 2 | Article/feed persistence baseline | `P1a` | `feature/p1a-article-feed-persistence` | feed articles 與 feed metadata 重啟後仍存在 |
| 3 | Quick summary state sync | `S3-1` | `feature/s3-1-summary-state-sync` | 生成 summary 後 article badge/stats/cache 狀態一致 |
| 4 | Feed relevance gate and ranking hardening | `S2-3b` | `feature/s2-3b-feed-relevance-ranking` | 低訊號 RSS candidate 不因 source metadata 含 AI 而進 feed，reader list 不再只偏新鮮度 |
| 5 | Rejected metadata decision | `S2-3a` | `feature/s2-3a-rejected-metadata-decision` | rejected candidates 不再只是在計畫裡消失 |
| 6 | Source/content quality upgrade | `S2-4` | `feature/s2-4-source-content-quality` | 來源少而精，正文抽取不再把雜訊或短摘要假裝成 full content |
| 7 | Reader list cursor hardening | `S1` follow-up | `feature/s1-article-list-cursor-hardening` | hidden/filter/new articles 下 pagination 不漏不重 |

### Copy-Paste Prompts

#### 1. S4-1 AI report contract hardening

```text
請遵守根目錄 AGENTS.md，根據 docs/slices/ai-daily-slices.md 的 S4-1，實作 AI report provider、SSE contract、rate limit 與 spec 偏差回補。

只做 S4-1，不要做 persistence baseline、quick summary、feed quality、feed crawl POST guard、bookmark/hidden 功能。

開始前先回報 git_flow_classification，預設：
- base_branch: develop
- branch_type: feature
- suggested_branch_name: feature/s4-1-report-contract-hardening

成功標準：
- SSE contract 預設採「版本化目前 MVP shape」：`started/status/report/completed/error`，並寫成明確 API/SSE contract。若你認為必須改成 spec shape `start/chunk/field_done/done/error`，先停下來問使用者，不要自行拍板。
- AI generation 有 per-user/per-article 或等價最小 rate limit。
- 超過限制回文件化錯誤，例如 AI_RATE_LIMIT_EXCEEDED。
- Provider error 不洩漏 API key、raw provider URL 或 secret。
- Prompt content 有 deterministic length/token budget 上限。
- 補後端 HTTP/SSE integration tests，以及前端 SSE parser 或 composable tests。

validation_commands:
- dotnet run --project backend/tests/AiDaily.UnitTests/AiDaily.UnitTests.csproj
- npm test
- npm run build

如果 npm 指令在本機工具層無法執行，必須回報未驗證前端，不可宣稱完成。
```

#### 2. P1a article/feed persistence baseline

```text
請遵守根目錄 AGENTS.md，根據 docs/slices/ai-daily-slices.md 的 P1a，實作 Article/feed persistence baseline。

只做 P1a，不要同時改 SSE contract、rate limit、AI summary/report persistence、bookmark/hidden persistence、auth UI 或 CI/CD。

開始前先回報 git_flow_classification，預設：
- base_branch: develop
- branch_type: feature
- suggested_branch_name: feature/p1a-article-feed-persistence

成功標準：
- EF Core AiDailyDbContext 不再只是 placeholder。
- Article、FeedSource 或等價 feed metadata 有 PostgreSQL mapping。
- Article source identity 或 SourceUrl 有 unique constraint，避免 RSS sync 重複寫入。
- Content status、content text、contentExtractedAt、ingestionScore、matchedKeywords、sourceQualityTier 有可保存欄位或明確 deferral。
- API 可用設定選擇 DB article/feed repository；in-memory adapter 保留給 tests/dev fallback。
- API 重啟後仍可讀到 feed articles 與 feed metadata。
- 補 article/feed repository persistence tests 或 lightweight integration tests。

validation_commands:
- dotnet run --project backend/tests/AiDaily.UnitTests/AiDaily.UnitTests.csproj
- dotnet build backend/AiDaily.sln

如果 build 因既有 obj/bin 檔案鎖定失敗，必須回報鎖定錯誤與未完成的驗證，不要清除檔案或 reset。
```

#### 3. S3-1 quick summary state sync

```text
請遵守根目錄 AGENTS.md，根據 docs/slices/ai-daily-slices.md 的 S3-1，修正 AI quick summary state sync 與 cache correctness。

只做 S3-1，不要同時做 deep report SSE、PostgreSQL persistence baseline、Redis adapter 或 feed crawler。

開始前先回報 git_flow_classification，預設：
- base_branch: develop
- branch_type: feature
- suggested_branch_name: feature/s3-1-summary-state-sync

成功標準：
- 生成 summary 後，article list/detail 的 hasAiSummary 與 Dashboard AI Briefs count 能反映新狀態。
- quick summary generation 有並行防重或最小 tracker。
- provider failure 有 domain-level error mapping，不只落成 500。
- forced regeneration 成功保存後才替換舊 cache。
- 補 AiSummaryPanel/store/service tests，覆蓋 empty、generate、error、refresh。

validation_commands:
- dotnet run --project backend/tests/AiDaily.UnitTests/AiDaily.UnitTests.csproj
- npm test

如果 npm 指令在本機工具層無法執行，必須回報未驗證前端。
```

#### 4. S2-3b feed relevance gate and ranking hardening

```text
請遵守根目錄 AGENTS.md，根據 docs/slices/ai-daily-slices.md 的 S2-3b，修正 Feed relevance gate and ranking hardening。

只做 S2-3b，不要同時做 rejected candidate audit persistence、admin/debug UI、LLM ranking、大規模 source expansion、persistence baseline 或 cursor hardening。

開始前先回報 git_flow_classification，預設：
- base_branch: develop
- branch_type: feature
- suggested_branch_name: feature/s2-3b-feed-relevance-ranking

成功標準：
- AI relevance 判斷主要依 title、summary、canonical URL 或明確 allowlist topic；source.Name/source.TopicScope 不得讓候選在內容無 AI 訊號時自動通過。
- Generic `ai` only、過短摘要、活動/公告/列表型低訊號候選需被 deterministic rule 拒絕或明確降分。
- `ingestionScore` 語意能區分高訊號與勉強相關候選；低於 threshold 的候選不進一般 reader feed。
- `GET /api/v1/articles` 預設排序有明確 quality-vs-recency 規則，避免低分新文章無條件壓過高分稍早文章。
- 補測試：source name/topic 含 AI，但 title/summary/sourceUrl 無 AI relevance 時應 rejected。
- 補測試：高分稍早文章不應被低分新文章無條件壓過；測試需反映實作採用的排序規則。
- 保留既有 rejected article 排除、candidateLimit 掃描、accepted article ingestion metadata 測試。

validation_commands:
- dotnet run --project backend/tests/AiDaily.UnitTests/AiDaily.UnitTests.csproj
```

#### 5. S2-3a rejected metadata decision

```text
請遵守根目錄 AGENTS.md，根據 docs/slices/ai-daily-slices.md 的 S2-3a 與 docs/feed-source-policy.md，處理 rejected candidate metadata decision。

只做 S2-3a 的 rejected candidate 決策，不要重做 broader feed quality rules、candidateLimit、sorting、整個 persistence baseline 或 admin UI。

開始前先回報 git_flow_classification，預設：
- base_branch: develop
- branch_type: feature
- suggested_branch_name: feature/s2-3a-rejected-metadata-decision

成功標準：
- 明確決定 rejected candidates 要保存 audit metadata，或文件化 logs-only MVP deferral。
- 若保存：新增 rejected candidate persistence / query test。
- 若 deferral：更新 feed-source-policy 與 slice 文件，說明目前不能做長期 source quality analysis。
- 一般 reader feed 仍排除 rejected articles。

validation_commands:
- dotnet run --project backend/tests/AiDaily.UnitTests/AiDaily.UnitTests.csproj
```

#### 6. S2-4 source/content quality upgrade

```text
請遵守根目錄 AGENTS.md，根據 docs/slices/ai-daily-slices.md 的 S2-4，改善 AINEWS 的 source/content quality。

只做 S2-4，不要同時做 rejected audit persistence、admin/debug UI、LLM ranking、個人化推薦、完整 search/news API、cursor hardening 或大規模來源擴張。

開始前先回報 git_flow_classification，預設：
- base_branch: main
- branch_type: feature
- suggested_branch_name: feature/s2-4-source-content-quality

成功標準：
- Seed feed sources 改成少而精：優先官方 AI lab/product/research/policy/engineering feeds 與少量高品質 AI topic feeds；移除或降級明顯低訊號 aggregator/newsletter/watch source。
- Source tier threshold 更硬：`core`、`standard`、`watch` 或等價 tier 對 accepted score 有不同門檻；watch/aggregator 不得只因 generic AI signal 進一般 reader feed。
- HTML content extraction 不再把過短正文、navigation/cookie/sidebar/related-posts 雜訊或 RSS summary fallback 標成 `full_content_ready`。
- 新增 deterministic content quality gate，例如 minimum clean text length、clean text 與 fallback summary 的區分、noise phrase rejection 或 content/source ratio；低品質正文需標成 `summary_fallback` 或 `extraction_failed`。
- AI summary/report input 只能把 `full_content_ready` 視為完整正文；fallback 狀態不得宣稱已讀完整原文。
- 補測試：低品質 HTML 不應標成 full content；可讀正文應保留 main article text 並移除 nav/sidebar/cookie 類雜訊；watch source 低訊號候選應被 rejected 或低於 reader threshold。
- 保留 S2-3b relevance/ranking 測試、S2-3a rejected deferral 文件語意、既有 content extraction fallback 測試。

validation_commands:
- dotnet run --project backend/tests/AiDaily.UnitTests/AiDaily.UnitTests.csproj

如果 build 因本機 API/Visual Studio 鎖定 bin 檔失敗，可改用隔離 output，例如加上 `--property:BaseOutputPath=<repo>/.tmp/s2-4-test-output/`，並在完成後清理 `.tmp`。
```

#### 7. S1 reader list cursor hardening

```text
請遵守根目錄 AGENTS.md，根據 docs/slices/ai-daily-slices.md 的 S1 current gaps，修正 reader list cursor pagination。

只做 article list cursor hardening，不要同時做 persistence baseline、feed crawler 或 AI summary。

開始前先回報 git_flow_classification，預設：
- base_branch: develop
- branch_type: feature
- suggested_branch_name: feature/s1-article-list-cursor-hardening

成功標準：
- cursor 從 base64 offset index 改成穩定 keyset cursor，例如 publishedAt、ingestionScore、id。
- hidden/filter/new article 變動下不漏資料、不重複資料。
- API contract 文件和 tests 反映新 cursor shape。
- 補後端 query tests；若前端需要調整，補 store tests。

prerequisite:
- 若 ingestionScore 尚未持久化，仍可使用目前查詢可取得的 stable ordering 欄位；不要因此擴大到 persistence baseline。

validation_commands:
- dotnet run --project backend/tests/AiDaily.UnitTests/AiDaily.UnitTests.csproj
- npm test
```

### Do Not Start From These Yet

- 不要把 `S1-1` 當下一個從零 implementation slice；stats API 和 Dashboard stats row 已部分完成。它現在適合做驗收、補測與 `hasAiSummary` source-of-truth 修正。
- 不要直接做 CI/CD；目前 persistence、rate limit、SSE contract 還不穩，CI/CD 會先把不穩的 contract 固化。
- 不要一次做 S4-1 + P1a + S3-1。這些會互相碰到 AI/report/summary 狀態，但 owner slice 不同，應分開 commit。

### CI/CD Readiness Gate

不要把 CI/CD 當下一個 slice。重新啟動 CI/CD 學習或 pipeline 建置前，至少要有：

- `S4-1` 完成，AI report SSE/rate-limit/provider error contract 有測試。
- `P1a` 完成，Article/feed persistence baseline 有測試。
- `S3-1` 完成，quick summary state sync/cache correctness 有測試。
- `git log` 中至少有上述 docs/feature/test commits，可讓 AI 從 commit history 學到「rebaseline -> contract hardening -> persistence -> tests」的順序。

### Evidence / Verification Index

| Area | 主要證據 | 最窄驗證 |
| --- | --- | --- |
| S4-1 report contract | `backend/src/AiDaily.Application/AiSummaries`、`backend/src/AiDaily.Infrastructure/AI`、`frontend/src/composables/useAiReportStream.ts` | `dotnet run --project backend/tests/AiDaily.UnitTests/AiDaily.UnitTests.csproj`、`npm test` |
| P1a article/feed persistence | `backend/src/AiDaily.Infrastructure/Persistence`、`backend/src/AiDaily.Infrastructure/Repositories`、`backend/src/AiDaily.Domain/Entities` | `dotnet run --project backend/tests/AiDaily.UnitTests/AiDaily.UnitTests.csproj`、`dotnet build backend/AiDaily.sln` |
| S3-1 summary state sync | `backend/src/AiDaily.Application/AiSummaries`、`frontend/src/components/ai/AiSummaryPanel.vue`、`frontend/src/stores/aiSummaryStore.ts` | `dotnet run --project backend/tests/AiDaily.UnitTests/AiDaily.UnitTests.csproj`、`npm test` |
| S2-3b feed relevance/ranking | `backend/src/AiDaily.Infrastructure/FeedCrawler/FeedArticleQualityFilter.cs`、`backend/src/AiDaily.Application/Articles/ArticleQueryService.cs`、`backend/tests/AiDaily.UnitTests` | `dotnet run --project backend/tests/AiDaily.UnitTests/AiDaily.UnitTests.csproj` |
| S2-3a rejected metadata | `backend/src/AiDaily.Infrastructure/FeedCrawler`、`docs/feed-source-policy.md` | `dotnet run --project backend/tests/AiDaily.UnitTests/AiDaily.UnitTests.csproj` |
| S2-4 source/content quality | `backend/src/AiDaily.Infrastructure/FeedCrawler/SeedFeedSources.cs`、`backend/src/AiDaily.Infrastructure/FeedCrawler/FeedArticleQualityFilter.cs`、`backend/src/AiDaily.Infrastructure/ContentExtraction`、`backend/src/AiDaily.Infrastructure/AI`、`backend/tests/AiDaily.UnitTests` | `dotnet run --project backend/tests/AiDaily.UnitTests/AiDaily.UnitTests.csproj` |
| S1 cursor hardening | `backend/src/AiDaily.Application/Articles`、`frontend/src/stores/articleStore.ts` | `dotnet run --project backend/tests/AiDaily.UnitTests/AiDaily.UnitTests.csproj`、`npm test` |

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

#### Current implementation

- 已有 `GET /api/v1/articles`、Dashboard feed、filter、article cards 與 load-more store flow。
- 目前 article repository 是 in-memory seed/upsert，尚未接 PostgreSQL。
- 目前 cursor 是 base64 offset index；資料新增、hidden filter 或排序變動時可能漏看或重複。

#### Known gaps

- 需要把正式 cursor contract 改成 keyset cursor，例如 `(publishedAt, ingestionScore, id)`。
- 需要把 reader list 的 HTTP contract 補進 integration tests，而不是只測 service/query。
- 需要避免 bookmark/hide 等 mutation error 污染 article list fatal error state。

#### Next actions

- 在 persistence baseline 前，先文件化目前 pagination 是 offset MVP。
- 在後續 article list hardening slice 中改成 keyset cursor，並補 hidden/filter 變動下的 pagination 測試。

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

#### Current implementation

- 已有 `GET /api/v1/articles/{id}`、`Report.vue`、RSS crawler、feed crawl run service、content extraction 與 cold-start sync UX。
- Crawler 會對 accepted articles 保存 ingestion metadata；rejected candidates 目前只寫入 logs，沒有保存 audit record。
- Reader list 會排除 rejected articles 與 hidden articles；reader detail/report direct access policy 尚未定義。

#### Known gaps

- RSS imported articles、content extraction result 與 feed source state 仍無 DB persistence。
- Rejected candidates 無法被 debug/admin review，也無法長期調校 source quality。
- Article detail 對 hidden/rejected article 應回 404、顯示 hidden state，或提供 admin/debug route，需在後續 slice 決策。

#### Next actions

- 在 S2-3 決定 rejected candidate 是「logs-only MVP」還是「保存 rejected audit metadata」。
- 在 persistence baseline 中納入 article、feed source、content extraction metadata 的資料表與 unique constraints。
- 在 reader detail hardening 中補 hidden/rejected direct access behavior 與測試。

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

#### Current implementation

- 已有 quick summary read/generate endpoint、`AiSummaryPanel.vue`、`aiSummaryStore`、in-memory summary repository 與 in-memory read cache。
- 生成結果會保存 provider、promptVersion、generatedAt，並使用 `ai-summary:{articleId}` logical cache key。
- `ArticleDto.hasAiSummary` 仍來自 article projection，不會因 summary generation 自動同步。

#### Known gaps

- Summary persistence/cache 仍是 volatile MVP。
- Quick summary generation 缺並行防重、provider error mapping 與 rate limit。
- Dashboard AI brief count 與 article card badge 可能在生成後仍顯示舊狀態。

#### Next actions

- 在 S3-1 或 persistence baseline 中，讓 article query 能根據 summary repository 判斷 `hasAiSummary`。
- 為 quick summary generation 加 tracker/rate limit/error contract，避免 provider failure 只落成 500。
- 補 `AiSummaryPanel` component tests，覆蓋 empty/generate/error 與 generated summary refresh。

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

#### Current implementation

- 已有 `GET /api/v1/articles/{id}/ai-report`。
- 已有 `POST /api/v1/articles/{id}/ai-summary/generate` 串流 report generation。
- 目前 provider 可走 Gemini 或 Stub；spec target 仍是 Anthropic/Claude，偏差已文件化在 `AI-Daily-Spec.md`。
- 目前 MVP SSE events 是已版本化的 `started/status/report/completed/error`。
- 後端已有 draft normalizer/validator、per-user/per-article rate limiter、SSE wire-level tests 與前端 parser tests。

#### Known gaps

- SSE event shape 與 spec `start/chunk/field_done/done/error` 不一致，但 S4-1 已選擇版本化 MVP shape，後續改 shape 需另行決策。
- Prompt 目前使用 deterministic character budget；尚未補 token estimator 或 truncation metadata。
- 正式 auth/JWT 尚未完成，rate limit identity 仍是 local-user header。

#### Next actions

- 若要改成 spec shape 或 Claude provider，先請使用者決策。
- 在正式 auth slice 中把 local-user rate-limit identity 升級為 authenticated user identity。
- 後續可在 prompt builder 補 token estimator 與 truncation metadata。

## Spec Comparison Summary

- planning_mode: `apply_to_existing_plan`
- reference_plan_usage: 根據 `AI-Daily-Spec.md` 推導 reference coverage，但 reference plan 不保留；只把差異整理回本 artifact。
- existing_plan_summary: 目前 S1-S5-1 都已有不同程度的 MVP 基底；S4 是主要 contract risk，P1a 是下一個資料生命週期缺口。
- matched_coverage:
  - S1 已覆蓋文章列表、filter、pagination 與 Dashboard UI。
  - S1-1 stats API 與 Dashboard stats row 已部分完成。
  - S2 已部分完成 article detail、Report UI、RSS crawler、content extraction 與 cold-start sync。
  - S3 已部分完成 quick summary read/generate/cache flow。
  - S4 已部分完成 AI deep report generation、Gemini/Stub provider、MVP SSE 與 provider interface。
  - S5/S5-1 已部分完成 bookmark、theme、local-user hidden articles 與 settings/undo UI。
- missing_or_underdefined:
  - 全域缺 persistence baseline：Article、AiSummary、AiReport、Bookmark、HiddenArticle 與 feed metadata 仍是 in-memory；recommended_action: `add_slice` -> `P1 umbrella`，並先執行 `P1a`。
  - S1 cursor 仍是 offset MVP，不符合穩定 cursor acceptance；recommended_action: `add_acceptance` -> `S1` 或後續 article list hardening。
  - S1-1 stats API 已存在，但 AI brief count 依賴 article flag，生成 summary 後可能不同步；recommended_action: `add_acceptance` -> `S1-1` / `S3-1`。
  - S2 rejected candidates 只 log 不保存，與 feed-source policy 的 rejected metadata 方向不一致；recommended_action: `add_acceptance` -> `S2-3`。
  - S2 reader detail/report 對 hidden/rejected direct access policy 未定；recommended_action: `add_acceptance` -> `S2` / `S5-1`。
  - S3 quick summary generation 缺並行防重、provider error mapping 與 generated summary 對 article projection 的同步；recommended_action: `add_acceptance` -> `S3-1`。
  - S4 缺 provider/spec deviation 文件化、SSE MVP contract 版本化、rate limit、prompt content length limit、secret-safe provider errors；recommended_action: `add_slice` -> `S4-1`。
  - 前端缺 SSE parser/race/error state component tests；recommended_action: `add_acceptance` -> `S4-1` 與前端 hardening。
- intentional_deviation:
  - MVP 可用 Gemini/Stub 作 provider，但 spec target 是 Anthropic/Claude；document_or_fix: `document MVP deviation in S4-1, keep interface stable for later fix`。
  - MVP SSE 預設版本化 `started/status/report/completed/error`；若要改成 spec target `start/chunk/field_done/done/error`，需使用者明確決策。
  - MVP persistence/cache 可暫用 in-memory adapter，但文件與狀態表必須標成 volatile MVP；document_or_fix: `P1 must define the PostgreSQL persistence baseline before production claims`。

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

#### Current implementation

- 已有 `GET /api/v1/stats/today`、Dashboard stats row、tag/source breakdown 與 sync status。
- Stats query 目前依 `Article.HasAiSummary` 計算已總結數。

#### Known gaps

- `HasAiSummary` 不是從 summary repository 動態投影；quick summary 生成後 stats 可能仍顯示舊值。
- 缺 controller/HTTP contract tests 與 Dashboard component-level loading/error tests。

#### Next actions

- 將 S1-1 從下一個 implementation slice 改為「驗收/補測/狀態修正」項目。
- 在 S3-1 或 persistence baseline 中修正 AI brief count 的 source of truth。

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

#### Current implementation

- 已有 HTML content extraction，會移除 script/style/navigation 並 fallback 到 RSS summary。
- Report page 已顯示 content status 與 Source Content preview。

#### Known gaps

- Clean text 與 AI prompt input 缺長度/token budget 上限。
- Content extraction result 尚未持久化到 PostgreSQL。
- 目前 HTML extraction 仍是簡化版清理，不是 readability-grade extractor；可能把 navigation、cookie、related posts、短摘要或低密度正文誤判為可分析內容。

#### Next actions

- 加入 deterministic truncation，並記錄內容是否被截斷。
- 在 persistence baseline 中保存 content status、content text、extractedAt 與 source fallback metadata。
- 另開 S2-4 處理 source/content quality gate，避免爛正文或低訊號來源進入 AI summary/report 的主要輸入。

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

#### Current implementation

- `GET /api/v1/articles` 已保持 read-only。
- 已有 `POST /api/v1/feed-crawl/run?scope=today` 與 Dashboard cold-start/stale/sync-failed states。

#### Known gaps

- Feed crawl 寫入入口尚未有 auth/admin guard 或 rate limit。
- Feed crawl result 仍寫入 in-memory article repository。

#### Next actions

- 另開 API write guard follow-up 保護 feed crawl POST，至少限制 production 下任意觸發；不要併入 S4-1。
- 在 P1a 後，讓 explicit sync 寫入 DB 並保留 crawl run metadata。

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

#### Current implementation

- 已有 deterministic quality filter、candidateLimit 掃描與 accepted article ingestion metadata。
- S2-3b 已修正 quality filter，不再讓 `source.Name` 與 `source.TopicScope` 在候選內容無 AI 訊號時自動通過 relevance gate。
- S2-3b 已讓 `GET /api/v1/articles` 採用同日內 quality first、再 recency 的預設排序，避免低分新文章無條件壓過高分稍早文章。
- Seed source 實作目前只包含少量固定來源，且有 newsletter / aggregator 類 `watch` source；尚未回補 spec 中較高訊號的官方 blog、研究、政策與主流 AI topic feed。
- Rejected candidates 目前只寫入 crawler logs，沒有保存 `rejectionReason` 到 article 或 audit table。

#### Known gaps

- `docs/feed-source-policy.md` 期望保留 rejected metadata；目前 runtime 是 logs-only MVP。
- 沒有 admin/debug view 或 rejected candidate audit store 可用於調校來源。
- Rejected candidates 的長期 audit metadata 尚未保存；目前無法做 rejection-rate trend、rejected reason breakdown 或長期 source quality analysis。

#### Next actions

- S2-3a 決策：本階段將 rejected candidate metadata 明確文件化為 logs-only MVP deferral，不新增 rejected audit persistence 或 admin/debug UI。
- 後續若要做長期 source quality analysis，需另開 rejected audit persistence slice，定義 audit store、query surface、retention 與 reader/admin visibility 邊界。

#### S2-3a. Rejected candidate metadata decision

S2-3 是較大的 feed quality umbrella；下一個可執行工作只做 S2-3a。S2-3a 的範圍是決定 rejected candidates 是否保存 audit metadata，或明確把目前 runtime 降級為 logs-only MVP。不要在這一輪重做 candidate limit、sorting、source selection、admin UI 或整個 persistence baseline。

Decision: S2-3a 採用 logs-only MVP deferral。Crawler 仍會在 run logs 記錄 rejected candidates，但不新增 rejected candidate audit table、query endpoint、admin/debug UI 或長期保存機制。這代表目前不能做長期 rejected-candidate analysis、source rejection-rate trend 或 rejected reason breakdown；source quality 調校只能依 accepted article ingestion metadata、當次 crawler logs 與人工觀察。一般 reader feed 仍必須排除 `rejectionReason` 非空的 articles，並由既有 unit test 覆蓋。

#### S2-3b. Feed relevance gate and ranking hardening

S2-3b 是針對目前 feed 低價值感的最小可執行修正；它只處理 deterministic relevance gate、quality threshold 與一般 reader feed 排序，不處理 rejected audit persistence、admin UI、LLM ranking 或大規模 source expansion。

- owner_scope: RSS candidate relevance、ingestion score semantics、reader list ordering。
- suggested_branch_name: `feature/s2-3b-feed-relevance-ranking`
- user_flow: 使用者打開 Dashboard 時，預設 feed 優先顯示高訊號 AI 文章；來源名稱含 AI 但文章內容低訊號的候選不應只是因 source metadata 而進入 feed。
- files_or_areas:
  - `backend/src/AiDaily.Infrastructure/FeedCrawler/FeedArticleQualityFilter.cs`
  - `backend/src/AiDaily.Infrastructure/FeedCrawler/RssFeedCrawler.cs`
  - `backend/src/AiDaily.Application/Articles/ArticleQueryService.cs`
  - `backend/tests/AiDaily.UnitTests`
  - `docs/feed-source-policy.md`
- acceptance:
  - AI relevance 判斷主要依 title、summary、canonical URL 或明確 allowlist topic；`source.Name` 與 `source.TopicScope` 不得讓候選文章在內容無 AI 訊號時自動通過。
  - Generic `ai` only、過短摘要、活動/公告/列表型低訊號候選需被拒絕或明確降分；門檻與原因必須是 deterministic rule，不交給模型判斷。
  - `ingestionScore` 的語意需能區分高訊號與勉強相關候選；若分數低於 threshold，候選不應進入一般 reader feed。
  - `GET /api/v1/articles` 的預設排序需明確定義 quality-vs-recency 規則，例如同日內 quality first，或用可測試的 composite ranking 避免低分新文章無條件壓過高分文章。
  - 新增測試：source name/topic 含 AI，但 title/summary/sourceUrl 無 AI relevance 時，candidate 應被 rejected。
  - 新增測試：高分稍早文章不應被低分新文章無條件壓過；測試需反映實作採用的 quality-vs-recency 規則。
  - 保留現有 rejected article 不出現在一般列表、candidateLimit 會繼續掃描後續候選、accepted article 保存 ingestion metadata 的測試。
- not_included:
  - Rejected candidate audit persistence；仍由 S2-3a 決定。
  - Admin/debug source quality view。
  - LLM-based relevance ranking 或摘要品質判斷。
  - 付費 search/news API。
  - 大規模新增或替換 feed sources；source expansion 可在 gate 穩定後另開 follow-up。
- reason_first: 目前文章低價值的核心不是缺少 AI 產生能力，而是 deterministic gate 太容易被 source metadata 放行，且 reader feed 排序過度偏向新鮮度；先修 gate 與排序，才能避免後續 summary/report 把成本花在低訊號文章上。

### S2-4. Source/content quality upgrade

- backfills: `S2. 文章詳情與來源匯入`、`S3. AI 摘要預覽`、`S4. Deep AI report`
- source_gap: S2-3 已讓低訊號 candidate 較難進 feed，但目前 sources 仍偏少且混入 aggregator/newsletter/watch 類低訊號來源；HTML extraction 也仍可能把短摘要、導覽文字、cookie/subscribe/related-posts 等雜訊標成 `full_content_ready`，導致 AI summary/report 拿到的實際內容仍很爛。
- why_now: AINEWS 的價值不該只是「抓到 AI 相關 RSS」，而是餵給使用者與 AI 生成流程值得分析的文章。若 source 與 content quality 不升級，後續 prompt、summary 或 report 調校只是在包裝低品質輸入。
- git_flow_classification:
  ```yaml
  base_branch: main
  branch_type: feature
  suggested_branch_name: feature/s2-4-source-content-quality
  reason: "Improves source selection, source tier thresholds, and extracted content quality gates after S2-3 relevance/ranking hardening."
  ```
- user_flow: 使用者打開 Dashboard 或產生 AI summary/report 時，看到的是高訊號來源、可讀正文或明確 fallback 的文章；系統不再把短摘要、RSS 片段或頁面雜訊偽裝成完整原文。
- files_or_areas:
  - `backend/src/AiDaily.Infrastructure/FeedCrawler/SeedFeedSources.cs`
  - `backend/src/AiDaily.Infrastructure/FeedCrawler/FeedArticleQualityFilter.cs`
  - `backend/src/AiDaily.Infrastructure/FeedCrawler/RssFeedCrawler.cs`
  - `backend/src/AiDaily.Infrastructure/ContentExtraction`
  - `backend/src/AiDaily.Infrastructure/AI`
  - `backend/tests/AiDaily.UnitTests`
  - `docs/feed-source-policy.md`
- acceptance:
  - Seed feed sources 採「少而精」策略：優先官方 AI lab/product/research/policy/engineering feeds 與少量高品質 AI topic feeds；明顯低訊號 aggregator/newsletter/watch source 要移除、停用或降低 tier。
  - Source tier threshold 有明確 deterministic 規則；`watch` 或 aggregator 類來源必須有更高分數或更強 title/summary/sourceUrl 訊號才能進一般 reader feed。
  - Content extraction 必須有 deterministic quality gate，例如 minimum clean text length、noise phrase rejection、clean text 與 fallback summary 區分、或正文密度檢查。
  - 過短正文、RSS summary fallback、cookie/subscribe/related-posts/navigation/sidebar 類雜訊不得標成 `full_content_ready`。
  - AI summary/report prompt input 只在 `contentStatus == full_content_ready` 時宣稱使用完整原文；`summary_fallback` 或 `extraction_failed` 必須維持保守文案與 fallback behavior。
  - 補測試：低品質 HTML 不應標成 full content；可讀 HTML fixture 應保留正文並移除 nav/sidebar/cookie 雜訊；watch source 低訊號候選應被 rejected 或低於 reader threshold。
  - 保留 S2-3b relevance/ranking 測試、S2-3a logs-only deferral 文件語意、既有 extraction failure fallback 測試。
- not_included:
  - Rejected candidate audit persistence 或 rejected admin/debug UI。
  - LLM-based ranking、LLM source scoring 或個人化推薦。
  - 大規模 source expansion；本 slice 只做小規模高訊號來源替換/補強與低訊號來源降級。
  - 付費 search/news API、browser-based scraping、反 bot 繞過或需要 JavaScript 執行的來源。
  - PostgreSQL persistence baseline、cursor hardening、bookmark/hidden personalization。
- reason_first: S2-3 解決「低訊號 candidate 太容易進 feed」；S2-4 解決「進來的來源與正文仍不值得分析」。先建立 source/content quality gate，AINEWS 才能從 RSS reader 變成可信的 AI news signal pipeline。

#### Current implementation

- S2-3b 已有 deterministic relevance gate、quality threshold 與同日內 quality-first reader sorting。
- `HtmlArticleContentExtractor` 可移除部分 script/style/navigation 並 fallback 到 RSS summary。
- AI summary/report 已能根據 content status 避免宣稱 fallback summary 是完整原文。

#### Known gaps

- Seed sources 仍偏少，且 watch/aggregator/newsletter 類來源可能讓產品體感偏低價值。
- Content extraction 尚未有正文長度、正文密度、noise phrase 或 summary-vs-full-content 的品質門檻。
- `full_content_ready` 目前可能代表「HTML 清掉 tag 後有文字」，不保證是可分析的主文。
- 目前沒有 content quality score、extraction quality reason 或足夠測試 fixture 來區分可讀正文與頁面雜訊。

#### Next actions

- 先做小規模高訊號 source catalog 調整，不追求大量來源。
- 加入 content quality gate，讓低品質抽文降級為 `summary_fallback` 或 `extraction_failed`。
- 補 extraction fixture tests 與 watch source threshold tests。

### S3-1. AI quick summary state sync 與 cache correctness

- backfills: `S3. AI 摘要預覽`
- source_gap: Spec 要右側 quick summary 由 AI 產生並永久快取；目前 S3 已有 read/generate/cache，但生成後 article projection、Dashboard stats 與 cache replacement 語意仍可能不同步。
- why_now: S4 已接 provider 後，quick summary 不應讓 Dashboard、article detail 與 cache 各自判斷狀態，否則使用者會看到「已生成但列表仍顯示沒有 summary」的分裂狀態。
- git_flow_classification:
  ```yaml
  base_branch: develop
  branch_type: feature
  suggested_branch_name: feature/s3-1-summary-state-sync
  reason: "Hardens quick summary state synchronization and cache replacement behavior; this is planned feature/backfill work."
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
  - 生成 summary 後，article list/detail 的 `hasAiSummary` 與 Dashboard AI Briefs count 能反映新狀態。
  - 未 forced regeneration 時，同一篇文章不重複呼叫 provider。
  - `force=true` 成功生成並保存後才替換舊 cache；provider failure 不應讓舊 summary 短暫消失。
  - cached reads 需避免不必要的 repository/provider calls；cache key 與失效條件需文件化。
  - 若 article content 尚未完成，生成可 fallback summary/source metadata，並在 prompt/結果中避免宣稱看過完整原文。
  - provider failure 有 domain-level error mapping，不只落成 500。
- not_included:
  - Deep report streaming。
  - PostgreSQL persistence baseline；`AiSummary` DB mapping 屬於 P1 的後續子切片，不放進 S3-1。
  - Redis production cache adapter。
  - Prompt A/B testing UI。
  - 多 provider routing UI。
- reason_first: S3 是 S4 的輕量 AI 入口；先把狀態同步與 cache correctness 補齊，下一步 P1 才能有清楚的資料語意可持久化。

#### Current implementation

- 已有 quick summary read/generate/cache behavior，且 cache policy 已記錄 `ai-summary:{articleId}` logical key。
- Generate path 會重用 existing summary，`force=true` 會重新生成。

#### Known gaps

- `force=true` 會先移除 cache；provider failure 時舊 summary 可能短暫不可用。
- 缺 quick-summary generation tracker，並行 missing/force requests 可能重複打 provider。
- 生成後 article projection 與 Dashboard stats 仍可能不知道 summary 已存在。

#### Next actions

- 加入 quick summary generation tracker 與 provider error mapping。
- 調整 cache invalidation：新 summary 成功保存後再替換舊 cache。
- 在 query/projection 層讓 article list/detail 能反映 generated summary availability。

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
  - SSE event shape 預設將 MVP `started/status/report/completed/error` 文件化為版本化 contract；若要改成 spec `start/chunk/field_done/done/error`，需先請使用者決策。
  - AI generation 有最小 rate limiting；超過限制回 `AI_RATE_LIMIT_EXCEEDED` 或等價文件化錯誤。
  - 在 S2-1 未完成前，S4 只能宣稱使用 summary/source metadata fallback，不宣稱 full-content deep report。
- not_included:
  - Prompt A/B testing UI。
  - Exporting reports。
  - Feed crawl POST guard；另開 API write guard follow-up。
  - 完整 Anthropic/OpenAI/provider switching UI。
- reason_first: S4 是成本、安全、contract 漂移最容易放大的地方；這個回補項讓已開始的 provider 串接可被後續 slice 穩定承接。

#### Current implementation

- 已有 report query/generation service、Gemini provider adapter、Stub provider、in-progress tracker、per-user/per-article rate limiter、schema normalizer/validator 與版本化 MVP SSE stream。
- API key 由後端設定讀取；前端不直接接觸 provider key。
- `AI-Daily-Spec.md` 已文件化 S4-1 的 provider deviation、`started/status/report/completed/error` SSE contract、`AI_RATE_LIMIT_EXCEEDED` 429 contract 與 secret-safe provider error 邊界。

#### Known gaps

- 正式 auth/JWT guard 尚未完成；目前只用 local-user header 做 rate-limit identity，feed crawl POST guard 屬於獨立 API write guard follow-up。
- Spec target 仍是 Anthropic/Claude 與 `start/chunk/field_done/done/error`，目前作為已文件化 MVP deviation 保留。
- Prompt content 已有 deterministic character budget，但尚未加入 token estimator 或 truncation metadata。

#### Next actions

- 後續若要切回 spec SSE shape 或 Claude provider，需先由使用者明確決策。
- 在正式 auth slice 中把 local-user rate-limit identity 升級為 authenticated user identity。

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

#### Current implementation

- 已有 bookmark add/delete/list、bookmark UI、theme preference 與 temporary local-user strategy。
- Bookmark state 目前由 header `X-AI-Daily-Local-User` 區分使用者。

#### Known gaps

- Bookmark repository 是 in-memory；API 重啟後收藏會消失。
- 尚未有正式 auth；local-user id 產生與瀏覽器 storage 例外保護需要補強。
- Frontend mutation error 目前會寫入全域 article error state，可能干擾 Dashboard/Report 主畫面。

#### Next actions

- 在 persistence baseline 中加入 Bookmark table 與 `(userId, articleId)` unique constraint。
- 拆分 frontend list/detail/mutation errors，讓 bookmark failure 走 toast 或 inline action error。
- 在正式 auth 前，文件化 local-user strategy 的開發用途與限制。

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

#### Current implementation

- 已有 Article card/report `Not interested` action、optimistic removal、undo、hidden list 與 restore。
- `GET /api/v1/articles` 會依 local-user hidden preference 排除 hidden articles。

#### Known gaps

- HiddenArticle repository 是 in-memory；API 重啟後負回饋會消失。
- Article detail/report direct link 對 hidden article 的行為未定。
- Offset cursor 可能在 hidden item 變動後造成漏頁或重複。

#### Next actions

- 在 persistence baseline 中加入 HiddenArticle table 與 `(userId, articleId)` unique constraint。
- 決定 hidden detail/report direct access policy：404、hidden-state page，或 admin/debug bypass。
- 在 keyset cursor 修正前，文件化 hidden + pagination 仍是 MVP risk。

### P1. Persistence baseline umbrella

- status: planned
- backfills: `S1`、`S2`、`S3`、`S4`、`S5`、`S5-1`
- source_gap: `docker-compose.yml` 已提供 PostgreSQL/Redis，但 API runtime 仍註冊 in-memory repositories/cache，`AiDailyDbContext` 仍是 placeholder。
- why_now: 目前多個 slice 已可 demo，但 feed imports、bookmarks、hidden articles、AI summaries 與 AI reports 都會在 API 重啟後消失；若不先建立 persistence baseline，後續 CI/CD 與 production readiness 都缺真實資料生命週期。
- git_flow_classification:
  ```yaml
  base_branch: develop
  branch_type: feature
  suggested_branch_name: feature/p1-persistence-baseline
  reason: "Adds PostgreSQL-backed persistence for existing MVP reader and AI state; this is planned infrastructure hardening, not a production hotfix."
  ```
- user_flow: 使用者同步 feed、收藏文章、隱藏文章、產生 AI summary/report 後，重啟 API 仍能讀到同一批資料與個人化狀態。
- files_or_areas:
  - `backend/src/AiDaily.Infrastructure/Persistence/AiDailyDbContext.cs`
  - `backend/src/AiDaily.Infrastructure/Persistence/Configurations`
  - `backend/src/AiDaily.Infrastructure/Migrations`
  - `backend/src/AiDaily.Infrastructure/Repositories`
  - `backend/src/AiDaily.Infrastructure/AI`
  - `backend/src/AiDaily.API/Program.cs`
  - `docker-compose.yml`
  - `backend/tests/AiDaily.UnitTests`
- acceptance:
  - Article、AiSummary、AiReport、Bookmark、HiddenArticle、FeedSource 或等價 feed metadata 有 EF Core mapping。
  - `Article.SourceUrl` 或 stable source identity 有 unique constraint，避免 RSS sync 重複寫入。
  - AiSummary 與 AiReport 以 `articleId` 做唯一 upsert。
  - Bookmark 與 HiddenArticle 使用 `(userId, articleId)` unique constraint。
  - API 可透過設定選擇 DB repository；in-memory adapter 保留給 tests/dev fallback。
  - 重啟 API 後，feed articles、bookmarks、hidden preferences、AI summaries/reports 仍可讀取。
  - 至少包含 repository persistence tests 或 lightweight integration tests，驗證 upsert、unique constraints 與重啟後讀取語意。
- not_included:
  - 完整正式 auth。
  - Redis production cache adapter。
  - Admin feed-source management UI。
  - Cloud deployment / CI/CD pipeline。
- reason_first: 這不是新增產品功能，而是把已存在 MVP flows 從 volatile demo 推進到可被後續 slice、測試與 CI/CD 信任的資料基線。

P1 是 umbrella，不適合作為單次 build-and-learn-loop 任務直接執行。請先做 P1a；後續再拆 P1b/P1c，避免一次同時碰 Article、AI artifacts、bookmark/hidden preferences。

#### P1a. Article/feed persistence baseline

- owner_scope: Article、FeedSource、feed metadata、content extraction metadata。
- suggested_branch_name: `feature/p1a-article-feed-persistence`
- user_flow: 使用者同步 feed 後，API 重啟仍能讀到同一批 feed articles 與 feed metadata。
- acceptance:
  - `AiDailyDbContext` 不再只是 placeholder，至少能 mapping Article 與 FeedSource 或等價 feed metadata。
  - `Article.SourceUrl` 或 stable source identity 有 unique constraint，避免 RSS sync 重複寫入。
  - content status、content text、contentExtractedAt、ingestionScore、matchedKeywords、sourceQualityTier 有可保存欄位或明確 deferral。
  - API 可透過設定選擇 DB article/feed repository；in-memory adapter 保留給 tests/dev fallback。
  - 重啟 API 後，feed articles 與 feed metadata 仍可讀取。
  - 至少包含 article/feed repository persistence tests 或 lightweight integration tests。
- not_included:
  - AI summary/report persistence；後續可拆 P1b。
  - Bookmark/HiddenArticle persistence；後續可拆 P1c。
  - Redis production cache adapter。
  - Auth UI、CI/CD pipeline、admin feed-source management UI。

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
first_slice: S4-1. Deep report provider、SSE contract、rate limit 與 spec 偏差回補
prerequisite:
  - Prompt-Ready Next Work is present and current
  - worktree target repo is confirmed before branching
why_not_s1_1:
  - S1-1 stats API 與 Dashboard stats row 已部分完成，不應再當成從零 implementation slice。
  - S1-1 下一步應是驗收、補測與 AI summary count source-of-truth 修正。
next_order:
  - S4-1. Deep report provider、SSE contract、rate limit 與 spec 偏差回補
  - P1a. Article/feed persistence baseline
  - S3-1. AI quick summary state sync 與 cache correctness
  - S2-3b. Feed relevance gate and ranking hardening
  - S2-3a. Rejected candidate metadata decision
  - S2-4. Source/content quality upgrade
  - S1 follow-up. Reader list cursor hardening
s5_handoff:
  execution_rule: "Use the same feature branch, but run one slice per build-and-learn-loop pass; do not merge S5 and S5-1 into one implementation pass."
  shared_branch: feature/s5-personalization
  recommended_order:
    - S5. 個人化、主題與收藏
    - S5-1. 不感興趣文章隱藏與負回饋個人化
  reason: "S5 establishes bookmark/theme and minimal user preference/auth behavior; S5-1 extends that personalization layer with negative feedback and hidden article filtering."
```
