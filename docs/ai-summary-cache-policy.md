# AI Summary Cache Policy

S3-1 quick summaries use `ai-summary:{articleId}` as the logical cache key.

- DTO contract note: `highlights` is a `string[]` in the MVP API, while some spec examples describe it as a single string. The list shape is intentional for card/detail rendering.
- Persisted metadata: each generated summary stores `provider`, `promptVersion`, and `generatedAt` with the quick preview fields.
- Source of truth: persisted `AiSummary` records keyed by `articleId`.
- Read path: `GET /api/v1/articles/{id}/ai-summary` checks the read cache first, then the repository, then fills the cache.
- Generate path: `POST /api/v1/articles/{id}/ai-summary` reuses the persisted summary unless `force=true`.
- Invalidation: forced regeneration removes `ai-summary:{articleId}` before provider generation and replaces it after persistence succeeds.
- Redis is an optional future adapter for `IAiSummaryReadCache`; the current MVP uses the in-memory adapter with the same key semantics.
