# AI Daily Optimization Slice Plan

## Planning Mode

apply_to_existing_project_roadmap

This artifact turns the current repo health review into implementation-ready vertical slices. It complements `docs/slices/ai-daily-slices.md` and does not replace the historical slice plan.

## Goal

Move AI Daily from a local MVP/demo state toward a durable, safer, and easier-to-maintain product baseline without mixing unrelated work into one large implementation pass.

The optimization roadmap focuses on:

- Durable data lifecycle for existing reader, AI, bookmark, and hidden-article flows.
- Safer write/API boundaries before production-like usage.
- Clear API contracts where route names, SSE behavior, and error handling are currently confusing.
- Maintainable verification structure and frontend error/state handling.
- A stable reader list contract that behaves correctly as data and user preferences change.

## Assumptions

- Target repo scope is this product repo.
- This is a planning-only task; no product code is changed by this artifact.
- Existing MVP behavior is valuable and should be hardened incrementally, not rewritten.
- In-memory adapters remain useful for tests and local fallback until each persistent adapter is complete.
- Formal commit, push, branch creation, release, and deployment are out of scope until the user explicitly asks.
- The existing `docs/slices/ai-daily-slices.md` remains the broader product slice history; this file is the focused optimization roadmap.

## Target Repo Scope

- `backend/src/AiDaily.API`
- `backend/src/AiDaily.Application`
- `backend/src/AiDaily.Domain`
- `backend/src/AiDaily.Infrastructure`
- `backend/tests/AiDaily.UnitTests`
- `frontend/src`
- `frontend/vite.config.cjs`
- `README.md`
- `AI-Daily-Spec.md`
- `docs`
- `docker-compose.yml`

Protected or excluded by default:

- `agent-core/skills`
- `side-project-agent/skills`
- `note-agent/workspace/skills`
- root-level `dashboard.html` and `report.html`, unless a future task explicitly targets legacy static artifacts
- Git publishing operations

## Spec Coverage Inventory

### User Visible Flows

- Dashboard reader feed with filtering, pagination, stats, feed sync, quick summary, bookmark, and hidden-article actions.
- Report page with article context and AI deep report generation.
- Bookmarks and settings views with local personalization.
- Current MVP supports these flows. O1 adds optional PostgreSQL-backed repositories for reader, AI artifact, bookmark, and hidden-article state; default local settings can still choose in-memory adapters.

### Data Lifecycle

- Articles, feed source metadata, bookmarks, hidden articles, AI summaries, and AI reports have EF Core repository paths; runtime can still use in-memory mode per repository.
- Summary read cache, generation trackers, and report rate limiting are still in-memory.
- `AiDailyDatabaseInitializer` uses `EnsureCreatedAsync`; long-term schema evolution needs migrations.
- Reader pagination currently uses offset-like cursor behavior rather than a stable keyset cursor.

### External Integrations

- RSS feed crawl imports articles from configured sources.
- Article content extraction fetches remote HTML and falls back to summary content when blocked or low quality.
- Gemini is available behind `IAiReportGenerator`; spec target still mentions Anthropic/Claude as a future decision.
- Redis exists in `docker-compose.yml` but is not yet a production cache/rate-limit dependency.

### Generated Content Inputs

- AI quick summary and AI report depend on article title, summary, source metadata, tags, content status, and optional extracted content text.
- Full-content claims must only be made when `contentStatus` is `full_content_ready`.
- AI report has schema normalization/validation; quick summary still needs stronger state sync, provider error mapping, and cache replacement semantics.

### API Contracts

- API response envelope exists.
- Deep report generation currently lives under `/api/v1/articles/{articleId}/ai-summary/generate`, even though it produces an AI report SSE stream.
- SSE MVP event names are `started`, `status`, `report`, `completed`, and `error`; the older spec target `start/chunk/field_done/done/error` remains a future product decision.
- Bookmark and hidden-article mutation identity uses `X-AI-Daily-Local-User`.
- Feed crawl POST has no production-grade write guard.

### Persistence And Migrations

