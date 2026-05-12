# AI Daily Slice Plan

## Goal

Build AI Daily as a usable daily AI-news reader with a Vue 3 frontend, ASP.NET Core 8 API, PostgreSQL persistence, Redis-backed caching where useful, RSS ingestion, and AI-generated article summaries/reports.

The plan favors vertical slices that each produce a testable user-visible workflow, instead of building all infrastructure first.

## Assumptions

- Target repo scope is `AINews/`.
- The current repo only contains `AI-Daily-Spec.md`, so early slices include project scaffolding.
- MVP should prioritize the daily article feed, article details, AI summary preview, deep report generation, and bookmarking.
- Authentication can be introduced as a minimal local/JWT capability when user-specific features require it; public read-only article browsing can work before full auth.
- RSS ingestion can start with a small fixed source set and expand after the feed pipeline is stable.
- AI provider integration should be wrapped behind an application interface so model/provider changes remain localized.

## Target Repo Scope

- Frontend app: Vue 3, Vite, TypeScript, Pinia, Vue Router, Naive UI or an equivalent existing UI dependency if the repo later chooses one.
- Backend app: ASP.NET Core 8 Web API, EF Core, PostgreSQL, Redis, Hangfire, Serilog, OpenAPI.
- Shared contracts: API response envelope, pagination shape, article DTOs, AI summary/report DTOs, error codes.
- Tests: focused backend unit/integration tests and frontend store/component tests for each shipped slice.

## First Slice

S1. Readable Article Feed

This is first because it proves the central user value: a user can open the app and scan recent AI articles. It also establishes the repo structure, API conventions, database model, and frontend data flow that later slices build on.

## Slices

### S1. Readable Article Feed

- user_flow: A user opens `/`, sees recent AI articles, filters by keyword/tag/source/date, and loads more results with cursor pagination.
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
  - `GET /api/v1/articles` returns articles in the documented response envelope.
  - Cursor pagination supports `cursor`, `limit`, `keyword`, `tags`, `source`, and `date`.
  - The dashboard renders article cards with title, summary, source, tags, published time, AI-summary availability, bookmark state, and read time.
  - Loading, empty, and API error states are visible and non-blocking.
  - Backend includes at least one repository/query handler test for filtering and pagination.
  - Frontend includes at least one store or component test for loading and rendering article lists.
- not_included:
  - RSS crawler automation.
  - AI summary generation.
  - Bookmark mutation.
  - Login or user profiles.
  - Production deployment.
- reason_first: Establishes the end-to-end app skeleton and the primary reading workflow with the smallest useful backend/frontend contract.

### S2. Article Detail And Source Ingestion

- user_flow: A user selects an article, opens `/article/:id/report` or an article detail route, sees richer content, and the system can ingest new articles from configured RSS sources.
- files_or_areas:
  - `frontend/src/views/ArticleDetail.vue`
  - `frontend/src/router`
  - `frontend/src/composables/useArticle.ts`
  - `backend/src/AiDaily.API/Controllers/ArticlesController.cs`
  - `backend/src/AiDaily.Application/Articles`
  - `backend/src/AiDaily.Infrastructure/FeedCrawler`
  - `backend/src/AiDaily.Domain/Entities/FeedSource.cs`
  - `backend/src/AiDaily.Infrastructure/Persistence/Configurations`
  - `backend/src/AiDaily.Infrastructure/Migrations`
- acceptance:
  - `GET /api/v1/articles/{id}` returns full article details or `ARTICLE_NOT_FOUND`.
  - Feed sources are represented in persistence and can be seeded/configured.
  - A crawler job can fetch at least one RSS source, normalize article data, deduplicate by `source_url`, and store articles.
  - Crawler failures are logged and do not crash the API.
  - Article detail view handles loading, not found, and successful states.
- not_included:
  - AI-generated summary/report.
  - User-specific bookmarks.
  - Full source list tuning and scraping-only sources.
- reason_first: Turns the app from static/manual article data into a renewable news product while keeping the user-facing flow simple.

### S3. AI Summary Preview

