<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRoute } from 'vue-router'

import AiSummaryPanel from '@/components/ai/AiSummaryPanel.vue'
import { useArticle } from '@/composables/useArticle'
import { useAiReportStream } from '@/composables/useAiReportStream'

const route = useRoute()
const theme = ref<'dark' | 'light'>('dark')

const articleId = computed(() => String(route.params.id ?? ''))
const { article, errorMessage, isLoading, isNotFound } = useArticle(() => articleId.value)
const aiReport = useAiReportStream(() => articleId.value)

const keyPoints = computed(() => {
  if (aiReport.report.value) {
    return aiReport.report.value.keyPoints.map((description, index) => ({
      title: `Key point ${index + 1}`,
      description,
    }))
  }

  const current = article.value
  if (!current) return []

  return [
    {
      title: 'Core signal',
      description: current.summary ?? 'The article is queued for AI summarization.',
    },
    {
      title: 'Operational impact',
      description: `${current.sourceName} frames this as relevant to ${current.tags.join(', ') || 'AI'} teams.`,
    },
    {
      title: 'What to monitor next',
      description: 'Watch for pricing, benchmarks, API availability, safety notes, and implementation examples.',
    },
  ]
})

const pros = computed(() =>
  aiReport.report.value?.pros ?? [
    'Useful signal for product and engineering prioritization.',
    'Good candidate for a deeper benchmark or source follow-up.',
  ],
)

const cons = computed(() =>
  aiReport.report.value?.cons ?? [
    'Claims may need independent validation before operational decisions.',
    'Summary depth depends on currently available source metadata.',
  ],
)

const timeline = computed(() => {
  if (aiReport.report.value) return aiReport.report.value.timeline
  if (!article.value) return []

  return [
    {
      label: 'Before publication',
      description: 'Related model, safety, and agent news established the background for this update.',
    },
    {
      label: publishedLabel.value,
      description: `${article.value.sourceName} published this story and AI Daily indexed it for review.`,
    },
  ]
})

const impactRows = computed(() => {
  const scores = aiReport.report.value?.scores
  return [
    { label: 'Impact', score: scores?.impact ?? 80, color: 'var(--accent)', warn: false },
    { label: 'Confidence', score: scores?.confidence ?? 70, color: 'var(--green)', warn: false },
    { label: 'Controversy', score: scores?.controversy ?? 50, color: 'var(--red)', warn: true },
  ]
})

const contentState = computed(() => {
  const status = article.value?.contentStatus ?? 'summary_fallback'

  if (status === 'full_content_ready') {
    return {
      label: 'Full content ready',
      tone: 'ready',
      generationLabel: aiReport.hasReport.value ? 'Use saved report' : 'Generate full AI report',
      description: 'Readable source text is available for the AI report.',
    }
  }

  if (status === 'extraction_failed') {
    return {
      label: 'Extraction failed',
      tone: 'warning',
      generationLabel: aiReport.hasReport.value ? 'Use saved report' : 'Generate from summary',
      description: 'Source extraction failed, so the report will use RSS summary and metadata.',
    }
  }

  if (status === 'extracting_source') {
    return {
      label: 'Extracting source',
      tone: 'pending',
      generationLabel: 'Generate from summary',
      description: 'Source extraction is still pending; summary fallback remains available.',
    }
  }

  return {
    label: 'Summary fallback',
    tone: 'fallback',
    generationLabel: aiReport.hasReport.value ? 'Use saved report' : 'Generate from summary',
    description: 'Full text is not available yet, so the report will use imported metadata.',
  }
})

const sourceContentPreview = computed(() => {
  const text = article.value?.contentText ?? article.value?.summary ?? ''
  return text.length > 520 ? `${text.slice(0, 520).trim()}...` : text
})

const tagClass = computed(() => {
  const tag = article.value?.tags[0]
  if (tag === 'research') return 'tag-research'
  if (tag === 'product' || tag === 'agent') return 'tag-product'
  if (tag === 'safety') return 'tag-safety'
  return 'tag-model'
})