- PostgreSQL and Redis are present in local compose.
- `Article` and `FeedSource` mappings exist, including unique source URL/feed URL indexes.
- Durable mappings exist for AI summary, AI report, bookmark, and hidden article state. Cache/rate-limit state remains in-memory.
- EF migrations are not yet established as the normal schema change path.

### Security And Secrets

- CORS currently allows any origin.
- Formal auth/JWT is not implemented.
- API provider keys are backend-only, but production readiness needs documented secret handling and tests that errors do not expose keys or raw provider internals.
- Write endpoints need at least environment-aware guards before any production-like deployment.

### Observability And Failure

- API exception middleware maps unexpected errors to a generic JSON response.
- Feed crawl returns run status, but rejected candidates are currently logs-only.
- There is no structured production observability baseline, no OpenAPI contract generation, and no CI/CD gate.
- Frontend API errors are parsed in duplicate helper functions and some mutation errors share global article error state.

### Non Functional Requirements

- Spec targets include P95 API response goals, SSE first-response goals, Redis cache targets, auth, rate limiting, browser compatibility, and test coverage.
- Current test baseline is healthy, but backend tests are still wrapped in one large xUnit fact, which limits failure localization.
- Vite/Vitest work, but `vite.config.cjs` triggers Vite CJS Node API deprecation warnings.

### Deferred Or Explicitly Excluded

- Production deployment, cloud infrastructure, Kubernetes/Azure hosting, and release automation.
- Full provider switching UI.
- PDF export and share links.
- Admin feed-source management UI.
- Redis production cache adapter until persistence and write guards are stable.
- Large UI redesign.

## Coverage Gaps And Handling

| Gap | Handling | Recommended action | Location |
| --- | --- | --- | --- |
| Existing user-visible state disappears on API restart. | slice | add_slice | O1 persistence foundation |
| Write endpoints and CORS are too permissive for production-like usage. | slice | add_slice | O2 API write guard and local-user boundary |
| Deep report generation route name is semantically confusing. | slice | add_slice | O3 AI report route and contract cleanup |
| Quick summary state, cache replacement, and provider error semantics are not fully aligned. | slice | add_acceptance | O3 AI route/contract cleanup or existing S3-1 if implemented separately |
| Backend tests run as one large wrapper fact. | slice | add_slice | O4 verification modernization |
| Frontend API errors are duplicated and mutation errors can pollute broader state. | slice | add_slice | O5 frontend API and state cleanup |
| Reader cursor is offset-like and unstable under hidden/filter/new article changes. | slice | add_slice | O6 reader cursor hardening |
| Redis-backed production cache/rate-limit state is still absent. | deferred | defer | after O1 and O2 |
| CI/CD would currently freeze unstable contracts. | deferred | defer | after O2, O3, and O4 |
| Observability dashboard and metrics are not yet implemented. | deferred | defer | after persistence and API guards |

## First Slice

O2. API write guard and local-user boundary

Reason: O1 persistence foundation is complete enough for the current MVP baseline. The next highest-risk boundary is preventing permissive local write behavior from becoming accidental production behavior.

## Recommended Order

1. O1. Persistence foundation for existing MVP state (done)
2. O2. API write guard and local-user boundary
3. O3. AI report route and generated-content contract cleanup
4. O4. Verification modernization
5. O5. Frontend API and state cleanup
6. O6. Reader cursor hardening

O5 can run before O4 if the next developer is frontend-focused. O6 should run after O1 if possible, because stable persistence makes cursor behavior easier to validate against restart and mutation scenarios.

## Slices

### O1. Persistence Foundation For Existing MVP State

- status: done
- user_flow:
  - User syncs the feed, bookmarks an article, hides an article, generates an AI summary/report, restarts the API, and still sees the same reader and personalization state.
- files_or_areas:
  - `backend/src/AiDaily.Domain/Entities`
  - `backend/src/AiDaily.Application/AiSummaries`
  - `backend/src/AiDaily.Application/Bookmarks`
  - `backend/src/AiDaily.Application/UserPreferences`
  - `backend/src/AiDaily.Infrastructure/Persistence`
  - `backend/src/AiDaily.Infrastructure/Repositories`
  - `backend/src/AiDaily.Infrastructure/AI`
  - `backend/src/AiDaily.API/Program.cs`
  - `backend/src/AiDaily.API/appsettings.Local.example.json`
  - `backend/tests/AiDaily.UnitTests`
  - `README.md`