- user_flow: A user scans an article card or detail page, opens the AI summary panel, and sees highlights, impact scope, controversy, and editor view when a summary exists.
- files_or_areas:
  - `frontend/src/components/ai/AiSummaryPanel.vue`
  - `frontend/src/stores/aiSummaryStore.ts`
  - `frontend/src/services/aiSummaryApi.ts`
  - `backend/src/AiDaily.API/Controllers/AiSummaryController.cs`
  - `backend/src/AiDaily.Application/AiSummaries`
  - `backend/src/AiDaily.Domain/Entities/AiSummary.cs`
  - `backend/src/AiDaily.Infrastructure/AI`
  - `backend/src/AiDaily.Infrastructure/Cache`
- acceptance:
  - `GET /api/v1/articles/{id}/ai-summary` returns quick preview fields in the documented envelope.
  - Missing summary returns a documented error or empty state that the frontend can distinguish.
  - AI summary persistence supports quick preview fields and links to articles.
  - Cached reads avoid unnecessary database or AI provider calls where configured.
  - Frontend shows summary fields with clear loading and empty states.
- not_included:
  - Streaming generation.
  - Deep report page.
  - PDF/export/share.
- reason_first: Adds the core AI value without introducing the complexity of streaming or long-form reports.

### S4. Streaming Deep Report Generation

- user_flow: A user opens an article report page, triggers AI report generation, watches content stream in, and then sees a complete TL;DR, key points, pros/cons, timeline, scores, related tags, editor note, and rating.
- files_or_areas:
  - `frontend/src/views/AiDeepReport.vue`
  - `frontend/src/components/ai/ImpactMeter.vue`
  - `frontend/src/composables/useAiReportStream.ts`
  - `backend/src/AiDaily.API/Controllers/AiSummaryController.cs`
  - `backend/src/AiDaily.Application/AiSummaries/GenerateAiSummary`
  - `backend/src/AiDaily.Infrastructure/AI/ClaudeAiService.cs`
  - `backend/src/AiDaily.Application/Common/Errors`
  - `backend/src/AiDaily.API/Middleware`
- acceptance:
  - `POST /api/v1/articles/{id}/ai-summary/generate` streams SSE events using the documented event shape.
  - `GET /api/v1/articles/{id}/ai-report` returns the completed report.
  - Rate limiting prevents repeated expensive generation.
  - Concurrent generation for the same article returns `AI_GENERATION_IN_PROGRESS`.
  - Frontend can reconnect or show a retry state if streaming fails.
  - Generated report is stored and reused on later visits unless forced regeneration is allowed.
- not_included:
  - Multi-provider AI routing.
  - Prompt A/B testing UI.
  - Exporting reports.
- reason_first: Completes the highest-differentiation workflow after article data and summary storage already exist.

### S5. Personalization, Theme, And Bookmarks

- user_flow: A user switches light/dark theme, bookmarks articles, opens `/bookmarks`, and keeps preferences across sessions.
- files_or_areas:
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
  - `POST /api/v1/articles/{id}/bookmark` and `DELETE /api/v1/articles/{id}/bookmark` update bookmark state.
  - Bookmark list displays saved articles and empty state.
  - Theme follows system preference by default and can be manually persisted.
  - User-specific mutations require authentication or a clearly scoped temporary local-user strategy.
  - Bookmark and theme behavior is covered by focused frontend tests.
- not_included:
  - Social sharing.
  - PDF export.
  - Multi-device account management beyond the minimum auth approach chosen for MVP.
- reason_first: Adds retention and comfort features after the core reading and AI workflows are proven.

## Deferred

- PDF export for reports.
- Shareable report links.
- Admin feed-source management UI.
- Anthropic/OpenAI/provider switching UI.
- Full observability dashboards.
- Kubernetes or Azure Container Apps deployment.
- Prompt version experiment tooling.
- Advanced full-text ranking beyond the initial PostgreSQL search/indexes.

## Handoff To Build And Learn Loop

```yaml
next_skill: build-and-learn-loop
artifact_path: docs/slices/ai-daily-slices.md
first_slice: Readable Article Feed
```