const publishedLabel = computed(() => {
  if (!article.value) return ''

  return new Intl.DateTimeFormat('en', {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(article.value.publishedAt))
})

function toggleTheme(): void {
  theme.value = theme.value === 'dark' ? 'light' : 'dark'
}

function generateReport(force = false): void {
  void aiReport.generate(force)
}

</script>

<template>
  <div class="app-shell report-shell" :data-theme="theme">
    <header class="header">
      <RouterLink class="back-btn" :to="{ name: 'dashboard' }">Back</RouterLink>

      <div class="header-title">{{ article?.title ?? 'Loading report...' }}</div>
      <div class="header-meta">{{ article?.sourceName ?? 'AI Daily' }} / {{ publishedLabel }}</div>

      <div class="header-right">
        <button class="hdr-btn" type="button">Save</button>
        <a v-if="article" class="hdr-btn" :href="article.sourceUrl" target="_blank" rel="noreferrer">Source</a>
        <button class="hdr-btn accent" type="button">Export PDF</button>
        <button class="theme-toggle" type="button" @click="toggleTheme">{{ theme === 'dark' ? 'L' : 'D' }}</button>
      </div>
    </header>

    <main v-if="isLoading" class="report-main report-main--single">
      <div class="report-body">
        <div class="feed-state">Loading report...</div>
      </div>
    </main>

    <main v-else-if="isNotFound" class="report-main report-main--single">
      <div class="report-body">
        <div class="feed-state feed-state--error">
          Article not found. This report may have moved or the feed item has not been imported yet.
        </div>
      </div>
    </main>

    <main v-else-if="errorMessage" class="report-main report-main--single">
      <div class="report-body">
        <div class="feed-state feed-state--error">{{ errorMessage }}</div>
      </div>
    </main>

    <main v-else-if="article" class="report-main">
      <div class="report-body">
        <section class="origin-card">
          <div class="origin-icon">AI</div>
          <div class="origin-info">
            <h1 class="origin-title">{{ article.title }}</h1>
            <div class="origin-meta">
              <span class="source-chip">{{ article.sourceName }}</span>
              <span v-if="article.tags[0]" class="tag-badge" :class="tagClass">{{ article.tags[0] }}</span>
              <a class="origin-link" :href="article.sourceUrl" target="_blank" rel="noreferrer">Open original</a>
            </div>
          </div>
        </section>

        <section class="section">
          <div class="section-label"><span class="section-label-icon">01</span> TL;DR</div>
          <div v-if="aiReport.hasReport.value" class="ai-summary-panel">
            <div class="summary-content">
              <p class="summary-editor">{{ aiReport.report.value?.tldr }}</p>
              <div class="summary-meta">
                <span>Provider: {{ aiReport.report.value?.provider }}</span>
                <span>{{ new Date(aiReport.report.value?.generatedAt ?? '').toLocaleString() }}</span>
              </div>
            </div>
          </div>
          <AiSummaryPanel v-else :article="article" />
          <div class="report-generate-row">
            <button class="action-btn primary" type="button" :disabled="aiReport.isGenerating.value" @click="generateReport(false)">
              {{ aiReport.isGenerating.value ? 'Generating...' : contentState.generationLabel }}
            </button>
            <button class="action-btn" type="button" :disabled="aiReport.isGenerating.value" @click="generateReport(true)">
              Regenerate
            </button>
          </div>
          <p v-if="aiReport.statusMessage.value" class="report-stream-status">{{ aiReport.statusMessage.value }}</p>
          <p v-if="aiReport.errorMessage.value" class="report-stream-status report-stream-status--error">
            {{ aiReport.errorMessage.value }}
          </p>
        </section>

        <section class="section">
          <div class="section-label"><span class="section-label-icon">02</span> Source Content</div>
          <div class="source-content-card" :class="`source-content-card--${contentState.tone}`">
            <div class="source-content-head">
              <span class="source-content-status">{{ contentState.label }}</span>
              <span v-if="article.contentExtractedAt" class="source-content-time">
                {{ new Date(article.contentExtractedAt).toLocaleString() }}
              </span>
            </div>
            <p class="source-content-note">{{ contentState.description }}</p>
            <p class="source-content-preview">{{ sourceContentPreview || 'No readable source content has been imported yet.' }}</p>
          </div>
        </section>

        <section class="section">
          <div class="section-label"><span class="section-label-icon">03</span> Key Points</div>
          <div class="keypoints">
            <article v-for="(point, index) in keyPoints" :key="point.title" class="kp-card">
              <div class="kp-num">{{ index + 1 }}</div>
              <div class="kp-content">
                <h2 class="kp-title">{{ point.title }}</h2>
                <p class="kp-desc">{{ point.description }}</p>
              </div>
            </article>
          </div>
        </section>

        <section class="section">
          <div class="section-label"><span class="section-label-icon">04</span> Upside vs Risk</div>
          <div class="procon-grid">
            <div class="procon-card pro-card">
              <h2 class="procon-title">Upside</h2>
              <div class="procon-list">
                <p v-for="item in pros" :key="item" class="procon-item"><span class="procon-marker">+</span> {{ item }}</p>
              </div>
            </div>
            <div class="procon-card con-card">
              <h2 class="procon-title">Risk</h2>
              <div class="procon-list">
                <p v-for="item in cons" :key="item" class="procon-item"><span class="procon-marker">-</span> {{ item }}</p>
              </div>
            </div>
          </div>
        </section>

        <section class="section">
          <div class="section-label"><span class="section-label-icon">05</span> Context Timeline</div>
          <div class="timeline">
            <div v-for="(item, index) in timeline" :key="`${item.label}-${index}`" class="tl-item">
              <div class="tl-left">
                <div class="tl-dot" :class="{ old: index === 0 }"></div>
                <div v-if="index < timeline.length - 1" class="tl-line"></div>
              </div>
              <div class="tl-content">
                <div class="tl-date">{{ item.label }}</div>
                <p class="tl-text">{{ item.description }}</p>
              </div>
            </div>
          </div>
        </section>
      </div>

      <aside class="report-side">
        <section class="side-section">
          <div class="side-label">Impact Meter</div>
          <div class="impact-rows">
            <div v-for="row in impactRows" :key="row.label" class="impact-row">
              <div class="impact-label">
                <span>{{ row.label }}</span>
                <span class="impact-score" :class="{ warn: row.warn }">{{ row.score }} / 100</span>
              </div>
              <div class="impact-bar-bg"><div class="impact-bar" :style="{ width: `${row.score}%`, background: row.color }"></div></div>
            </div>
          </div>
        </section>

        <section class="side-section">
          <div class="side-label">Editor Opinion</div>
          <div class="opinion-card">
            <p class="opinion-text">
              {{ aiReport.report.value?.editorNote ?? 'Treat this as a high-signal update, but pair it with source verification and follow-up coverage before making strategic bets.' }}
            </p>
            <div class="opinion-rating-label">{{ aiReport.report.value?.rating ?? 'Worth tracking' }}</div>
          </div>
        </section>

        <section class="side-section">
          <div class="side-label">Related Tags</div>
          <div class="tag-cloud">
            <span v-for="tag in aiReport.report.value?.relatedTags ?? article.tags" :key="tag" class="tag-chip">{{ tag }}</span>
          </div>
        </section>

        <section class="side-section">
          <div class="side-label">Actions</div>
          <div class="action-list">
            <button class="action-btn primary" type="button">Save report</button>
            <a class="action-btn" :href="article.sourceUrl" target="_blank" rel="noreferrer">Read source</a>
            <RouterLink class="action-btn" :to="{ name: 'dashboard' }">Back to dashboard</RouterLink>
          </div>
        </section>
      </aside>
    </main>

    <main v-else class="report-main report-main--single">
      <div class="report-body">
        <div class="feed-state">Loading report...</div>
      </div>
    </main>
  </div>
</template>