- acceptance:
  - Add EF Core mappings or equivalent persistent repositories for `AiSummary`, `AiReport`, `Bookmark`, and `HiddenArticle`.
  - Keep existing `Article` and `FeedSource` persistence behavior intact, including unique source URL/feed URL semantics.
  - Add unique constraints for one summary/report per article and one bookmark/hidden preference per `(userId, articleId)`.
  - Runtime configuration can choose persistent repositories for article/feed, AI artifacts, bookmark, and hidden preference state while keeping in-memory adapters for tests/dev fallback.
  - API restart persistence tests prove that article/feed state, bookmark state, hidden state, AI summary, and AI report can be read after recreating the DbContext or service provider.
  - Replace production-path `EnsureCreatedAsync` with an explicit migration-friendly path, or document why a transitional test-only `EnsureCreatedAsync` path remains.
  - Existing tests still pass.
- not_included:
  - Formal auth/JWT.
  - Redis production cache adapter.
  - Admin feed-source UI.
  - CI/CD.
  - Cursor algorithm changes.
  - Provider switching.
- reason_first:
  - Durable state is the core prerequisite for moving from demo to usable product. It directly addresses the largest repeated gap in README, spec, and existing slice docs.

### O2. API Write Guard And Local-User Boundary

- user_flow:
  - A local developer can still trigger feed sync and personalization flows, while production-like settings prevent unauthenticated cross-origin callers from freely triggering write or AI-cost endpoints.
- files_or_areas:
  - `backend/src/AiDaily.API/Program.cs`
  - `backend/src/AiDaily.API/Controllers/FeedCrawlController.cs`
  - `backend/src/AiDaily.API/Controllers/BookmarksController.cs`
  - `backend/src/AiDaily.API/Controllers/UserPreferencesController.cs`
  - `backend/src/AiDaily.API/Controllers/AiSummaryController.cs`
  - `backend/src/AiDaily.API/Middleware`
  - `backend/src/AiDaily.Application/FeedCrawler`
  - `backend/src/AiDaily.Application/AiSummaries`
  - `frontend/src/services/apiClient.ts`
  - `frontend/src/services/aiSummaryApi.ts`
  - `README.md`
  - `AI-Daily-Spec.md`
- acceptance:
  - CORS origins are configurable and no longer hard-coded to allow any origin in non-development modes.
  - Feed crawl POST has a minimal guard, such as development-only allowance, admin token, local-only policy, or documented equivalent.
  - AI generation endpoints keep rate limiting and use a stable identity source; local-user header behavior is explicitly scoped to local/MVP mode.
  - Bookmark and hidden-article mutations reject missing or invalid local-user identity in guarded modes instead of silently using an empty user id.
  - Error responses use documented codes for unauthorized, forbidden, rate-limited, and invalid local-user cases.
  - Backend tests cover allowed local mode and rejected guarded mode for at least feed crawl and one user-specific mutation.
  - README documents the local-user strategy and its production limitations.
- not_included:
  - Full login UI.
  - Multi-device account management.
  - OAuth or external identity providers.
  - Role management UI.
  - Redis-backed distributed rate limiting.
- reason_first:
  - The current MVP exposes useful write flows, but those flows should not become accidental production behavior. This slice creates a minimal safety boundary without blocking local development.

### O3. AI Report Route And Generated-Content Contract Cleanup

- user_flow:
  - User opens a report page, triggers AI report generation through an endpoint whose path matches its behavior, sees predictable SSE events, and receives safe, actionable errors when generation fails.
- files_or_areas:
  - `backend/src/AiDaily.API/Controllers/AiSummaryController.cs`
  - `backend/src/AiDaily.Application/AiSummaries`
  - `backend/src/AiDaily.Infrastructure/AI`
  - `frontend/src/services/aiSummaryApi.ts`
  - `frontend/src/composables/useAiReportStream.ts`
  - `frontend/src/views/Report.vue`
  - `frontend/src/types/aiSummary.ts`
  - `frontend/src/tests`
  - `README.md`
  - `AI-Daily-Spec.md`
  - `docs/slices/ai-daily-slices.md`
