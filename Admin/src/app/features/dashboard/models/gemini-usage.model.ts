/** Gemini 使用量（系統自行累計，只含本系統的 OCR 呼叫）；from / to 為半開區間 [from, to) */
export interface GeminiUsage {
  from: string;
  to: string;
  /** 本月呼叫次數 */
  calls: number;
  inputTokens: number;
  outputTokens: number;
  totalTokens: number;
  sources: GeminiUsageSource[];
}

export interface GeminiUsageSource {
  /** invoice-ocr / quote-ocr */
  source: string;
  calls: number;
  inputTokens: number;
  outputTokens: number;
  totalTokens: number;
}

export const GEMINI_SOURCE_LABELS: Record<string, string> = {
  'invoice-ocr': '發票辨識',
  'quote-ocr': '報價單辨識',
};
