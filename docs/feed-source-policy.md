# Feed Source Policy

## Goal

AI Daily should prefer sources that reliably produce relevant AI news and research updates. Feed ingestion must scan enough candidates to recover good articles after low-value feed entries, then apply deterministic quality rules before articles enter the normal reader feed.

## URL Selection

Prefer:

- RSS or Atom feeds over homepage HTML.
- AI topic, tag, category, or research lab news feeds.
- Official product, research, policy, or engineering blogs with stable feed URLs.
- Sources whose feed entries include title, canonical link, summary, and published date.

Avoid:

- Homepages, search result pages, commercial directory pages, and generic index pages.
- Pages that require JavaScript execution to reveal article links.
- Feeds dominated by jobs, event registrations, sponsor posts, newsletter housekeeping, or release-note indexes.
- Sources that regularly duplicate another selected feed without adding useful context.

## Source Metadata

Each feed source should carry more than a URL:

- `sourceType`: RSS, Atom, official blog, research lab news, or aggregator.
- `topicScope`: the expected topical boundary, such as `ai-news`, `ai-research`, or `ai-ml-data-engineering`.
- `defaultCandidateLimit`: how many feed entries to inspect before stopping.
- `isEnabled`: whether the source participates in crawler runs.
- `sourceQualityTier`: `core`, `standard`, or `watch`.
- `qualityNotes`: why the source is useful and what to filter aggressively.

## Candidate Depth

Do not treat the first 10 RSS entries as the best 10 articles. Each source should scan to its `defaultCandidateLimit` so the crawler can skip low-value candidates and still find useful AI stories later in the feed. A source that repeatedly produces no accepted articles after the candidate limit should be moved to `watch` or disabled.

## Deterministic Pre-Filter

Before saving a candidate into the normal feed, check title, summary, canonical URL, and any explicit allowlist topic metadata. Source name or broad topic scope can inform source tuning, but they must not make a candidate pass the AI relevance gate when the candidate content and URL have no AI signal.

Accept when the candidate has clear AI relevance, such as AI, artificial intelligence, agents, LLMs, machine learning, model releases, benchmarks, multimodal systems, robotics, safety, reasoning, or named AI providers.

Reject or mark when the candidate is clearly low value for the reader feed:

- Job posts or hiring pages.
- Event-only pages, registrations, tickets, or webinar announcements.
- Sponsor and advertorial entries.
- Newsletter housekeeping such as unsubscribe or preference-center posts.
- Pure release-note indexes, changelogs, or index pages.
- Very short entries with too little signal to evaluate.

## Ingestion Metadata

Persist quality metadata with accepted articles so filtering can be tuned later:

- `ingestionScore`
- `matchedKeywords`
- `sourceQualityTier`
- `rejectionReason` when a candidate or imported article should be hidden from the normal feed

The default reader API should exclude rejected articles. Debug or admin views can expose rejected candidates later, but that is outside the current slice.

## Current MVP Gap

The current crawler applies the deterministic pre-filter and logs rejected candidates, but it does not persist rejected candidate records or a long-lived rejection audit trail.

S2-3a decision: keep rejection handling as logs-only MVP behavior for now. Rejected candidate audit persistence is deferred because adding a durable audit store and query surface would expand this slice into rejected-candidate persistence/admin-debug work.

Consequences of this deferral:

- Rejected candidates are visible only in crawler run logs during the current run.
- The system cannot perform long-term rejected-candidate analysis, rejection-rate trends, or source quality scoring from rejected metadata.
- Source tuning must rely on accepted article metadata, runtime logs, and manual observation until a later rejected audit persistence slice exists.
- Debug or admin views for rejected candidates remain out of scope.

Accepted articles should continue to store `ingestionScore`, `matchedKeywords`, and `sourceQualityTier`. Rejected candidates should not silently disappear from the planning model; this policy records the current documented deferral instead of claiming persistent audit data exists.