- acceptance:
  - Add a semantically correct AI report generation route, such as `POST /api/v1/articles/{articleId}/ai-report/generate`.
  - Preserve the old `/ai-summary/generate` route only as a documented compatibility alias, or explicitly remove it if the user approves a breaking API change before implementation.
  - Frontend calls the report route for report generation.
  - SSE event contract remains the versioned MVP shape `started/status/report/completed/error` unless the user explicitly decides to migrate to the older target shape.
  - Provider errors remain secret-safe and map to documented application error codes.
  - Quick summary generation keeps separate non-SSE semantics; report generation no longer reads as a quick-summary action in code or docs.
  - Add backend route/contract tests and frontend service/composable tests covering the report generate path, an SSE error event, and a rate-limit or provider-failure error.
  - README and spec API tables match runtime routes.
- not_included:
  - Switching provider from Gemini to Claude.
  - New prompt strategy or prompt A/B testing.
  - Full Redis cache adapter.
  - Deep report UI redesign.
  - Persistence work already covered by O1.
- reason_first:
  - The current route naming is small but high-friction: it makes report generation look like summary generation. Cleaning this up reduces future integration mistakes while preserving the already-versioned SSE contract.

### O4. Verification Modernization

- user_flow:
  - Developer runs focused backend and frontend checks, gets a failure tied to a specific behavior, and can trust build/test output before committing.
- files_or_areas:
  - `backend/tests/AiDaily.UnitTests`
  - `backend/tests/AiDaily.UnitTests/AiDaily.UnitTests.csproj`
  - `frontend/vite.config.cjs`
  - `frontend/package.json`
  - `frontend/src/tests`
  - `.gitignore`
  - `README.md`
  - `docs/slices/ai-daily-slices.md`
- acceptance:
  - Split the current single wrapper fact into focused xUnit test classes or facts without changing product behavior.
  - Keep `dotnet test backend/tests/AiDaily.UnitTests/AiDaily.UnitTests.csproj` as the primary backend verification command.
  - Keep the local smoke runner only if it still adds value; otherwise document the normal `dotnet test` path.
  - Convert Vite config away from the deprecated CJS Node API if feasible without reintroducing cache/write issues.
  - Frontend `npm.cmd run test` and `npm.cmd run build` remain green.
  - Backend `dotnet test` and `dotnet build backend/AiDaily.sln` remain green.
  - README verification section reflects the final commands.
- not_included:
  - New product functionality.
  - CI/CD pipeline.
  - Coverage threshold enforcement.
  - Test framework replacement.
  - Large refactor of production code just to make tests prettier.
- reason_first:
  - The test suite already catches real behavior, but failure localization and maintenance will get worse as persistence and auth tests grow. Modernizing verification before more hardening work keeps later slices cheaper.

### O5. Frontend API And State Cleanup

- user_flow:
  - User performs feed sync, bookmark, hidden-article, AI summary, and AI report actions; failures show in the right local context without corrupting unrelated dashboard/report state.
- files_or_areas:
  - `frontend/src/services/apiClient.ts`
  - `frontend/src/services/aiSummaryApi.ts`
  - `frontend/src/stores/articleStore.ts`
  - `frontend/src/stores/aiSummaryStore.ts`
  - `frontend/src/stores/bookmarkStore.ts`
  - `frontend/src/stores/preferenceStore.ts`
  - `frontend/src/composables/useAiReportStream.ts`
  - `frontend/src/components`
  - `frontend/src/views`
  - `frontend/src/tests`
- acceptance:
  - Extract one shared API error parser/helper used by article, bookmark, hidden preference, summary, and report services.
  - API error helper preserves `name`, message, status, and optional retry metadata when available.
  - Bookmark, hidden-article, feed sync, article load, summary, and report generation errors have separate store state or UI state boundaries.
  - Mutation failures roll back only the affected optimistic state.
  - Local-user id generation handles missing `localStorage` or `crypto.randomUUID` gracefully.
  - Frontend tests cover API error parsing, optimistic rollback, local-user fallback, and at least one SSE failure path.
  - Existing visual layout and component conventions are preserved.
