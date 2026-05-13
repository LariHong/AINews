export interface AiSummary {
  articleId: string
  highlights: string[]
  impactScope: string
  controversy: string
  editorView: string
  generatedAt: string
}

export interface AiReport {
  articleId: string
  tldr: string
  keyPoints: string[]
  pros: string[]
  cons: string[]
  timeline: AiReportTimelineItem[]
  scores: AiReportScores
  relatedTags: string[]
  editorNote: string
  rating: 'low-impact' | 'medium-impact' | 'high-impact' | 'watchlist'
  provider: string
  generatedAt: string
}

export interface AiReportTimelineItem {
  label: string
  description: string
}

export interface AiReportScores {
  impact: number
  confidence: number
  controversy: number
}

export interface AiReportStreamEvent {
  type: 'started' | 'status' | 'report' | 'completed' | 'error'
  message?: string
  code?: string
  report?: AiReport
}
