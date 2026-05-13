<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRoute } from 'vue-router'

import { useArticle } from '@/composables/useArticle'

const route = useRoute()
const theme = ref<'dark' | 'light'>('dark')

const articleId = computed(() => String(route.params.id ?? ''))
const { article, errorMessage, isLoading, isNotFound } = useArticle(() => articleId.value)

const keyPoints = computed(() => {
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
          <div class="tldr-block">
            <p class="tldr-text">{{ article.summary ?? 'No summary available yet.' }}</p>
          </div>
        </section>

        <section class="section">
          <div class="section-label"><span class="section-label-icon">02</span> Key Points</div>
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
          <div class="section-label"><span class="section-label-icon">03</span> Upside vs Risk</div>
          <div class="procon-grid">
            <div class="procon-card pro-card">
              <h2 class="procon-title">Upside</h2>
              <div class="procon-list">
                <p class="procon-item"><span class="procon-marker">+</span> Useful signal for product and engineering prioritization.</p>
                <p class="procon-item"><span class="procon-marker">+</span> Good candidate for a deeper benchmark or source follow-up.</p>
              </div>
            </div>
            <div class="procon-card con-card">
              <h2 class="procon-title">Risk</h2>
              <div class="procon-list">
                <p class="procon-item"><span class="procon-marker">-</span> Claims may need independent validation before operational decisions.</p>
                <p class="procon-item"><span class="procon-marker">-</span> Summary depth depends on currently available source metadata.</p>
              </div>
            </div>
          </div>
        </section>

        <section class="section">
          <div class="section-label"><span class="section-label-icon">04</span> Context Timeline</div>
          <div class="timeline">
            <div class="tl-item">
              <div class="tl-left">
                <div class="tl-dot old"></div>
                <div class="tl-line"></div>
              </div>
              <div class="tl-content">
                <div class="tl-date">Before publication</div>
                <p class="tl-text">Related model, safety, and agent news established the background for this update.</p>
              </div>
            </div>
            <div class="tl-item">
              <div class="tl-left">
                <div class="tl-dot"></div>
              </div>
              <div class="tl-content">
                <div class="tl-date">{{ publishedLabel }}</div>
                <p class="tl-text">{{ article.sourceName }} published this story and AI Daily indexed it for review.</p>
              </div>
            </div>
          </div>
        </section>
      </div>

      <aside class="report-side">
        <section class="side-section">
          <div class="side-label">Impact Meter</div>
          <div class="impact-rows">
            <div class="impact-row">
              <div class="impact-label"><span>Product relevance</span><span class="impact-score">8 / 10</span></div>
              <div class="impact-bar-bg"><div class="impact-bar" style="width: 80%; background: var(--accent)"></div></div>
            </div>
            <div class="impact-row">
              <div class="impact-label"><span>Developer impact</span><span class="impact-score">7 / 10</span></div>
              <div class="impact-bar-bg"><div class="impact-bar" style="width: 70%; background: var(--green)"></div></div>
            </div>
            <div class="impact-row">
              <div class="impact-label"><span>Risk level</span><span class="impact-score warn">5 / 10</span></div>
              <div class="impact-bar-bg"><div class="impact-bar" style="width: 50%; background: var(--red)"></div></div>
            </div>
          </div>
        </section>

        <section class="side-section">
          <div class="side-label">Editor Opinion</div>
          <div class="opinion-card">
            <p class="opinion-text">
              Treat this as a high-signal update, but pair it with source verification and follow-up coverage before making strategic bets.
            </p>
            <div class="opinion-stars" aria-label="4 out of 5">****<span class="star empty">*</span></div>
            <div class="opinion-rating-label">Worth tracking</div>
          </div>
        </section>

        <section class="side-section">
          <div class="side-label">Related Tags</div>
          <div class="tag-cloud">
            <span v-for="tag in article.tags" :key="tag" class="tag-chip">{{ tag }}</span>
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