- not_included:
  - New UI design system.
  - Auth UI.
  - Backend API contract changes, except consuming documented errors already exposed by other slices.
  - Full accessibility audit.
- reason_first:
  - The frontend is already usable, so this slice should reduce state surprises rather than redesign the app. It improves trust in existing flows with a narrow blast radius.

### O6. Reader Cursor Hardening

- user_flow:
  - User scrolls the article list with filters applied, hides or restores articles, and loads more pages without duplicates, skipped items, or unstable page boundaries.
- files_or_areas:
  - `backend/src/AiDaily.Application/Articles`
  - `backend/src/AiDaily.Infrastructure/Repositories`
  - `backend/src/AiDaily.API/Controllers/ArticlesController.cs`
  - `backend/tests/AiDaily.UnitTests`
  - `frontend/src/stores/articleStore.ts`
  - `frontend/src/components/article/ArticleFeed.vue`
  - `frontend/src/tests`
  - `README.md`
  - `AI-Daily-Spec.md`
  - `docs/slices/ai-daily-slices.md`
- acceptance:
  - Replace offset-like cursor behavior with a stable keyset cursor, such as `(publishedAt, ingestionScore, id)` or another documented deterministic order.
  - Cursor encoding/decoding rejects invalid cursor values with a documented API error instead of silently falling back to page one.
  - Hidden-article filtering does not create duplicate or skipped visible articles across pages.
  - New article insertion between page loads does not duplicate already-loaded items for the same filter/order.
  - Backend tests cover cursor continuation, invalid cursor handling, filter interaction, hidden item interaction, and quality-vs-recency ordering if it remains part of the default order.
  - Frontend store tests cover load-more behavior and reset-on-filter behavior with the new cursor contract.
  - API docs describe cursor as opaque and stable, not as an offset.
- not_included:
  - Persistence baseline, unless O1 has not yet been done and a small in-memory test adapter is enough for cursor behavior.
  - Feed relevance/ranking overhaul.
  - Infinite scroll UI redesign.
  - Search indexing or PostgreSQL full-text search.
- reason_first:
  - Cursor bugs are user-visible and become more likely once hidden preferences, persistence, and live feed sync coexist. This slice stabilizes the core reading loop.

## Deferred

- Redis production cache adapter for article lists, stats, summaries, sessions, and distributed rate limiting.
- CI/CD pipeline and required checks.
- OpenAPI/Swagger generated contract review and public API docs.
- Structured observability baseline with request IDs, crawl metrics, provider metrics, and dashboards.
- Formal JWT login flow and multi-device user accounts.
- Admin feed-source management UI.
- Rejected candidate audit persistence and source-quality analytics.
- Provider switching UI and Claude migration.
- PDF export and report share links.
- Cloud deployment and infrastructure-as-code.

## Handoff To Build And Learn Loop

```yaml
next_skill: build-and-learn-loop
artifact_path: docs/slices/ai-daily-optimization-slices.md
first_slice: O2. API Write Guard And Local-User Boundary
recommended_order:
  - O1. Persistence Foundation For Existing MVP State (done)
  - O2. API Write Guard And Local-User Boundary
  - O3. AI Report Route And Generated-Content Contract Cleanup
  - O4. Verification Modernization
  - O5. Frontend API And State Cleanup
  - O6. Reader Cursor Hardening
stop_before:
  - branch creation
  - staging paths
  - commit
  - push
  - production deployment
validation_baseline:
  - dotnet test backend/tests/AiDaily.UnitTests/AiDaily.UnitTests.csproj
  - dotnet build backend/AiDaily.sln
  - npm.cmd run test
  - npm.cmd run build
notes:
  - Run one slice per build-and-learn-loop pass.
  - O1 is complete for the current MVP baseline; do not re-run it unless a future migrations or production persistence slice is explicitly requested.
  - Do not modify protected skill directories for product implementation work.
```
