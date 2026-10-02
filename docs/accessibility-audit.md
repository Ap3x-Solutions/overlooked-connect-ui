# OVC-110 - Accessibility Audit

**Scope:** InternalWeb and PublicWeb Razor views
**Standard:** WCAG 2.1 Level AA
**Date:** 27 September 2026

---

## 1. Method

- Manual review of every `.cshtml` view for image alt text, form labels,
  and colour-only signalling.
- Contrast ratio testing using WebAIM's Contrast Checker.
- Keyboard navigation testing on key form screens.

---

## 2. Findings - Passing

### Image alt text

Every img tag across both projects has an alt attribute. Search across
all `.cshtml` files returned zero img tags missing alt.

### Form labels

Every form input uses a label with asp-for or an associated
asp-validation-for span for its accessible name.

### Colour is not the only signal

Status badges include text content (e.g. HIGH, IN PROGRESS) rather than
relying solely on red/amber/green colour to convey meaning.

### Focus and keyboard operability

Forms use standard HTML button, input and anchor elements. jQuery
unobtrusive validation is keyboard-accessible by default.

---

## 3. Findings - Fixed

### 3.1 Internal Web muted text contrast

**File:** src/OverlookedConnect.InternalWeb/wwwroot/css/internal.css
**Change:** --text-muted variable

| Before | After |
|--------|-------|
| #9AA3B2 | #6B7280 |

**Reason:** #9AA3B2 on white gives a contrast ratio of 2.54:1, which
fails WCAG AA for both normal text and large text. It was used for
secondary text in data tables, list rows, chart axis labels, and phone
tab labels - all meaningful UI text.

#6B7280 on white gives 4.83:1, which passes WCAG AA for normal text and
WCAG AAA for large text. The public site already used this value, so the
change also brings the two stylesheets into alignment.

Verification: https://webaim.org/resources/contrastchecker/?fcolor=6B7280&bcolor=FFFFFF

---

## 4. Contrast Pair Verification

| Pair | Purpose | Ratio | Result |
|------|---------|-------|--------|
| #8FA0BE on #0F1E3A | Brand subtitle in sidebar | 6.26:1 | PASS AA |
| #7F8FAB on #0F1E3A | Nav icons | 5.06:1 | PASS AA |
| #5B6675 on #FFFFFF | Body text | 5.82:1 | PASS AA |
| #6B7280 on #FFFFFF | Muted text (after fix) | 4.83:1 | PASS AA |

---

## 5. Deliberately Out of Scope
Gold: #D4A544 on White: #FFFFFF gives a contrast ratio of 2.26:1, which fails WCAG AA.
However, in the current UI, gold is only ever applied as:

- Decorative borders and icons (`border-color: var(--gold)` on dark navy)

- Backgrounds for dark text (e.g. `.btn-gold` with `color: var(--navy-900)`)

- Chart bars (`.chart .bar.gold` - decorative)

Gold is used exclusively as a decorative border, chart fill, and background for dark navy text.
It's never applied as text on a light background in the current UI.
So if a future change uses gold as text on light, that would need testing.

---

## 6. Recommendation

Any new text colour introduced into either stylesheet should be checked
against WebAIM's Contrast Checker before merge.

---

## 7. References

- WebAIM. [s.a.]. Contrast Checker. [online]. Available at:
  https://webaim.org/resources/contrastchecker/.
- W3C. 2018. Web Content Accessibility Guidelines (WCAG) 2.1. [online].
  Available at: https://www.w3.org/TR/WCAG21/.
