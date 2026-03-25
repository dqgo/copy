export interface SensitiveMatch {
  kind: "otp" | "bank_card" | "cn_id" | "password_hint";
  evidence: string;
}

export interface SensitivePolicy {
  enabled: boolean;
  allowManualOverride: boolean;
}

const OTP_REGEX = /\b\d{4,8}\b/;
const BANK_CARD_REGEX = /\b\d{16,19}\b/;
const CN_ID_REGEX = /\b\d{17}[\dXx]\b/;
const PASSWORD_HINT_REGEX = /(password|passwd|口令|密码)/i;

export function detectSensitiveText(text: string): SensitiveMatch[] {
  const matches: SensitiveMatch[] = [];

  if (OTP_REGEX.test(text) && /(验证码|otp|code)/i.test(text)) {
    matches.push({ kind: "otp", evidence: "otp-pattern" });
  }

  if (BANK_CARD_REGEX.test(text)) {
    matches.push({ kind: "bank_card", evidence: "16-19-digit" });
  }

  if (CN_ID_REGEX.test(text)) {
    matches.push({ kind: "cn_id", evidence: "cn-id-pattern" });
  }

  if (PASSWORD_HINT_REGEX.test(text)) {
    matches.push({ kind: "password_hint", evidence: "password-keyword" });
  }

  return matches;
}

export function shouldBlockByPolicy(text: string, policy: SensitivePolicy, manualOverride: boolean): boolean {
  if (!policy.enabled) {
    return false;
  }

  if (manualOverride && policy.allowManualOverride) {
    return false;
  }

  return detectSensitiveText(text).length > 0;
}
