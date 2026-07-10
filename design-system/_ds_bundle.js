/* @ds-bundle: {"format":4,"namespace":"QuiztinDesignSystem_138691","components":[{"name":"Button","sourcePath":"components/actions/Button.jsx"},{"name":"IconButton","sourcePath":"components/actions/IconButton.jsx"},{"name":"Badge","sourcePath":"components/feedback/Badge.jsx"},{"name":"ProgressBar","sourcePath":"components/feedback/ProgressBar.jsx"},{"name":"Toast","sourcePath":"components/feedback/Toast.jsx"},{"name":"Tooltip","sourcePath":"components/feedback/Tooltip.jsx"},{"name":"Checkbox","sourcePath":"components/forms/Checkbox.jsx"},{"name":"Radio","sourcePath":"components/forms/Radio.jsx"},{"name":"Select","sourcePath":"components/forms/Select.jsx"},{"name":"Switch","sourcePath":"components/forms/Switch.jsx"},{"name":"TextField","sourcePath":"components/forms/TextField.jsx"},{"name":"Icon","sourcePath":"components/foundation/Icon.jsx"},{"name":"Tabs","sourcePath":"components/navigation/Tabs.jsx"},{"name":"AIFeedbackCard","sourcePath":"components/quiz/AIFeedbackCard.jsx"},{"name":"AnswerChoice","sourcePath":"components/quiz/AnswerChoice.jsx"},{"name":"ResultSummary","sourcePath":"components/quiz/ResultSummary.jsx"},{"name":"Card","sourcePath":"components/surfaces/Card.jsx"},{"name":"Dialog","sourcePath":"components/surfaces/Dialog.jsx"}],"sourceHashes":{"components/actions/Button.jsx":"629e98b790bd","components/actions/IconButton.jsx":"d1b3e3a0b44d","components/feedback/Badge.jsx":"5d0be34365c7","components/feedback/ProgressBar.jsx":"57eed2cb5edd","components/feedback/Toast.jsx":"20e56b075f15","components/feedback/Tooltip.jsx":"341899e8e524","components/forms/Checkbox.jsx":"f1f22399b29f","components/forms/Radio.jsx":"e92259cfa5ae","components/forms/Select.jsx":"ed8f7a939d48","components/forms/Switch.jsx":"0fe1eb5a8554","components/forms/TextField.jsx":"aa4115c1b5e6","components/foundation/Icon.jsx":"3edb3cd65b12","components/navigation/Tabs.jsx":"1ea51e40f650","components/quiz/AIFeedbackCard.jsx":"e861c9bd9c9e","components/quiz/AnswerChoice.jsx":"8b4d6601513c","components/quiz/ResultSummary.jsx":"8a6b5b480a02","components/surfaces/Card.jsx":"b3a2724513a3","components/surfaces/Dialog.jsx":"23233703b070","ui_kits/student/chrome.jsx":"02b3e36004f9","ui_kits/student/home.jsx":"525b8b6ca2c4","ui_kits/student/review.jsx":"03a75f1c79cf","ui_kits/student/take.jsx":"7ff685cea53b","ui_kits/teacher/chrome.jsx":"881b181fc4f6","ui_kits/teacher/dashboard.jsx":"9239df18a237","ui_kits/teacher/editor.jsx":"1ddbc164c852","ui_kits/teacher/results.jsx":"ee91cd3731f3"},"inlinedExternals":[],"unexposedExports":[]} */

(() => {

const __ds_ns = (window.QuiztinDesignSystem_138691 = window.QuiztinDesignSystem_138691 || {});

const __ds_scope = {};

(__ds_ns.__errors = __ds_ns.__errors || []);

// components/feedback/ProgressBar.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const CSS = `
.qz-progress { display: flex; flex-direction: column; gap: 8px; font-family: var(--font-body); }
.qz-progress__head { display: flex; justify-content: space-between; align-items: baseline; gap: 12px; }
.qz-progress__label { font: var(--type-label); color: var(--text-strong); }
.qz-progress__count { font: var(--type-caption); color: var(--text-muted); font-variant-numeric: tabular-nums; white-space: nowrap; }
.qz-progress__track { width: 100%; border-radius: var(--radius-pill); background: var(--sand-200); overflow: hidden; }
.qz-progress__fill { height: 100%; border-radius: var(--radius-pill); background: var(--primary); transition: width var(--duration-slow) var(--ease-out); }
.qz-progress--sm .qz-progress__track { height: 8px; }
.qz-progress--md .qz-progress__track { height: 12px; }
.qz-progress--lg .qz-progress__track { height: 16px; }
.qz-progress--accent .qz-progress__fill { background: var(--accent); }
.qz-progress--success .qz-progress__fill { background: var(--success); }
`;
if (typeof document !== 'undefined' && !document.getElementById('qz-progress-css')) {
  const el = document.createElement('style');
  el.id = 'qz-progress-css';
  el.textContent = CSS;
  document.head.appendChild(el);
}

/**
 * Calm, reassuring progress indicator — the backbone of the low-anxiety
 * take-quiz flow. Shows "Question X of Y" style counts and animates gently.
 */
function ProgressBar({
  value,
  max = 100,
  label,
  showCount = false,
  countFormat,
  tone = 'primary',
  size = 'md',
  className = '',
  ...rest
}) {
  const pct = Math.max(0, Math.min(100, value / max * 100));
  const count = countFormat ? countFormat(value, max) : `${value} of ${max}`;
  const cls = ['qz-progress', `qz-progress--${tone}`, `qz-progress--${size}`, className].filter(Boolean).join(' ');
  return /*#__PURE__*/React.createElement("div", _extends({
    className: cls
  }, rest), (label || showCount) && /*#__PURE__*/React.createElement("div", {
    className: "qz-progress__head"
  }, label && /*#__PURE__*/React.createElement("span", {
    className: "qz-progress__label"
  }, label), showCount && /*#__PURE__*/React.createElement("span", {
    className: "qz-progress__count"
  }, count)), /*#__PURE__*/React.createElement("div", {
    className: "qz-progress__track",
    role: "progressbar",
    "aria-valuenow": value,
    "aria-valuemin": 0,
    "aria-valuemax": max,
    "aria-label": label || 'Progress'
  }, /*#__PURE__*/React.createElement("div", {
    className: "qz-progress__fill",
    style: {
      width: `${pct}%`
    }
  })));
}
Object.assign(__ds_scope, { ProgressBar });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/feedback/ProgressBar.jsx", error: String((e && e.message) || e) }); }

// components/feedback/Tooltip.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const CSS = `
.qz-tip { position: relative; display: inline-flex; }
.qz-tip__bubble {
  position: absolute; z-index: 60; left: 50%; bottom: calc(100% + 9px); transform: translateX(-50%) translateY(4px);
  background: var(--surface-inverse); color: var(--text-on-inverse);
  font-family: var(--font-body); font-weight: 600; font-size: var(--text-xs); line-height: var(--leading-normal);
  padding: 8px 11px; border-radius: var(--radius-sm); box-shadow: var(--shadow-md);
  width: max-content; max-width: 240px; text-align: center; pointer-events: none;
  opacity: 0; transition: opacity var(--duration-fast) var(--ease-out), transform var(--duration-fast) var(--ease-out);
}
.qz-tip__bubble::after { content: ''; position: absolute; left: 50%; top: 100%; transform: translateX(-50%); border: 5px solid transparent; border-top-color: var(--surface-inverse); }
.qz-tip--bottom .qz-tip__bubble { bottom: auto; top: calc(100% + 9px); transform: translateX(-50%) translateY(-4px); }
.qz-tip--bottom .qz-tip__bubble::after { top: auto; bottom: 100%; border-top-color: transparent; border-bottom-color: var(--surface-inverse); }
.qz-tip:hover .qz-tip__bubble,
.qz-tip:focus-within .qz-tip__bubble { opacity: 1; transform: translateX(-50%) translateY(0); }
`;
if (typeof document !== 'undefined' && !document.getElementById('qz-tooltip-css')) {
  const el = document.createElement('style');
  el.id = 'qz-tooltip-css';
  el.textContent = CSS;
  document.head.appendChild(el);
}
let tipSeq = 0;

/**
 * Hover/focus tooltip. Wraps a single interactive child (button, icon) and
 * shows a small dark bubble. Appears on both hover AND keyboard focus.
 */
function Tooltip({
  content,
  placement = 'top',
  children,
  className = '',
  ...rest
}) {
  const id = React.useMemo(() => `qz-tip-${++tipSeq}`, []);
  const cls = ['qz-tip', `qz-tip--${placement}`, className].filter(Boolean).join(' ');
  const child = React.isValidElement(children) ? React.cloneElement(children, {
    'aria-describedby': id
  }) : children;
  return /*#__PURE__*/React.createElement("span", _extends({
    className: cls
  }, rest), child, /*#__PURE__*/React.createElement("span", {
    className: "qz-tip__bubble",
    role: "tooltip",
    id: id
  }, content));
}
Object.assign(__ds_scope, { Tooltip });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/feedback/Tooltip.jsx", error: String((e && e.message) || e) }); }

// components/forms/Radio.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const CSS = `
.qz-radio { display: inline-flex; align-items: flex-start; gap: 12px; cursor: pointer; font-family: var(--font-body); }
.qz-radio--disabled { cursor: not-allowed; opacity: 0.55; }
.qz-radio__input { position: absolute; width: 1px; height: 1px; opacity: 0; margin: 0; }
.qz-radio__box {
  flex: none; width: 24px; height: 24px; border-radius: 50%;
  border: 2px solid var(--sand-300); background: var(--surface-card);
  display: inline-flex; align-items: center; justify-content: center; margin-top: 1px;
  transition: var(--transition-colors), box-shadow var(--duration-fast) var(--ease-out);
}
.qz-radio__dot { width: 10px; height: 10px; border-radius: 50%; background: #fff; opacity: 0; transform: scale(0.4); transition: opacity var(--duration-fast) var(--ease-out), transform var(--duration-base) var(--ease-spring); }
.qz-radio:hover .qz-radio__input:not(:disabled):not(:checked) + .qz-radio__box { border-color: var(--sand-400); }
.qz-radio__input:checked + .qz-radio__box { background: var(--primary); border-color: var(--primary); }
.qz-radio__input:checked + .qz-radio__box .qz-radio__dot { opacity: 1; transform: scale(1); }
.qz-radio__input:focus-visible + .qz-radio__box { box-shadow: var(--focus-ring-shadow); }
.qz-radio__text { display: flex; flex-direction: column; gap: 2px; }
.qz-radio__label { font: var(--type-body-strong); color: var(--text-strong); }
.qz-radio__desc { font: var(--type-caption); color: var(--text-muted); }
`;
if (typeof document !== 'undefined' && !document.getElementById('qz-radio-css')) {
  const el = document.createElement('style');
  el.id = 'qz-radio-css';
  el.textContent = CSS;
  document.head.appendChild(el);
}

/**
 * Single radio option. Group several by giving them the same `name`. Use for
 * mutually-exclusive settings; for quiz answers use {@link AnswerChoice}.
 */
function Radio({
  label,
  description,
  id,
  disabled = false,
  className = '',
  ...rest
}) {
  const autoId = React.useId();
  const fieldId = id || autoId;
  const cls = ['qz-radio', disabled && 'qz-radio--disabled', className].filter(Boolean).join(' ');
  return /*#__PURE__*/React.createElement("label", {
    className: cls,
    htmlFor: fieldId
  }, /*#__PURE__*/React.createElement("input", _extends({
    type: "radio",
    id: fieldId,
    className: "qz-radio__input",
    disabled: disabled
  }, rest)), /*#__PURE__*/React.createElement("span", {
    className: "qz-radio__box"
  }, /*#__PURE__*/React.createElement("span", {
    className: "qz-radio__dot"
  })), (label || description) && /*#__PURE__*/React.createElement("span", {
    className: "qz-radio__text"
  }, label && /*#__PURE__*/React.createElement("span", {
    className: "qz-radio__label"
  }, label), description && /*#__PURE__*/React.createElement("span", {
    className: "qz-radio__desc"
  }, description)));
}
Object.assign(__ds_scope, { Radio });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/forms/Radio.jsx", error: String((e && e.message) || e) }); }

// components/forms/Switch.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const CSS = `
.qz-switch { display: inline-flex; align-items: center; gap: 12px; cursor: pointer; font-family: var(--font-body); }
.qz-switch--disabled { cursor: not-allowed; opacity: 0.55; }
.qz-switch__input { position: absolute; width: 1px; height: 1px; opacity: 0; margin: 0; }
.qz-switch__track {
  flex: none; width: 46px; height: 28px; border-radius: var(--radius-pill);
  background: var(--sand-300); position: relative;
  transition: background-color var(--duration-base) var(--ease-out), box-shadow var(--duration-fast) var(--ease-out);
}
.qz-switch__thumb {
  position: absolute; top: 3px; left: 3px; width: 22px; height: 22px; border-radius: 50%;
  background: #fff; box-shadow: var(--shadow-sm);
  transition: transform var(--duration-base) var(--ease-spring);
}
.qz-switch:hover .qz-switch__input:not(:disabled):not(:checked) + .qz-switch__track { background: var(--sand-400); }
.qz-switch__input:checked + .qz-switch__track { background: var(--primary); }
.qz-switch__input:checked + .qz-switch__track .qz-switch__thumb { transform: translateX(18px); }
.qz-switch__input:focus-visible + .qz-switch__track { box-shadow: var(--focus-ring-shadow); }
.qz-switch__text { display: flex; flex-direction: column; gap: 2px; }
.qz-switch__label { font: var(--type-body-strong); color: var(--text-strong); }
.qz-switch__desc { font: var(--type-caption); color: var(--text-muted); }
`;
if (typeof document !== 'undefined' && !document.getElementById('qz-switch-css')) {
  const el = document.createElement('style');
  el.id = 'qz-switch-css';
  el.textContent = CSS;
  document.head.appendChild(el);
}

/**
 * On/off toggle for immediate settings (e.g. "Allow retakes"). Uses role=switch
 * so assistive tech announces on/off state. Prefer over Checkbox when the change
 * takes effect instantly rather than on form submit.
 */
function Switch({
  label,
  description,
  id,
  disabled = false,
  className = '',
  ...rest
}) {
  const autoId = React.useId();
  const fieldId = id || autoId;
  const cls = ['qz-switch', disabled && 'qz-switch--disabled', className].filter(Boolean).join(' ');
  return /*#__PURE__*/React.createElement("label", {
    className: cls,
    htmlFor: fieldId
  }, /*#__PURE__*/React.createElement("input", _extends({
    type: "checkbox",
    role: "switch",
    id: fieldId,
    className: "qz-switch__input",
    disabled: disabled
  }, rest)), /*#__PURE__*/React.createElement("span", {
    className: "qz-switch__track"
  }, /*#__PURE__*/React.createElement("span", {
    className: "qz-switch__thumb"
  })), (label || description) && /*#__PURE__*/React.createElement("span", {
    className: "qz-switch__text"
  }, label && /*#__PURE__*/React.createElement("span", {
    className: "qz-switch__label"
  }, label), description && /*#__PURE__*/React.createElement("span", {
    className: "qz-switch__desc"
  }, description)));
}
Object.assign(__ds_scope, { Switch });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/forms/Switch.jsx", error: String((e && e.message) || e) }); }

// components/foundation/Icon.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
/**
 * Icon — thin wrapper over Phosphor Icons (web font).
 * Consumers must load the Phosphor stylesheet(s) once, e.g.
 *   <link rel="stylesheet" href="https://unpkg.com/@phosphor-icons/web@2.1.1/src/regular/style.css">
 * (plus bold/fill/duotone weights as needed).
 */
function Icon({
  name,
  weight = 'regular',
  size = '1.25em',
  color,
  label,
  className = '',
  style,
  ...rest
}) {
  const weightClass = weight === 'regular' ? 'ph' : `ph-${weight}`;
  const a11y = label ? {
    role: 'img',
    'aria-label': label
  } : {
    'aria-hidden': true
  };
  return /*#__PURE__*/React.createElement("i", _extends({
    className: `${weightClass} ph-${name} ${className}`.trim(),
    style: {
      fontSize: typeof size === 'number' ? `${size}px` : size,
      color: color || 'inherit',
      lineHeight: 1,
      display: 'inline-flex',
      flex: 'none',
      ...style
    }
  }, a11y, rest));
}
Object.assign(__ds_scope, { Icon });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/foundation/Icon.jsx", error: String((e && e.message) || e) }); }

// components/actions/Button.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const CSS = `
.qz-btn {
  display: inline-flex; align-items: center; justify-content: center; gap: 8px;
  font-family: var(--font-body); font-weight: 700; line-height: 1;
  border: 1.5px solid transparent; border-radius: var(--radius-button);
  cursor: pointer; text-decoration: none; white-space: nowrap; user-select: none;
  min-height: var(--tap-min);
  transition: transform var(--duration-fast) var(--ease-out),
              background-color var(--duration-fast) var(--ease-out),
              border-color var(--duration-fast) var(--ease-out),
              color var(--duration-fast) var(--ease-out),
              box-shadow var(--duration-fast) var(--ease-out);
}
.qz-btn:focus-visible { outline: none; box-shadow: var(--focus-ring-shadow); }
.qz-btn:disabled, .qz-btn[aria-disabled="true"] { cursor: not-allowed; opacity: 0.5; box-shadow: none; transform: none; }
.qz-btn:not(:disabled):not([aria-disabled="true"]):active { transform: scale(var(--press-scale)); }

.qz-btn--sm { font-size: var(--text-sm); padding: 0 14px; min-height: 36px; border-radius: var(--radius-sm); gap: 6px; }
.qz-btn--md { font-size: var(--text-base); padding: 0 20px; }
.qz-btn--lg { font-size: var(--text-lg); padding: 0 28px; min-height: 54px; border-radius: var(--radius-lg); }
.qz-btn--block { width: 100%; }

.qz-btn--primary { background: var(--primary); color: var(--text-on-primary); box-shadow: var(--shadow-primary); }
.qz-btn--primary:not(:disabled):hover { background: var(--primary-hover); }
.qz-btn--primary:not(:disabled):active { background: var(--primary-press); }

.qz-btn--accent { background: var(--accent); color: var(--text-on-accent); box-shadow: var(--shadow-accent); }
.qz-btn--accent:not(:disabled):hover { background: var(--accent-hover); }

.qz-btn--secondary { background: var(--surface-card); color: var(--text-strong); border-color: var(--border-strong); box-shadow: var(--shadow-xs); }
.qz-btn--secondary:not(:disabled):hover { background: var(--sand-50); border-color: var(--sand-400); }

.qz-btn--subtle { background: var(--sand-100); color: var(--text-strong); }
.qz-btn--subtle:not(:disabled):hover { background: var(--sand-200); }

.qz-btn--ghost { background: transparent; color: var(--primary-text); }
.qz-btn--ghost:not(:disabled):hover { background: var(--primary-softer); }

.qz-btn--danger { background: var(--danger); color: #fff; }
.qz-btn--danger:not(:disabled):hover { background: var(--rose-700); }

.qz-btn__spinner {
  width: 1.05em; height: 1.05em; border-radius: 50%;
  border: 2px solid currentColor; border-top-color: transparent;
  animation: qz-btn-spin 0.6s linear infinite;
}
@keyframes qz-btn-spin { to { transform: rotate(360deg); } }
@media (prefers-reduced-motion: reduce) { .qz-btn__spinner { animation-duration: 1.4s; } }
`;
if (typeof document !== 'undefined' && !document.getElementById('qz-button-css')) {
  const el = document.createElement('style');
  el.id = 'qz-button-css';
  el.textContent = CSS;
  document.head.appendChild(el);
}

/**
 * Primary interactive control. Blueberry `primary` for the main action on a
 * view, `accent` (coral) for warm/brand moments, `secondary`/`subtle`/`ghost`
 * for lower emphasis. Only one primary per view.
 */
function Button({
  children,
  variant = 'primary',
  size = 'md',
  icon,
  iconLeft,
  iconRight,
  fullWidth = false,
  loading = false,
  disabled = false,
  type = 'button',
  as,
  href,
  className = '',
  ...rest
}) {
  const Tag = as === 'a' || href ? 'a' : 'button';
  const left = iconLeft || icon;
  const isDisabled = disabled || loading;
  const cls = ['qz-btn', `qz-btn--${variant}`, `qz-btn--${size}`, fullWidth && 'qz-btn--block', className].filter(Boolean).join(' ');
  const inner = /*#__PURE__*/React.createElement(React.Fragment, null, loading && /*#__PURE__*/React.createElement("span", {
    className: "qz-btn__spinner",
    "aria-hidden": "true"
  }), !loading && left && /*#__PURE__*/React.createElement(__ds_scope.Icon, {
    name: left,
    weight: "bold",
    size: "1.15em"
  }), children != null && /*#__PURE__*/React.createElement("span", null, children), !loading && iconRight && /*#__PURE__*/React.createElement(__ds_scope.Icon, {
    name: iconRight,
    weight: "bold",
    size: "1.15em"
  }));
  if (Tag === 'a') {
    return /*#__PURE__*/React.createElement("a", _extends({
      className: cls,
      href: isDisabled ? undefined : href,
      role: "button",
      "aria-disabled": isDisabled || undefined,
      tabIndex: isDisabled ? -1 : undefined
    }, rest), inner);
  }
  return /*#__PURE__*/React.createElement("button", _extends({
    className: cls,
    type: type,
    disabled: isDisabled,
    "aria-busy": loading || undefined
  }, rest), inner);
}
Object.assign(__ds_scope, { Button });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/actions/Button.jsx", error: String((e && e.message) || e) }); }

// components/actions/IconButton.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const CSS = `
.qz-iconbtn {
  display: inline-flex; align-items: center; justify-content: center;
  border: 1.5px solid transparent; border-radius: var(--radius-md);
  cursor: pointer; padding: 0; color: var(--text-body); background: transparent;
  transition: transform var(--duration-fast) var(--ease-out),
              background-color var(--duration-fast) var(--ease-out),
              border-color var(--duration-fast) var(--ease-out),
              color var(--duration-fast) var(--ease-out),
              box-shadow var(--duration-fast) var(--ease-out);
}
.qz-iconbtn:focus-visible { outline: none; box-shadow: var(--focus-ring-shadow); }
.qz-iconbtn:disabled { cursor: not-allowed; opacity: 0.5; }
.qz-iconbtn:not(:disabled):active { transform: scale(var(--press-scale)); }

.qz-iconbtn--sm { width: 34px; height: 34px; font-size: 18px; border-radius: var(--radius-sm); }
.qz-iconbtn--md { width: 44px; height: 44px; font-size: 20px; }
.qz-iconbtn--lg { width: 52px; height: 52px; font-size: 24px; border-radius: var(--radius-lg); }

.qz-iconbtn--ghost:not(:disabled):hover { background: var(--sand-100); color: var(--text-strong); }
.qz-iconbtn--secondary { background: var(--surface-card); border-color: var(--border-strong); box-shadow: var(--shadow-xs); color: var(--text-strong); }
.qz-iconbtn--secondary:not(:disabled):hover { background: var(--sand-50); border-color: var(--sand-400); }
.qz-iconbtn--primary { background: var(--primary); color: #fff; }
.qz-iconbtn--primary:not(:disabled):hover { background: var(--primary-hover); }
.qz-iconbtn--accent { background: var(--accent); color: #fff; }
.qz-iconbtn--accent:not(:disabled):hover { background: var(--accent-hover); }
.qz-iconbtn--danger:not(:disabled):hover { background: var(--danger-soft); color: var(--danger-text); }
`;
if (typeof document !== 'undefined' && !document.getElementById('qz-iconbutton-css')) {
  const el = document.createElement('style');
  el.id = 'qz-iconbutton-css';
  el.textContent = CSS;
  document.head.appendChild(el);
}

/**
 * A square, icon-only control for toolbars and dense rows. Always requires a
 * `label` for screen-reader users (also used as the tooltip title).
 */
function IconButton({
  icon,
  label,
  variant = 'ghost',
  size = 'md',
  weight = 'regular',
  disabled = false,
  className = '',
  ...rest
}) {
  const cls = ['qz-iconbtn', `qz-iconbtn--${variant}`, `qz-iconbtn--${size}`, className].filter(Boolean).join(' ');
  return /*#__PURE__*/React.createElement("button", _extends({
    type: "button",
    className: cls,
    "aria-label": label,
    title: label,
    disabled: disabled
  }, rest), /*#__PURE__*/React.createElement(__ds_scope.Icon, {
    name: icon,
    weight: weight,
    size: "1em"
  }));
}
Object.assign(__ds_scope, { IconButton });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/actions/IconButton.jsx", error: String((e && e.message) || e) }); }

// components/feedback/Badge.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const CSS = `
.qz-badge { display: inline-flex; align-items: center; gap: 6px; font-family: var(--font-body); font-weight: 700; border-radius: var(--radius-pill); border: 1px solid transparent; white-space: nowrap; line-height: 1; }
.qz-badge--sm { font-size: var(--text-2xs); padding: 4px 9px; letter-spacing: var(--tracking-wide); text-transform: uppercase; }
.qz-badge--md { font-size: var(--text-xs); padding: 6px 12px; }
.qz-badge__dot { width: 7px; height: 7px; border-radius: 50%; background: currentColor; flex: none; }

.qz-badge--neutral { background: var(--sand-100); color: var(--text-muted); border-color: var(--sand-200); }
.qz-badge--primary { background: var(--primary-soft); color: var(--blueberry-800); }
.qz-badge--accent  { background: var(--accent-soft); color: var(--coral-800); }
.qz-badge--success { background: var(--success-soft); color: var(--success-text); }
.qz-badge--danger  { background: var(--danger-soft); color: var(--danger-text); }
.qz-badge--warning { background: var(--warning-soft); color: var(--warning-text); }

.qz-badge--solid { border-color: transparent; color: #fff; }
.qz-badge--solid.qz-badge--neutral { background: var(--sand-600); }
.qz-badge--solid.qz-badge--primary { background: var(--primary); }
.qz-badge--solid.qz-badge--accent  { background: var(--accent); }
.qz-badge--solid.qz-badge--success { background: var(--success); }
.qz-badge--solid.qz-badge--danger  { background: var(--danger); }
.qz-badge--solid.qz-badge--warning { background: var(--warning); }
`;
if (typeof document !== 'undefined' && !document.getElementById('qz-badge-css')) {
  const el = document.createElement('style');
  el.id = 'qz-badge-css';
  el.textContent = CSS;
  document.head.appendChild(el);
}

/**
 * Small pill for status, counts, and tags. Soft (tinted) by default; `solid`
 * for stronger emphasis. Also covers the "tag" role — no separate Tag component.
 */
function Badge({
  children,
  tone = 'neutral',
  size = 'md',
  dot = false,
  icon,
  solid = false,
  className = '',
  ...rest
}) {
  const cls = ['qz-badge', `qz-badge--${tone}`, `qz-badge--${size}`, solid && 'qz-badge--solid', className].filter(Boolean).join(' ');
  return /*#__PURE__*/React.createElement("span", _extends({
    className: cls
  }, rest), dot && /*#__PURE__*/React.createElement("span", {
    className: "qz-badge__dot"
  }), icon && /*#__PURE__*/React.createElement(__ds_scope.Icon, {
    name: icon,
    weight: "bold",
    size: "1.05em"
  }), children);
}
Object.assign(__ds_scope, { Badge });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/feedback/Badge.jsx", error: String((e && e.message) || e) }); }

// components/feedback/Toast.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const CSS = `
.qz-toast {
  display: flex; gap: 12px; align-items: flex-start; padding: 14px 16px;
  border-radius: var(--radius-md); background: var(--surface-card);
  border: 1px solid var(--border); box-shadow: var(--shadow-lg);
  font-family: var(--font-body); width: 100%; max-width: 420px;
}
.qz-toast__icon { font-size: 22px; flex: none; margin-top: 1px; display: flex; }
.qz-toast__body { display: flex; flex-direction: column; gap: 2px; flex: 1; min-width: 0; }
.qz-toast__title { font: var(--type-body-strong); color: var(--text-strong); }
.qz-toast__msg { font: var(--type-caption); color: var(--text-muted); line-height: var(--leading-normal); }
.qz-toast__close {
  flex: none; border: 0; background: transparent; cursor: pointer; color: var(--text-subtle);
  width: 28px; height: 28px; border-radius: var(--radius-sm); display: inline-flex; align-items: center; justify-content: center;
  margin: -3px -4px 0 0; transition: var(--transition-colors);
}
.qz-toast__close:hover { background: var(--sand-100); color: var(--text-strong); }
.qz-toast__close:focus-visible { outline: none; box-shadow: var(--focus-ring-shadow); }

.qz-toast--info    .qz-toast__icon { color: var(--primary); }
.qz-toast--success .qz-toast__icon { color: var(--success); }
.qz-toast--danger  .qz-toast__icon { color: var(--danger); }
.qz-toast--warning .qz-toast__icon { color: var(--warning); }
.qz-toast--ai { background: var(--ai-surface); border-color: var(--ai-border); }
.qz-toast--ai .qz-toast__icon { color: var(--ai-accent); }
`;
if (typeof document !== 'undefined' && !document.getElementById('qz-toast-css')) {
  const el = document.createElement('style');
  el.id = 'qz-toast-css';
  el.textContent = CSS;
  document.head.appendChild(el);
}
const DEFAULT_ICON = {
  info: 'info',
  success: 'check-circle',
  danger: 'x-circle',
  warning: 'warning',
  ai: 'sparkle'
};

/**
 * A lightweight notification / inline alert. The consumer positions it (toast
 * stack, inline in a form, etc.). Uses role=status so it's announced politely.
 */
function Toast({
  tone = 'info',
  title,
  children,
  icon,
  onClose,
  className = '',
  ...rest
}) {
  const cls = ['qz-toast', `qz-toast--${tone}`, className].filter(Boolean).join(' ');
  return /*#__PURE__*/React.createElement("div", _extends({
    className: cls,
    role: tone === 'danger' ? 'alert' : 'status'
  }, rest), /*#__PURE__*/React.createElement("span", {
    className: "qz-toast__icon"
  }, /*#__PURE__*/React.createElement(__ds_scope.Icon, {
    name: icon || DEFAULT_ICON[tone],
    weight: "fill"
  })), /*#__PURE__*/React.createElement("div", {
    className: "qz-toast__body"
  }, title && /*#__PURE__*/React.createElement("span", {
    className: "qz-toast__title"
  }, title), children && /*#__PURE__*/React.createElement("span", {
    className: "qz-toast__msg"
  }, children)), onClose && /*#__PURE__*/React.createElement("button", {
    type: "button",
    className: "qz-toast__close",
    "aria-label": "Dismiss",
    onClick: onClose
  }, /*#__PURE__*/React.createElement(__ds_scope.Icon, {
    name: "x",
    weight: "bold",
    size: "16px"
  })));
}
Object.assign(__ds_scope, { Toast });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/feedback/Toast.jsx", error: String((e && e.message) || e) }); }

// components/forms/Checkbox.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const CSS = `
.qz-check { display: inline-flex; align-items: flex-start; gap: 12px; cursor: pointer; font-family: var(--font-body); }
.qz-check--disabled { cursor: not-allowed; opacity: 0.55; }
.qz-check__input { position: absolute; width: 1px; height: 1px; opacity: 0; margin: 0; }
.qz-check__box {
  flex: none; width: 24px; height: 24px; border-radius: 8px;
  border: 2px solid var(--sand-300); background: var(--surface-card);
  display: inline-flex; align-items: center; justify-content: center; color: #fff; margin-top: 1px;
  transition: var(--transition-colors), box-shadow var(--duration-fast) var(--ease-out);
}
.qz-check__box .ph { opacity: 0; transform: scale(0.5); transition: opacity var(--duration-fast) var(--ease-out), transform var(--duration-base) var(--ease-spring); }
.qz-check:hover .qz-check__input:not(:disabled):not(:checked) + .qz-check__box { border-color: var(--sand-400); }
.qz-check__input:checked + .qz-check__box { background: var(--primary); border-color: var(--primary); }
.qz-check__input:checked + .qz-check__box .ph { opacity: 1; transform: scale(1); }
.qz-check__input:focus-visible + .qz-check__box { box-shadow: var(--focus-ring-shadow); }
.qz-check__text { display: flex; flex-direction: column; gap: 2px; }
.qz-check__label { font: var(--type-body-strong); color: var(--text-strong); }
.qz-check__desc { font: var(--type-caption); color: var(--text-muted); }
`;
if (typeof document !== 'undefined' && !document.getElementById('qz-checkbox-css')) {
  const el = document.createElement('style');
  el.id = 'qz-checkbox-css';
  el.textContent = CSS;
  document.head.appendChild(el);
}

/**
 * Checkbox with an optional label + description. Uses a hidden native input for
 * full keyboard / screen-reader support; the box is a styled sibling.
 */
function Checkbox({
  label,
  description,
  id,
  disabled = false,
  className = '',
  ...rest
}) {
  const autoId = React.useId();
  const fieldId = id || autoId;
  const cls = ['qz-check', disabled && 'qz-check--disabled', className].filter(Boolean).join(' ');
  return /*#__PURE__*/React.createElement("label", {
    className: cls,
    htmlFor: fieldId
  }, /*#__PURE__*/React.createElement("input", _extends({
    type: "checkbox",
    id: fieldId,
    className: "qz-check__input",
    disabled: disabled
  }, rest)), /*#__PURE__*/React.createElement("span", {
    className: "qz-check__box"
  }, /*#__PURE__*/React.createElement(__ds_scope.Icon, {
    name: "check",
    weight: "bold",
    size: "0.8em"
  })), (label || description) && /*#__PURE__*/React.createElement("span", {
    className: "qz-check__text"
  }, label && /*#__PURE__*/React.createElement("span", {
    className: "qz-check__label"
  }, label), description && /*#__PURE__*/React.createElement("span", {
    className: "qz-check__desc"
  }, description)));
}
Object.assign(__ds_scope, { Checkbox });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/forms/Checkbox.jsx", error: String((e && e.message) || e) }); }

// components/forms/Select.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const CSS = `
.qz-select { display: flex; flex-direction: column; gap: 6px; font-family: var(--font-body); }
.qz-select__label { font: var(--type-label); color: var(--text-strong); display: flex; gap: 6px; align-items: baseline; }
.qz-select__req { color: var(--accent); font-weight: 800; }
.qz-select__wrap { position: relative; display: flex; align-items: center; }
.qz-select__control {
  width: 100%; font-family: var(--font-body); font-size: var(--text-base); font-weight: 600; color: var(--text-strong);
  background: var(--surface-card); border: 1.5px solid var(--border-field);
  border-radius: var(--radius-input); padding: 12px 44px 12px 14px; min-height: var(--tap-min);
  appearance: none; -webkit-appearance: none; cursor: pointer;
  transition: var(--transition-colors), box-shadow var(--duration-fast) var(--ease-out);
}
.qz-select__control:hover:not(:disabled) { border-color: var(--sand-400); }
.qz-select__control:focus { outline: none; border-color: var(--primary); box-shadow: var(--focus-ring-shadow); }
.qz-select__control:disabled { background: var(--sand-50); color: var(--text-disabled); cursor: not-allowed; }
.qz-select__control:required:invalid { color: var(--text-subtle); font-weight: 400; }
.qz-select__caret { position: absolute; right: 14px; color: var(--text-muted); font-size: 18px; pointer-events: none; display: flex; }
.qz-select--error .qz-select__control { border-color: var(--danger); }
.qz-select__hint { font: var(--type-caption); color: var(--text-muted); }
.qz-select__hint--error { color: var(--danger-text); display: inline-flex; gap: 6px; align-items: center; }
`;
if (typeof document !== 'undefined' && !document.getElementById('qz-select-css')) {
  const el = document.createElement('style');
  el.id = 'qz-select-css';
  el.textContent = CSS;
  document.head.appendChild(el);
}

/**
 * Styled native <select>. Accepts either an `options` array or <option>
 * children. A `placeholder` renders a disabled leading option.
 */
function Select({
  label,
  hint,
  error,
  options,
  placeholder,
  required = false,
  id,
  className = '',
  children,
  ...rest
}) {
  const autoId = React.useId();
  const fieldId = id || autoId;
  const hintId = `${fieldId}-hint`;
  const cls = ['qz-select', error && 'qz-select--error', className].filter(Boolean).join(' ');
  return /*#__PURE__*/React.createElement("div", {
    className: cls
  }, label && /*#__PURE__*/React.createElement("label", {
    className: "qz-select__label",
    htmlFor: fieldId
  }, label, required && /*#__PURE__*/React.createElement("span", {
    className: "qz-select__req",
    "aria-hidden": "true"
  }, "*")), /*#__PURE__*/React.createElement("div", {
    className: "qz-select__wrap"
  }, /*#__PURE__*/React.createElement("select", _extends({
    id: fieldId,
    className: "qz-select__control",
    "aria-invalid": error ? true : undefined,
    "aria-describedby": hint || error ? hintId : undefined,
    required: required,
    defaultValue: placeholder != null && rest.value === undefined && rest.defaultValue === undefined ? '' : undefined
  }, rest), placeholder != null && /*#__PURE__*/React.createElement("option", {
    value: "",
    disabled: true
  }, placeholder), options ? options.map(o => /*#__PURE__*/React.createElement("option", {
    key: o.value,
    value: o.value,
    disabled: o.disabled
  }, o.label)) : children), /*#__PURE__*/React.createElement("span", {
    className: "qz-select__caret"
  }, /*#__PURE__*/React.createElement(__ds_scope.Icon, {
    name: "caret-down",
    weight: "bold"
  }))), (error || hint) && /*#__PURE__*/React.createElement("span", {
    id: hintId,
    className: `qz-select__hint${error ? ' qz-select__hint--error' : ''}`
  }, error ? /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(__ds_scope.Icon, {
    name: "warning-circle",
    weight: "fill",
    size: "1em"
  }), error) : hint));
}
Object.assign(__ds_scope, { Select });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/forms/Select.jsx", error: String((e && e.message) || e) }); }

// components/forms/TextField.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const CSS = `
.qz-field { display: flex; flex-direction: column; gap: 6px; font-family: var(--font-body); }
.qz-field__label { font: var(--type-label); color: var(--text-strong); display: flex; gap: 6px; align-items: baseline; }
.qz-field__req { color: var(--accent); font-weight: 800; }
.qz-field__optional { font-weight: 400; color: var(--text-subtle); font-size: var(--text-xs); }
.qz-field__wrap { position: relative; display: flex; align-items: center; }
.qz-field__icon { position: absolute; left: 14px; top: 13px; color: var(--text-subtle); font-size: 19px; pointer-events: none; display: flex; }
.qz-field__control {
  width: 100%; font-family: var(--font-body); font-size: var(--text-base); color: var(--text-strong);
  background: var(--surface-card); border: 1.5px solid var(--border-field);
  border-radius: var(--radius-input); padding: 12px 14px; min-height: var(--tap-min);
  transition: var(--transition-colors), box-shadow var(--duration-fast) var(--ease-out);
}
.qz-field__control::placeholder { color: var(--text-subtle); }
.qz-field__wrap--icon .qz-field__control { padding-left: 44px; }
.qz-field__control:hover:not(:disabled) { border-color: var(--sand-400); }
.qz-field__control:focus { outline: none; border-color: var(--primary); box-shadow: var(--focus-ring-shadow); }
.qz-field__control:disabled { background: var(--sand-50); color: var(--text-disabled); cursor: not-allowed; }
textarea.qz-field__control { min-height: 108px; resize: vertical; line-height: var(--leading-normal); }
.qz-field--error .qz-field__control { border-color: var(--danger); }
.qz-field--error .qz-field__control:focus { box-shadow: 0 0 0 4px rgba(200, 64, 78, 0.24); }
.qz-field__hint { font: var(--type-caption); color: var(--text-muted); }
.qz-field__hint--error { color: var(--danger-text); display: inline-flex; gap: 6px; align-items: center; }
`;
if (typeof document !== 'undefined' && !document.getElementById('qz-textfield-css')) {
  const el = document.createElement('style');
  el.id = 'qz-textfield-css';
  el.textContent = CSS;
  document.head.appendChild(el);
}

/**
 * Text input with a friendly label, optional leading icon, and gentle helper /
 * error messaging. Set `multiline` for a textarea. Error text is announced via
 * aria-describedby + aria-invalid.
 */
function TextField({
  label,
  hint,
  error,
  icon,
  multiline = false,
  required = false,
  optional = false,
  id,
  className = '',
  ...rest
}) {
  const autoId = React.useId();
  const fieldId = id || autoId;
  const hintId = `${fieldId}-hint`;
  const Control = multiline ? 'textarea' : 'input';
  const cls = ['qz-field', error && 'qz-field--error', className].filter(Boolean).join(' ');
  return /*#__PURE__*/React.createElement("div", {
    className: cls
  }, label && /*#__PURE__*/React.createElement("label", {
    className: "qz-field__label",
    htmlFor: fieldId
  }, label, required && /*#__PURE__*/React.createElement("span", {
    className: "qz-field__req",
    "aria-hidden": "true"
  }, "*"), optional && /*#__PURE__*/React.createElement("span", {
    className: "qz-field__optional"
  }, "Optional")), /*#__PURE__*/React.createElement("div", {
    className: `qz-field__wrap${icon ? ' qz-field__wrap--icon' : ''}`
  }, icon && /*#__PURE__*/React.createElement("span", {
    className: "qz-field__icon"
  }, /*#__PURE__*/React.createElement(__ds_scope.Icon, {
    name: icon
  })), /*#__PURE__*/React.createElement(Control, _extends({
    id: fieldId,
    className: "qz-field__control",
    "aria-invalid": error ? true : undefined,
    "aria-describedby": hint || error ? hintId : undefined,
    required: required
  }, rest))), (error || hint) && /*#__PURE__*/React.createElement("span", {
    id: hintId,
    className: `qz-field__hint${error ? ' qz-field__hint--error' : ''}`
  }, error ? /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(__ds_scope.Icon, {
    name: "warning-circle",
    weight: "fill",
    size: "1em"
  }), error) : hint));
}
Object.assign(__ds_scope, { TextField });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/forms/TextField.jsx", error: String((e && e.message) || e) }); }

// components/navigation/Tabs.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const CSS = `
.qz-tabs { display: inline-flex; font-family: var(--font-body); max-width: 100%; }
.qz-tab { border: 0; background: transparent; cursor: pointer; display: inline-flex; align-items: center; gap: 8px; font-weight: 700; color: var(--text-muted); white-space: nowrap; transition: var(--transition-colors); }
.qz-tab:focus-visible { outline: none; box-shadow: var(--focus-ring-shadow); }
.qz-tab__badge { font-size: var(--text-2xs); font-weight: 700; background: var(--sand-200); color: var(--text-muted); border-radius: var(--radius-pill); padding: 1px 7px; min-width: 18px; text-align: center; }

.qz-tabs--underline { gap: 2px; border-bottom: 2px solid var(--border); overflow-x: auto; }
.qz-tabs--underline .qz-tab { position: relative; padding: 11px 14px; font-size: var(--text-base); border-radius: var(--radius-sm) var(--radius-sm) 0 0; }
.qz-tabs--underline .qz-tab::after { content: ''; position: absolute; left: 10px; right: 10px; bottom: -2px; height: 3px; border-radius: 3px 3px 0 0; background: transparent; transition: background-color var(--duration-fast) var(--ease-out); }
.qz-tabs--underline .qz-tab:hover { color: var(--text-strong); }
.qz-tabs--underline .qz-tab[aria-selected="true"] { color: var(--primary-text); }
.qz-tabs--underline .qz-tab[aria-selected="true"]::after { background: var(--primary); }
.qz-tabs--underline .qz-tab[aria-selected="true"] .qz-tab__badge { background: var(--primary-soft); color: var(--blueberry-800); }

.qz-tabs--pill { gap: 3px; padding: 4px; background: var(--sand-100); border-radius: var(--radius-pill); }
.qz-tabs--pill .qz-tab { padding: 8px 16px; border-radius: var(--radius-pill); font-size: var(--text-sm); }
.qz-tabs--pill .qz-tab:hover { color: var(--text-strong); }
.qz-tabs--pill .qz-tab[aria-selected="true"] { background: var(--surface-card); color: var(--text-strong); box-shadow: var(--shadow-xs); }
`;
if (typeof document !== 'undefined' && !document.getElementById('qz-tabs-css')) {
  const el = document.createElement('style');
  el.id = 'qz-tabs-css';
  el.textContent = CSS;
  document.head.appendChild(el);
}

/**
 * Tab bar with roving-focus keyboard support (←/→). Controlled (`value`) or
 * uncontrolled (`defaultValue`). Renders the tab strip only — you render the
 * panel for the active id.
 */
function Tabs({
  tabs,
  value,
  defaultValue,
  onChange,
  variant = 'underline',
  className = '',
  ...rest
}) {
  const [internal, setInternal] = React.useState(defaultValue ?? (tabs[0] && tabs[0].id));
  const active = value !== undefined ? value : internal;
  const refs = React.useRef([]);
  const select = id => {
    if (value === undefined) setInternal(id);
    onChange && onChange(id);
  };
  const onKeyDown = (e, i) => {
    if (e.key !== 'ArrowRight' && e.key !== 'ArrowLeft' && e.key !== 'Home' && e.key !== 'End') return;
    e.preventDefault();
    let next = i;
    if (e.key === 'ArrowRight') next = (i + 1) % tabs.length;else if (e.key === 'ArrowLeft') next = (i - 1 + tabs.length) % tabs.length;else if (e.key === 'Home') next = 0;else if (e.key === 'End') next = tabs.length - 1;
    refs.current[next] && refs.current[next].focus();
    select(tabs[next].id);
  };
  const cls = ['qz-tabs', `qz-tabs--${variant}`, className].filter(Boolean).join(' ');
  return /*#__PURE__*/React.createElement("div", _extends({
    className: cls,
    role: "tablist"
  }, rest), tabs.map((t, i) => /*#__PURE__*/React.createElement("button", {
    key: t.id,
    ref: el => {
      refs.current[i] = el;
    },
    type: "button",
    role: "tab",
    "aria-selected": active === t.id,
    tabIndex: active === t.id ? 0 : -1,
    className: "qz-tab",
    onClick: () => select(t.id),
    onKeyDown: e => onKeyDown(e, i)
  }, t.icon && /*#__PURE__*/React.createElement(__ds_scope.Icon, {
    name: t.icon,
    weight: active === t.id ? 'fill' : 'regular',
    size: "1.2em"
  }), t.label, t.badge != null && /*#__PURE__*/React.createElement("span", {
    className: "qz-tab__badge"
  }, t.badge))));
}
Object.assign(__ds_scope, { Tabs });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/navigation/Tabs.jsx", error: String((e && e.message) || e) }); }

// components/quiz/AIFeedbackCard.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const CSS = `
.qz-ai {
  display: flex; gap: 14px; padding: 18px; border-radius: var(--radius-lg);
  background: var(--ai-surface); border: 1px solid var(--ai-border); font-family: var(--font-body);
}
.qz-ai__face {
  width: 44px; height: 44px; flex: none; border-radius: 50%; background: var(--brand-mark); color: #fff;
  display: flex; align-items: center; justify-content: center; font: 700 20px var(--font-display);
  box-shadow: var(--shadow-accent);
}
.qz-ai__body { display: flex; flex-direction: column; gap: 6px; min-width: 0; flex: 1; }
.qz-ai__who { display: flex; align-items: center; gap: 8px; font: var(--type-label); color: var(--ai-accent); }
.qz-ai__pill {
  font-size: var(--text-2xs); font-weight: 700; letter-spacing: var(--tracking-wide); text-transform: uppercase;
  background: var(--coral-100); color: var(--coral-700); padding: 3px 9px; border-radius: var(--radius-pill);
  display: inline-flex; align-items: center; gap: 4px;
}
.qz-ai__msg { font: var(--type-body); color: var(--ai-text); line-height: var(--leading-relaxed); }
.qz-ai__msg em { color: var(--accent-text); font-style: normal; font-weight: 700; }
.qz-ai__shimmer { display: flex; flex-direction: column; gap: 8px; margin-top: 4px; }
.qz-ai__line { height: 12px; border-radius: var(--radius-pill); background: linear-gradient(90deg, var(--coral-100) 25%, var(--coral-200) 37%, var(--coral-100) 63%); background-size: 400% 100%; }
@media (prefers-reduced-motion: no-preference) { .qz-ai__line { animation: qz-ai-shimmer 1.4s ease infinite; } }
@keyframes qz-ai-shimmer { 0% { background-position: 100% 0; } 100% { background-position: 0 0; } }
`;
if (typeof document !== 'undefined' && !document.getElementById('qz-aifeedback-css')) {
  const el = document.createElement('style');
  el.id = 'qz-aifeedback-css';
  el.textContent = CSS;
  document.head.appendChild(el);
}

/**
 * Quiztin's AI voice. Warm apricot surface + companion avatar so it reads as a
 * helpful friend, clearly distinct from the blue product chrome — never a grade
 * stamp. Supports a shimmer `loading` state while feedback generates.
 */
function AIFeedbackCard({
  children,
  name = 'Quiztin',
  label = 'AI feedback',
  avatar = 'Q',
  loading = false,
  className = '',
  ...rest
}) {
  return /*#__PURE__*/React.createElement("div", _extends({
    className: `qz-ai ${className}`.trim()
  }, rest), /*#__PURE__*/React.createElement("div", {
    className: "qz-ai__face",
    "aria-hidden": "true"
  }, avatar), /*#__PURE__*/React.createElement("div", {
    className: "qz-ai__body"
  }, /*#__PURE__*/React.createElement("div", {
    className: "qz-ai__who"
  }, name, /*#__PURE__*/React.createElement("span", {
    className: "qz-ai__pill"
  }, /*#__PURE__*/React.createElement(__ds_scope.Icon, {
    name: "sparkle",
    weight: "fill",
    size: "0.9em"
  }), label)), loading ? /*#__PURE__*/React.createElement("div", {
    className: "qz-ai__shimmer",
    "aria-label": "Quiztin is writing feedback\u2026"
  }, /*#__PURE__*/React.createElement("span", {
    className: "qz-ai__line",
    style: {
      width: '92%'
    }
  }), /*#__PURE__*/React.createElement("span", {
    className: "qz-ai__line",
    style: {
      width: '78%'
    }
  }), /*#__PURE__*/React.createElement("span", {
    className: "qz-ai__line",
    style: {
      width: '54%'
    }
  })) : /*#__PURE__*/React.createElement("p", {
    className: "qz-ai__msg"
  }, children)));
}
Object.assign(__ds_scope, { AIFeedbackCard });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/quiz/AIFeedbackCard.jsx", error: String((e && e.message) || e) }); }

// components/quiz/AnswerChoice.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const CSS = `
.qz-answer {
  display: flex; align-items: center; gap: 14px; width: 100%; text-align: left;
  padding: 16px 18px; border: 2px solid var(--answer-idle-border); border-radius: var(--radius-tile);
  background: var(--surface-card); color: var(--text-body); font-family: var(--font-body);
  cursor: pointer; min-height: var(--tap-min);
  transition: transform var(--duration-fast) var(--ease-out),
              border-color var(--duration-fast) var(--ease-out),
              background-color var(--duration-fast) var(--ease-out),
              box-shadow var(--duration-fast) var(--ease-out);
}
.qz-answer:hover:not(:disabled) { border-color: var(--sand-400); background: var(--sand-50); }
.qz-answer:active:not(:disabled) { transform: scale(0.99); }
.qz-answer:focus-visible { outline: none; box-shadow: var(--focus-ring-shadow); }
.qz-answer:disabled { cursor: default; }

.qz-answer__marker {
  flex: none; width: 34px; height: 34px; border-radius: 50%;
  border: 2px solid var(--sand-300); background: var(--surface-card);
  display: flex; align-items: center; justify-content: center;
  font-weight: 800; font-size: var(--text-base); color: var(--text-muted);
  transition: var(--transition-colors);
}
.qz-answer__label { flex: 1; font: var(--type-body-strong); font-size: var(--text-lg); color: var(--text-strong); }
.qz-answer__tag { flex: none; font: 700 var(--text-2xs)/1 var(--font-body); letter-spacing: var(--tracking-wide); text-transform: uppercase; }

.qz-answer--selected { border-color: var(--answer-selected-border); background: var(--answer-selected-bg); }
.qz-answer--selected .qz-answer__marker { background: var(--primary); border-color: var(--primary); color: #fff; }
.qz-answer--selected .qz-answer__tag { color: var(--primary); }

.qz-answer--correct { border-color: var(--answer-correct-border); background: var(--answer-correct-bg); }
.qz-answer--correct .qz-answer__marker { background: var(--success); border-color: var(--success); color: #fff; }
.qz-answer--correct .qz-answer__tag { color: var(--success-text); }

.qz-answer--incorrect { border-color: var(--answer-incorrect-border); background: var(--answer-incorrect-bg); }
.qz-answer--incorrect .qz-answer__marker { background: var(--danger); border-color: var(--danger); color: #fff; }
.qz-answer--incorrect .qz-answer__tag { color: var(--danger-text); }

.qz-answer--missed { border-color: var(--answer-correct-border); border-style: dashed; background: var(--surface-card); }
.qz-answer--missed .qz-answer__marker { border-color: var(--success); color: var(--success); }
.qz-answer--missed .qz-answer__tag { color: var(--success-text); }
`;
if (typeof document !== 'undefined' && !document.getElementById('qz-answerchoice-css')) {
  const el = document.createElement('style');
  el.id = 'qz-answerchoice-css';
  el.textContent = CSS;
  document.head.appendChild(el);
}
const STATE_TAG = {
  selected: 'Selected',
  correct: 'Correct',
  incorrect: 'Your answer',
  missed: 'Correct answer'
};
function markerContent(state, marker) {
  if (state === 'correct') return /*#__PURE__*/React.createElement(__ds_scope.Icon, {
    name: "check",
    weight: "bold",
    size: "1em"
  });
  if (state === 'incorrect') return /*#__PURE__*/React.createElement(__ds_scope.Icon, {
    name: "x",
    weight: "bold",
    size: "1em"
  });
  return marker;
}

/**
 * The selectable answer tile — the core of taking a quiz. During the quiz it's
 * `idle`/`selected`; on the results page it shows `correct`/`incorrect`/`missed`.
 * Big touch target, gentle result colours.
 */
function AnswerChoice({
  marker,
  children,
  label,
  state = 'idle',
  tag,
  disabled = false,
  onSelect,
  className = '',
  ...rest
}) {
  const isResult = state === 'correct' || state === 'incorrect' || state === 'missed';
  const cls = ['qz-answer', state !== 'idle' && `qz-answer--${state}`, className].filter(Boolean).join(' ');
  const shownTag = tag !== undefined ? tag : STATE_TAG[state];
  return /*#__PURE__*/React.createElement("button", _extends({
    type: "button",
    className: cls,
    onClick: onSelect,
    disabled: disabled || isResult,
    "aria-pressed": state === 'selected' || undefined
  }, rest), marker != null && /*#__PURE__*/React.createElement("span", {
    className: "qz-answer__marker"
  }, markerContent(state, marker)), /*#__PURE__*/React.createElement("span", {
    className: "qz-answer__label"
  }, children ?? label), shownTag && /*#__PURE__*/React.createElement("span", {
    className: "qz-answer__tag"
  }, shownTag));
}
Object.assign(__ds_scope, { AnswerChoice });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/quiz/AnswerChoice.jsx", error: String((e && e.message) || e) }); }

// components/quiz/ResultSummary.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const CSS = `
.qz-result {
  display: flex; gap: 24px; align-items: center; padding: var(--space-7);
  border-radius: var(--radius-xl); background: var(--surface-card);
  border: 1px solid var(--border); box-shadow: var(--shadow-sm); font-family: var(--font-body);
}
.qz-result__ring {
  width: 128px; height: 128px; border-radius: 50%; flex: none; position: relative;
  display: flex; align-items: center; justify-content: center;
}
.qz-result__ring::before { content: ''; position: absolute; inset: 13px; background: var(--surface-card); border-radius: 50%; }
.qz-result__pct { position: relative; font: 800 var(--text-3xl) var(--font-display); color: var(--text-strong); line-height: 1; }
.qz-result__pct span { font-size: var(--text-lg); }
.qz-result__body { display: flex; flex-direction: column; gap: 4px; flex: 1; min-width: 0; }
.qz-result__headline { font: var(--type-heading); color: var(--text-strong); }
.qz-result__sub { font: var(--type-body-lg); color: var(--text-muted); }
.qz-result__stats { display: flex; gap: 10px; margin-top: 12px; flex-wrap: wrap; }
.qz-result__stat { display: inline-flex; align-items: center; gap: 7px; padding: 7px 13px; border-radius: var(--radius-pill); font: var(--type-label); }
.qz-result__stat--correct { background: var(--success-soft); color: var(--success-text); }
.qz-result__stat--incorrect { background: var(--danger-soft); color: var(--danger-text); }
`;
if (typeof document !== 'undefined' && !document.getElementById('qz-resultsummary-css')) {
  const el = document.createElement('style');
  el.id = 'qz-resultsummary-css';
  el.textContent = CSS;
  document.head.appendChild(el);
}
function tier(pct) {
  if (pct >= 90) return {
    color: 'var(--success)',
    headline: 'Fantastic work!'
  };
  if (pct >= 70) return {
    color: 'var(--primary)',
    headline: 'Nice work!'
  };
  if (pct >= 50) return {
    color: 'var(--accent)',
    headline: 'Good effort!'
  };
  return {
    color: 'var(--accent)',
    headline: "Let's keep going!"
  };
}

/**
 * The encouraging score summary shown right after submitting. Leads with a
 * warm, plain-language headline (never just a number), a calm progress ring,
 * and correct/incorrect counts. Tone stays supportive at every score.
 */
function ResultSummary({
  correct,
  total,
  headline,
  message,
  className = '',
  ...rest
}) {
  const pct = total > 0 ? Math.round(correct / total * 100) : 0;
  const t = tier(pct);
  const incorrect = total - correct;
  const sub = message || `You got ${correct} of ${total} questions right.`;
  return /*#__PURE__*/React.createElement("div", _extends({
    className: `qz-result ${className}`.trim()
  }, rest), /*#__PURE__*/React.createElement("div", {
    className: "qz-result__ring",
    style: {
      background: `conic-gradient(${t.color} ${pct}%, var(--sand-200) 0)`
    },
    role: "img",
    "aria-label": `Score: ${pct} percent`
  }, /*#__PURE__*/React.createElement("span", {
    className: "qz-result__pct"
  }, pct, /*#__PURE__*/React.createElement("span", null, "%"))), /*#__PURE__*/React.createElement("div", {
    className: "qz-result__body"
  }, /*#__PURE__*/React.createElement("span", {
    className: "qz-result__headline"
  }, headline || t.headline), /*#__PURE__*/React.createElement("span", {
    className: "qz-result__sub"
  }, sub), /*#__PURE__*/React.createElement("div", {
    className: "qz-result__stats"
  }, /*#__PURE__*/React.createElement("span", {
    className: "qz-result__stat qz-result__stat--correct"
  }, /*#__PURE__*/React.createElement(__ds_scope.Icon, {
    name: "check-circle",
    weight: "fill"
  }), correct, " correct"), incorrect > 0 && /*#__PURE__*/React.createElement("span", {
    className: "qz-result__stat qz-result__stat--incorrect"
  }, /*#__PURE__*/React.createElement(__ds_scope.Icon, {
    name: "arrow-counter-clockwise",
    weight: "fill"
  }), incorrect, " to review"))));
}
Object.assign(__ds_scope, { ResultSummary });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/quiz/ResultSummary.jsx", error: String((e && e.message) || e) }); }

// components/surfaces/Card.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const CSS = `
.qz-card {
  background: var(--surface-card); border: 1px solid var(--border);
  border-radius: var(--radius-card); box-shadow: var(--shadow-sm);
  font-family: var(--font-body); color: var(--text-body);
  display: block; text-align: left; width: 100%; box-sizing: border-box;
}
.qz-card--flat { box-shadow: none; }
.qz-card--sunken { background: var(--surface-sunken); box-shadow: none; border-color: var(--sand-200); }
.qz-card--pad-none { padding: 0; }
.qz-card--pad-sm { padding: var(--space-4); }
.qz-card--pad-md { padding: var(--space-6); }
.qz-card--pad-lg { padding: var(--space-8); }
.qz-card--interactive {
  cursor: pointer; -webkit-appearance: none; appearance: none;
  transition: transform var(--duration-fast) var(--ease-out),
              box-shadow var(--duration-fast) var(--ease-out),
              border-color var(--duration-fast) var(--ease-out);
}
.qz-card--interactive:hover { box-shadow: var(--shadow-md); transform: translateY(var(--lift-y)); border-color: var(--sand-300); }
.qz-card--interactive:active { transform: scale(0.995); }
.qz-card--interactive:focus-visible { outline: none; box-shadow: var(--focus-ring-shadow); }
`;
if (typeof document !== 'undefined' && !document.getElementById('qz-card-css')) {
  const el = document.createElement('style');
  el.id = 'qz-card-css';
  el.textContent = CSS;
  document.head.appendChild(el);
}

/**
 * The workhorse surface — quizzes, classrooms, results, settings all sit on
 * cards. Hairline border + soft warm shadow. `interactive` makes the whole card
 * a button/link with a gentle hover lift.
 */
function Card({
  children,
  variant = 'raised',
  padding = 'md',
  interactive = false,
  as,
  href,
  onClick,
  className = '',
  ...rest
}) {
  const clickable = interactive || !!href || !!onClick;
  const Tag = href ? 'a' : as || (clickable ? 'button' : 'div');
  const cls = ['qz-card', `qz-card--${variant}`, `qz-card--pad-${padding}`, clickable && 'qz-card--interactive', className].filter(Boolean).join(' ');
  const extra = Tag === 'button' ? {
    type: 'button'
  } : {};
  return /*#__PURE__*/React.createElement(Tag, _extends({
    className: cls,
    href: href,
    onClick: onClick
  }, extra, rest), children);
}
Object.assign(__ds_scope, { Card });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/surfaces/Card.jsx", error: String((e && e.message) || e) }); }

// components/surfaces/Dialog.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const CSS = `
.qz-dialog__backdrop {
  position: fixed; inset: 0; z-index: 100; display: flex; align-items: center; justify-content: center;
  padding: var(--space-6); background: rgba(33, 28, 21, 0.42); -webkit-backdrop-filter: blur(3px); backdrop-filter: blur(3px);
}
.qz-dialog {
  position: relative; background: var(--surface-card); border-radius: var(--radius-modal);
  box-shadow: var(--shadow-xl); width: 100%; max-height: calc(100vh - 48px); overflow: auto;
  font-family: var(--font-body); display: flex; flex-direction: column;
}
.qz-dialog--sm { max-width: 400px; }
.qz-dialog--md { max-width: 520px; }
.qz-dialog--lg { max-width: 680px; }
.qz-dialog__head { display: flex; gap: 14px; align-items: flex-start; padding: var(--space-6) var(--space-6) 0; }
.qz-dialog__icon { width: 46px; height: 46px; border-radius: var(--radius-md); flex: none; display: flex; align-items: center; justify-content: center; font-size: 24px; }
.qz-dialog__icon--primary { background: var(--primary-soft); color: var(--primary); }
.qz-dialog__icon--danger { background: var(--danger-soft); color: var(--danger); }
.qz-dialog__icon--success { background: var(--success-soft); color: var(--success); }
.qz-dialog__icon--warning { background: var(--warning-soft); color: var(--warning); }
.qz-dialog__icon--ai { background: var(--ai-surface); color: var(--ai-accent); }
.qz-dialog__titles { flex: 1; display: flex; flex-direction: column; gap: 5px; padding-top: 2px; }
.qz-dialog__title { font: var(--type-subheading); color: var(--text-strong); }
.qz-dialog__desc { font: var(--type-body); color: var(--text-muted); line-height: var(--leading-relaxed); }
.qz-dialog__close {
  position: absolute; top: 14px; right: 14px; border: 0; background: transparent; cursor: pointer;
  width: 34px; height: 34px; border-radius: var(--radius-sm); display: inline-flex; align-items: center; justify-content: center;
  color: var(--text-subtle); transition: var(--transition-colors);
}
.qz-dialog__close:hover { background: var(--sand-100); color: var(--text-strong); }
.qz-dialog__close:focus-visible { outline: none; box-shadow: var(--focus-ring-shadow); }
.qz-dialog__body { padding: var(--space-5) var(--space-6); color: var(--text-body); flex: 1; }
.qz-dialog__body:first-child { padding-top: var(--space-6); }
.qz-dialog__foot { display: flex; gap: 12px; justify-content: flex-end; flex-wrap: wrap; padding: 0 var(--space-6) var(--space-6); }
@media (prefers-reduced-motion: no-preference) {
  .qz-dialog__backdrop { animation: qz-dlg-fade var(--duration-base) var(--ease-out); }
  .qz-dialog { animation: qz-dlg-pop var(--duration-base) var(--ease-spring); }
}
@keyframes qz-dlg-fade { from { opacity: 0; } }
@keyframes qz-dlg-pop { from { opacity: 0; transform: translateY(12px) scale(0.97); } }
`;
if (typeof document !== 'undefined' && !document.getElementById('qz-dialog-css')) {
  const el = document.createElement('style');
  el.id = 'qz-dialog-css';
  el.textContent = CSS;
  document.head.appendChild(el);
}

/**
 * Accessible modal dialog. Handles the backdrop, Escape-to-close, focus on
 * open, and role=dialog/aria-modal. Perfect for the "Ready to submit?" calm
 * confirmation before irreversible actions.
 */
function Dialog({
  open,
  onClose,
  title,
  description,
  icon,
  tone = 'primary',
  size = 'sm',
  footer,
  children,
  closeLabel = 'Close',
  showClose = true,
  className = '',
  ...rest
}) {
  const panelRef = React.useRef(null);
  const labelId = React.useId();
  const descId = React.useId();
  React.useEffect(() => {
    if (!open) return undefined;
    const onKey = e => {
      if (e.key === 'Escape') onClose && onClose();
    };
    document.addEventListener('keydown', onKey);
    const prevOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    const t = setTimeout(() => {
      panelRef.current && panelRef.current.focus();
    }, 0);
    return () => {
      document.removeEventListener('keydown', onKey);
      document.body.style.overflow = prevOverflow;
      clearTimeout(t);
    };
  }, [open, onClose]);
  if (!open) return null;
  return /*#__PURE__*/React.createElement("div", {
    className: "qz-dialog__backdrop",
    onMouseDown: e => {
      if (e.target === e.currentTarget) onClose && onClose();
    }
  }, /*#__PURE__*/React.createElement("div", _extends({
    ref: panelRef,
    className: `qz-dialog qz-dialog--${size} ${className}`.trim(),
    role: "dialog",
    "aria-modal": "true",
    "aria-labelledby": title ? labelId : undefined,
    "aria-describedby": description ? descId : undefined,
    tabIndex: -1
  }, rest), (title || icon) && /*#__PURE__*/React.createElement("div", {
    className: "qz-dialog__head"
  }, icon && /*#__PURE__*/React.createElement("span", {
    className: `qz-dialog__icon qz-dialog__icon--${tone}`
  }, /*#__PURE__*/React.createElement(__ds_scope.Icon, {
    name: icon,
    weight: "fill"
  })), /*#__PURE__*/React.createElement("div", {
    className: "qz-dialog__titles"
  }, title && /*#__PURE__*/React.createElement("span", {
    className: "qz-dialog__title",
    id: labelId
  }, title), description && /*#__PURE__*/React.createElement("span", {
    className: "qz-dialog__desc",
    id: descId
  }, description))), showClose && onClose && /*#__PURE__*/React.createElement("button", {
    type: "button",
    className: "qz-dialog__close",
    "aria-label": closeLabel,
    onClick: onClose
  }, /*#__PURE__*/React.createElement(__ds_scope.Icon, {
    name: "x",
    weight: "bold",
    size: "18px"
  })), children && /*#__PURE__*/React.createElement("div", {
    className: "qz-dialog__body"
  }, children), footer && /*#__PURE__*/React.createElement("div", {
    className: "qz-dialog__foot"
  }, footer)));
}
Object.assign(__ds_scope, { Dialog });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/surfaces/Dialog.jsx", error: String((e && e.message) || e) }); }

// ui_kits/student/chrome.jsx
try { (() => {
/* Quiztin — Student UI kit: shared chrome, sample data, helpers.
   Attaches to window.SK for the other kit scripts + index.html. */
const {
  Icon: SKIcon,
  IconButton: SKIconButton
} = window.QuiztinDesignSystem_138691;
const STUDENT = {
  name: 'Jordan Lee',
  initials: 'JL'
};
const ASSIGNED = [{
  id: 'water',
  title: 'The Water Cycle',
  subject: 'Science',
  cls: 'Period 3 · Biology',
  questions: 8,
  due: 'Due Friday',
  dueTone: 'primary'
}, {
  id: 'frac',
  title: 'Fractions Review',
  subject: 'Maths',
  cls: 'Period 2 · Maths',
  questions: 10,
  due: 'Due Monday',
  dueTone: 'neutral'
}];
const COMPLETED = [{
  id: 'photo',
  title: 'Photosynthesis',
  subject: 'Biology',
  score: 90,
  when: 'Yesterday'
}, {
  id: 'maps',
  title: 'Map Skills',
  subject: 'Geography',
  score: 75,
  when: 'Mon'
}];

/* The quiz the student takes / reviews */
const QUIZ = {
  title: 'The Water Cycle',
  subject: 'Science',
  cls: 'Period 3 · Biology',
  questions: [{
    prompt: 'What process turns water vapour back into liquid water?',
    options: [{
      id: 'a',
      text: 'Evaporation'
    }, {
      id: 'b',
      text: 'Condensation'
    }, {
      id: 'c',
      text: 'Precipitation'
    }, {
      id: 'd',
      text: 'Collection'
    }],
    correct: 'b',
    feedback: "That's condensation — when vapour cools it forms tiny droplets, which is how clouds are made."
  }, {
    prompt: 'Which step of the water cycle is powered by the sun?',
    options: [{
      id: 'a',
      text: 'Evaporation'
    }, {
      id: 'b',
      text: 'Runoff'
    }, {
      id: 'c',
      text: 'Groundwater flow'
    }],
    correct: 'a',
    feedback: "Right — the sun's heat is what lifts water into the air as vapour."
  }, {
    prompt: 'Where does most of Earth\u2019s evaporation happen?',
    options: [{
      id: 'a',
      text: 'Lakes and rivers'
    }, {
      id: 'b',
      text: 'The oceans'
    }, {
      id: 'c',
      text: 'Plants and trees'
    }, {
      id: 'd',
      text: 'Puddles'
    }],
    correct: 'b',
    feedback: "Picture how much of the planet is ocean — that huge surface means most evaporation happens there. Lakes and rivers help, but they're a small share."
  }, {
    prompt: 'What is it called when water falls from clouds as rain or snow?',
    options: [{
      id: 'a',
      text: 'Collection'
    }, {
      id: 'b',
      text: 'Condensation'
    }, {
      id: 'c',
      text: 'Precipitation'
    }],
    correct: 'c',
    feedback: "Exactly — rain, snow, sleet and hail are all forms of precipitation."
  }]
};

/* The student's answers, used by the Review screen (index 2 is wrong on purpose) */
const REVIEW_ANSWERS = ['b', 'a', 'a', 'c'];
const studentChromeStyles = {
  shell: {
    minHeight: '100vh',
    background: 'var(--color-bg)',
    fontFamily: 'var(--font-body)',
    color: 'var(--text-body)'
  },
  bar: {
    display: 'flex',
    alignItems: 'center',
    gap: 20,
    padding: '16px 28px',
    background: 'var(--surface-card)',
    borderBottom: '1px solid var(--border)',
    position: 'sticky',
    top: 0,
    zIndex: 10
  },
  brand: {
    display: 'flex',
    alignItems: 'flex-end',
    gap: 7
  },
  brandWord: {
    font: '600 22px/1 var(--font-display)',
    letterSpacing: '-0.02em',
    color: 'var(--text-strong)'
  },
  brandDot: {
    width: 7,
    height: 7,
    borderRadius: '50%',
    background: 'var(--brand-mark)',
    marginBottom: 5
  },
  nav: {
    display: 'flex',
    gap: 4,
    marginLeft: 12,
    flex: 1
  },
  navItem: active => ({
    border: 0,
    background: active ? 'var(--primary-soft)' : 'transparent',
    color: active ? 'var(--blueberry-800)' : 'var(--text-muted)',
    font: 'var(--type-label)',
    padding: '9px 16px',
    borderRadius: 'var(--radius-pill)',
    cursor: 'pointer'
  }),
  avatar: {
    width: 40,
    height: 40,
    borderRadius: '50%',
    background: 'var(--coral-500)',
    color: '#fff',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    font: '700 15px var(--font-body)',
    flex: 'none'
  },
  content: {
    maxWidth: 760,
    margin: '0 auto',
    padding: '32px 24px 64px'
  }
};
function StudentShell({
  active,
  onNav,
  children
}) {
  return /*#__PURE__*/React.createElement("div", {
    style: studentChromeStyles.shell
  }, /*#__PURE__*/React.createElement("header", {
    style: studentChromeStyles.bar
  }, /*#__PURE__*/React.createElement("div", {
    style: studentChromeStyles.brand
  }, /*#__PURE__*/React.createElement("span", {
    style: studentChromeStyles.brandWord
  }, "Quiztin"), /*#__PURE__*/React.createElement("span", {
    style: studentChromeStyles.brandDot
  })), /*#__PURE__*/React.createElement("nav", {
    style: studentChromeStyles.nav
  }, /*#__PURE__*/React.createElement("button", {
    style: studentChromeStyles.navItem(active === 'home'),
    onClick: () => onNav && onNav('home')
  }, "Home"), /*#__PURE__*/React.createElement("button", {
    style: studentChromeStyles.navItem(active === 'results'),
    onClick: () => onNav && onNav('results')
  }, "My results")), /*#__PURE__*/React.createElement("div", {
    style: studentChromeStyles.avatar
  }, STUDENT.initials)), /*#__PURE__*/React.createElement("main", {
    style: studentChromeStyles.content
  }, children));
}
window.SK = Object.assign(window.SK || {}, {
  STUDENT,
  ASSIGNED,
  COMPLETED,
  QUIZ,
  REVIEW_ANSWERS,
  StudentShell
});
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/student/chrome.jsx", error: String((e && e.message) || e) }); }

// ui_kits/student/home.jsx
try { (() => {
/* Quiztin — Student kit: Home screen (assigned + completed). */
const {
  Card: HCard,
  Button: HButton,
  Badge: HBadge,
  Icon: HIcon,
  TextField: HTextField
} = window.QuiztinDesignSystem_138691;
const homeStyles = {
  hi: {
    font: '600 var(--text-3xl)/1.1 var(--font-display)',
    color: 'var(--text-strong)',
    marginBottom: 6
  },
  sub: {
    font: 'var(--type-body-lg)',
    color: 'var(--text-muted)',
    marginBottom: 30
  },
  secTitle: {
    font: 'var(--type-heading)',
    fontSize: 'var(--text-xl)',
    color: 'var(--text-strong)',
    marginBottom: 14,
    display: 'flex',
    alignItems: 'center',
    gap: 10
  },
  section: {
    marginBottom: 32
  },
  todo: {
    display: 'flex',
    flexDirection: 'column',
    gap: 14
  },
  card: {
    display: 'flex',
    alignItems: 'center',
    gap: 18,
    padding: '20px 22px'
  },
  icon: {
    width: 54,
    height: 54,
    borderRadius: 'var(--radius-lg)',
    background: 'var(--primary-soft)',
    color: 'var(--primary)',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    fontSize: 28,
    flex: 'none'
  },
  cardMain: {
    flex: 1,
    minWidth: 0
  },
  cardTitle: {
    font: 'var(--type-subheading)',
    color: 'var(--text-strong)',
    marginBottom: 3
  },
  cardMeta: {
    display: 'flex',
    alignItems: 'center',
    gap: 10,
    font: 'var(--type-caption)',
    color: 'var(--text-muted)'
  },
  dot: {
    width: 3,
    height: 3,
    borderRadius: '50%',
    background: 'var(--sand-400)'
  },
  join: {
    display: 'flex',
    gap: 12,
    alignItems: 'flex-end'
  },
  joinField: {
    flex: 1
  },
  completedRow: {
    display: 'flex',
    alignItems: 'center',
    gap: 16,
    padding: '14px 20px',
    borderBottom: '1px solid var(--border)'
  },
  cScore: c => ({
    font: '800 var(--text-base) var(--font-body)',
    color: '#fff',
    background: c,
    borderRadius: 'var(--radius-pill)',
    padding: '4px 12px',
    minWidth: 52,
    textAlign: 'center'
  })
};
function AssignedCard({
  q,
  onStart
}) {
  return /*#__PURE__*/React.createElement(HCard, {
    padding: "none"
  }, /*#__PURE__*/React.createElement("div", {
    style: homeStyles.card
  }, /*#__PURE__*/React.createElement("div", {
    style: homeStyles.icon
  }, /*#__PURE__*/React.createElement(HIcon, {
    name: "list-checks",
    weight: "fill"
  })), /*#__PURE__*/React.createElement("div", {
    style: homeStyles.cardMain
  }, /*#__PURE__*/React.createElement("div", {
    style: homeStyles.cardTitle
  }, q.title), /*#__PURE__*/React.createElement("div", {
    style: homeStyles.cardMeta
  }, /*#__PURE__*/React.createElement("span", null, q.cls), /*#__PURE__*/React.createElement("span", {
    style: homeStyles.dot
  }), /*#__PURE__*/React.createElement("span", null, q.questions, " questions"))), /*#__PURE__*/React.createElement(HBadge, {
    tone: q.dueTone,
    icon: "clock"
  }, q.due), /*#__PURE__*/React.createElement(HButton, {
    variant: "primary",
    icon: "play",
    onClick: () => onStart && onStart(q)
  }, "Start")));
}
function StudentHome({
  onStart,
  onReview
}) {
  const SK = window.SK;
  const scoreColor = s => s >= 90 ? 'var(--success)' : s >= 70 ? 'var(--primary)' : 'var(--accent)';
  return /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("h1", {
    style: homeStyles.hi
  }, "Hi, Jordan"), /*#__PURE__*/React.createElement("p", {
    style: homeStyles.sub
  }, "You have 2 quizzes to take. No rush \u2014 take your time with each one."), /*#__PURE__*/React.createElement("section", {
    style: homeStyles.section
  }, /*#__PURE__*/React.createElement("h2", {
    style: homeStyles.secTitle
  }, /*#__PURE__*/React.createElement(HIcon, {
    name: "tray",
    weight: "fill",
    color: "var(--primary)"
  }), " To do"), /*#__PURE__*/React.createElement("div", {
    style: homeStyles.todo
  }, SK.ASSIGNED.map(q => /*#__PURE__*/React.createElement(AssignedCard, {
    key: q.id,
    q: q,
    onStart: onStart
  })))), /*#__PURE__*/React.createElement("section", {
    style: homeStyles.section
  }, /*#__PURE__*/React.createElement("h2", {
    style: homeStyles.secTitle
  }, /*#__PURE__*/React.createElement(HIcon, {
    name: "plus-circle",
    weight: "fill",
    color: "var(--accent)"
  }), " Join a class"), /*#__PURE__*/React.createElement(HCard, {
    padding: "lg"
  }, /*#__PURE__*/React.createElement("div", {
    style: homeStyles.join
  }, /*#__PURE__*/React.createElement("div", {
    style: homeStyles.joinField
  }, /*#__PURE__*/React.createElement(HTextField, {
    label: "Class code",
    icon: "hash",
    placeholder: "e.g. QZ-8P4K",
    hint: "Your teacher will give you this code."
  })), /*#__PURE__*/React.createElement(HButton, {
    variant: "accent",
    icon: "arrow-right"
  }, "Join")))), /*#__PURE__*/React.createElement("section", {
    style: {
      ...homeStyles.section,
      marginBottom: 0
    }
  }, /*#__PURE__*/React.createElement("h2", {
    style: homeStyles.secTitle
  }, /*#__PURE__*/React.createElement(HIcon, {
    name: "check-circle",
    weight: "fill",
    color: "var(--success)"
  }), " Recently completed"), /*#__PURE__*/React.createElement(HCard, {
    padding: "none"
  }, SK.COMPLETED.map(q => /*#__PURE__*/React.createElement("div", {
    key: q.id,
    style: homeStyles.completedRow
  }, /*#__PURE__*/React.createElement("div", {
    style: homeStyles.cardMain
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      font: 'var(--type-body-strong)',
      color: 'var(--text-strong)'
    }
  }, q.title), /*#__PURE__*/React.createElement("div", {
    style: homeStyles.cardMeta
  }, q.subject, " \xB7 ", q.when)), /*#__PURE__*/React.createElement("span", {
    style: homeStyles.cScore(scoreColor(q.score))
  }, q.score, "%"), /*#__PURE__*/React.createElement(HButton, {
    variant: "ghost",
    size: "sm",
    iconRight: "caret-right",
    onClick: () => onReview && onReview(q)
  }, "Review"))))));
}
window.SK = Object.assign(window.SK || {}, {
  StudentHome
});
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/student/home.jsx", error: String((e && e.message) || e) }); }

// ui_kits/student/review.jsx
try { (() => {
/* Quiztin — Student kit: Quiz review screen (results + AI feedback). */
const {
  AnswerChoice: RvAnswer,
  ResultSummary: RvSummary,
  AIFeedbackCard: RvAI,
  Card: RvCard,
  Badge: RvBadge,
  Button: RvButton,
  Icon: RvIcon
} = window.QuiztinDesignSystem_138691;
const RV_LETTERS = ['A', 'B', 'C', 'D', 'E'];
const reviewStyles = {
  section: {
    marginTop: 28
  },
  secTitle: {
    font: 'var(--type-heading)',
    fontSize: 'var(--text-xl)',
    color: 'var(--text-strong)',
    marginBottom: 16
  },
  qCard: {
    marginBottom: 16
  },
  qHead: {
    display: 'flex',
    alignItems: 'center',
    gap: 12,
    marginBottom: 16
  },
  qNum: {
    width: 30,
    height: 30,
    borderRadius: '50%',
    background: 'var(--surface-sunken)',
    color: 'var(--text-muted)',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    font: '800 var(--text-sm) var(--font-body)',
    flex: 'none'
  },
  qPrompt: {
    flex: 1,
    font: 'var(--type-subheading)',
    color: 'var(--text-strong)'
  },
  opts: {
    display: 'flex',
    flexDirection: 'column',
    gap: 10,
    marginBottom: 4
  },
  goodNote: {
    display: 'flex',
    alignItems: 'center',
    gap: 8,
    marginTop: 12,
    font: 'var(--type-label)',
    color: 'var(--success-text)'
  },
  actions: {
    display: 'flex',
    gap: 12,
    marginTop: 28,
    justifyContent: 'center'
  }
};
function QuestionReview({
  q,
  index,
  chosen
}) {
  const wasRight = chosen === q.correct;
  return /*#__PURE__*/React.createElement(RvCard, {
    padding: "lg",
    style: reviewStyles.qCard
  }, /*#__PURE__*/React.createElement("div", {
    style: reviewStyles.qHead
  }, /*#__PURE__*/React.createElement("span", {
    style: reviewStyles.qNum
  }, index + 1), /*#__PURE__*/React.createElement("span", {
    style: reviewStyles.qPrompt
  }, q.prompt), wasRight ? /*#__PURE__*/React.createElement(RvBadge, {
    tone: "success",
    icon: "check-circle"
  }, "Correct") : /*#__PURE__*/React.createElement(RvBadge, {
    tone: "warning",
    icon: "arrow-counter-clockwise"
  }, "Review")), /*#__PURE__*/React.createElement("div", {
    style: reviewStyles.opts
  }, q.options.map((o, oi) => {
    let state = 'idle';
    if (o.id === q.correct) state = wasRight ? 'correct' : 'missed';else if (o.id === chosen) state = 'incorrect';
    return /*#__PURE__*/React.createElement(RvAnswer, {
      key: o.id,
      marker: RV_LETTERS[oi],
      state: state,
      disabled: true
    }, o.text);
  })), wasRight ? /*#__PURE__*/React.createElement("div", {
    style: reviewStyles.goodNote
  }, /*#__PURE__*/React.createElement(RvIcon, {
    name: "sparkle",
    weight: "fill"
  }), " You nailed this one.") : /*#__PURE__*/React.createElement(RvAI, null, q.feedback));
}
function QuizReview({
  result,
  onHome,
  onRetake
}) {
  const SK = window.SK;
  const quiz = SK.QUIZ;
  const total = quiz.questions.length;
  const answers = result && result.answers ? result.answers : SK.REVIEW_ANSWERS.reduce((m, v, i) => {
    m[i] = v;
    return m;
  }, {});
  const correct = result && typeof result.correct === 'number' ? result.correct : quiz.questions.reduce((a, qq, i) => a + (answers[i] === qq.correct ? 1 : 0), 0);
  return /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(RvSummary, {
    correct: correct,
    total: total
  }), /*#__PURE__*/React.createElement("div", {
    style: {
      marginTop: 16
    }
  }, /*#__PURE__*/React.createElement(RvAI, null, "Great work, Jordan \u2014 ", correct, " out of ", total, "! You clearly understand condensation and the sun's role in the cycle. The one to revisit is ", /*#__PURE__*/React.createElement("em", null, "where most evaporation happens"), " \u2014 take a peek at the note on question 3 and you'll have it.")), /*#__PURE__*/React.createElement("section", {
    style: reviewStyles.section
  }, /*#__PURE__*/React.createElement("h2", {
    style: reviewStyles.secTitle
  }, "Question by question"), quiz.questions.map((q, i) => /*#__PURE__*/React.createElement(QuestionReview, {
    key: i,
    q: q,
    index: i,
    chosen: answers[i]
  }))), /*#__PURE__*/React.createElement("div", {
    style: reviewStyles.actions
  }, /*#__PURE__*/React.createElement(RvButton, {
    variant: "secondary",
    icon: "house",
    onClick: onHome
  }, "Back to home"), /*#__PURE__*/React.createElement(RvButton, {
    variant: "ghost",
    icon: "arrow-counter-clockwise",
    onClick: onRetake
  }, "Retake quiz")));
}
window.SK = Object.assign(window.SK || {}, {
  QuizReview
});
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/student/review.jsx", error: String((e && e.message) || e) }); }

// ui_kits/student/take.jsx
try { (() => {
/* Quiztin — Student kit: Take-quiz screen. Calm, low-anxiety, confirm-before-submit. */
const {
  AnswerChoice: TAnswer,
  Button: TButton,
  Dialog: TDialog,
  ProgressBar: TProgress,
  Icon: TIcon
} = window.QuiztinDesignSystem_138691;
const LETTERS = ['A', 'B', 'C', 'D', 'E'];
const takeStyles = {
  shell: {
    minHeight: '100vh',
    display: 'flex',
    flexDirection: 'column',
    background: 'var(--color-bg)',
    fontFamily: 'var(--font-body)',
    color: 'var(--text-body)'
  },
  bar: {
    display: 'flex',
    alignItems: 'center',
    gap: 14,
    padding: '14px 24px',
    background: 'var(--surface-card)',
    borderBottom: '1px solid var(--border)'
  },
  brand: {
    display: 'flex',
    alignItems: 'flex-end',
    gap: 6
  },
  brandWord: {
    font: '600 20px/1 var(--font-display)',
    letterSpacing: '-0.02em',
    color: 'var(--text-strong)'
  },
  brandDot: {
    width: 6,
    height: 6,
    borderRadius: '50%',
    background: 'var(--brand-mark)',
    marginBottom: 4
  },
  qtitle: {
    font: 'var(--type-body-strong)',
    color: 'var(--text-muted)',
    flex: 1,
    borderLeft: '1px solid var(--border)',
    paddingLeft: 14,
    marginLeft: 4
  },
  progressWrap: {
    padding: '18px 24px 8px',
    maxWidth: 680,
    margin: '0 auto',
    width: '100%',
    boxSizing: 'border-box'
  },
  dots: {
    display: 'flex',
    gap: 8,
    marginTop: 14,
    justifyContent: 'center'
  },
  dot: state => ({
    width: 30,
    height: 30,
    borderRadius: '50%',
    cursor: 'pointer',
    border: '2px solid',
    borderColor: state === 'current' ? 'var(--primary)' : state === 'done' ? 'var(--primary)' : 'var(--sand-300)',
    background: state === 'done' ? 'var(--primary)' : 'var(--surface-card)',
    color: state === 'done' ? '#fff' : state === 'current' ? 'var(--primary)' : 'var(--text-subtle)',
    font: '700 var(--text-sm) var(--font-body)',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center'
  }),
  content: {
    flex: 1,
    maxWidth: 680,
    margin: '0 auto',
    width: '100%',
    padding: '24px',
    boxSizing: 'border-box'
  },
  eyebrow: {
    font: '700 var(--text-sm) var(--font-body)',
    letterSpacing: 'var(--tracking-wide)',
    textTransform: 'uppercase',
    color: 'var(--primary)',
    marginBottom: 12,
    display: 'block'
  },
  prompt: {
    font: '600 var(--text-3xl)/1.25 var(--font-body)',
    color: 'var(--text-strong)',
    marginBottom: 26
  },
  options: {
    display: 'flex',
    flexDirection: 'column',
    gap: 12
  },
  reassure: {
    display: 'flex',
    alignItems: 'center',
    gap: 8,
    marginTop: 22,
    font: 'var(--type-caption)',
    color: 'var(--text-muted)',
    justifyContent: 'center'
  },
  footer: {
    position: 'sticky',
    bottom: 0,
    display: 'flex',
    alignItems: 'center',
    gap: 12,
    padding: '16px 24px',
    background: 'var(--surface-card)',
    borderTop: '1px solid var(--border)'
  },
  footInner: {
    maxWidth: 680,
    margin: '0 auto',
    width: '100%',
    display: 'flex',
    alignItems: 'center',
    gap: 12
  },
  spacer: {
    flex: 1
  }
};
function TakeQuiz({
  onSubmit,
  onExit
}) {
  const SK = window.SK;
  const quiz = SK.QUIZ;
  const n = quiz.questions.length;
  const [i, setI] = React.useState(0);
  const [answers, setAnswers] = React.useState({});
  const [confirm, setConfirm] = React.useState(false);
  const q = quiz.questions[i];
  const answeredCount = Object.keys(answers).length;
  const isLast = i === n - 1;
  const select = oid => setAnswers(a => ({
    ...a,
    [i]: oid
  }));
  const go = idx => setI(Math.max(0, Math.min(n - 1, idx)));
  const score = () => quiz.questions.reduce((acc, qq, idx) => acc + (answers[idx] === qq.correct ? 1 : 0), 0);
  const submit = () => {
    setConfirm(false);
    onSubmit && onSubmit({
      answers,
      correct: score(),
      total: n
    });
  };
  return /*#__PURE__*/React.createElement("div", {
    style: takeStyles.shell
  }, /*#__PURE__*/React.createElement("header", {
    style: takeStyles.bar
  }, /*#__PURE__*/React.createElement("div", {
    style: takeStyles.brand
  }, /*#__PURE__*/React.createElement("span", {
    style: takeStyles.brandWord
  }, "Quiztin"), /*#__PURE__*/React.createElement("span", {
    style: takeStyles.brandDot
  })), /*#__PURE__*/React.createElement("span", {
    style: takeStyles.qtitle
  }, quiz.title), /*#__PURE__*/React.createElement(TButton, {
    variant: "ghost",
    size: "sm",
    icon: "floppy-disk",
    onClick: onExit
  }, "Save & exit")), /*#__PURE__*/React.createElement("div", {
    style: takeStyles.progressWrap
  }, /*#__PURE__*/React.createElement(TProgress, {
    value: i + 1,
    max: n,
    label: "Your progress",
    showCount: true,
    countFormat: (v, m) => `Question ${v} of ${m}`
  }), /*#__PURE__*/React.createElement("div", {
    style: takeStyles.dots
  }, quiz.questions.map((_, idx) => {
    const state = idx === i ? 'current' : answers[idx] != null ? 'done' : 'todo';
    return /*#__PURE__*/React.createElement("button", {
      key: idx,
      style: takeStyles.dot(state),
      onClick: () => go(idx),
      "aria-label": `Go to question ${idx + 1}`,
      title: `Question ${idx + 1}`
    }, state === 'done' && idx !== i ? /*#__PURE__*/React.createElement(TIcon, {
      name: "check",
      weight: "bold",
      size: "0.8em"
    }) : idx + 1);
  }))), /*#__PURE__*/React.createElement("div", {
    style: takeStyles.content
  }, /*#__PURE__*/React.createElement("span", {
    style: takeStyles.eyebrow
  }, "Question ", i + 1, " of ", n), /*#__PURE__*/React.createElement("h2", {
    style: takeStyles.prompt
  }, q.prompt), /*#__PURE__*/React.createElement("div", {
    style: takeStyles.options
  }, q.options.map((o, oi) => /*#__PURE__*/React.createElement(TAnswer, {
    key: o.id,
    marker: LETTERS[oi],
    state: answers[i] === o.id ? 'selected' : 'idle',
    onSelect: () => select(o.id)
  }, o.text))), /*#__PURE__*/React.createElement("div", {
    style: takeStyles.reassure
  }, /*#__PURE__*/React.createElement(TIcon, {
    name: "clock-countdown"
  }), " No timer \u2014 take your time. You can change answers before submitting.")), /*#__PURE__*/React.createElement("footer", {
    style: takeStyles.footer
  }, /*#__PURE__*/React.createElement("div", {
    style: takeStyles.footInner
  }, /*#__PURE__*/React.createElement(TButton, {
    variant: "ghost",
    icon: "arrow-left",
    disabled: i === 0,
    onClick: () => go(i - 1)
  }, "Back"), /*#__PURE__*/React.createElement("div", {
    style: takeStyles.spacer
  }), isLast ? /*#__PURE__*/React.createElement(TButton, {
    variant: "accent",
    icon: "paper-plane-tilt",
    onClick: () => setConfirm(true)
  }, "Review & submit") : /*#__PURE__*/React.createElement(TButton, {
    variant: "primary",
    iconRight: "arrow-right",
    onClick: () => go(i + 1)
  }, "Next"))), /*#__PURE__*/React.createElement(TDialog, {
    open: confirm,
    onClose: () => setConfirm(false),
    icon: "paper-plane-tilt",
    tone: "primary",
    title: "Ready to submit?",
    description: answeredCount === n ? `You answered all ${n} questions. You can't change answers after submitting.` : `You've answered ${answeredCount} of ${n}. You can go back and finish the rest, or submit now.`,
    footer: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(TButton, {
      variant: "ghost",
      onClick: () => setConfirm(false)
    }, "Keep working"), /*#__PURE__*/React.createElement(TButton, {
      variant: "accent",
      onClick: submit
    }, "Submit quiz"))
  }));
}
window.SK = Object.assign(window.SK || {}, {
  TakeQuiz
});
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/student/take.jsx", error: String((e && e.message) || e) }); }

// ui_kits/teacher/chrome.jsx
try { (() => {
/* Quiztin — Teacher UI kit: shared chrome, sample data, helpers.
   Attaches everything to window.TK for the other kit scripts + index.html. */
const {
  Icon,
  IconButton,
  Badge
} = window.QuiztinDesignSystem_138691;

/* ----------------------------- Sample data ----------------------------- */
const TEACHER = {
  name: 'Ms. Rivera',
  initials: 'MR',
  subject: 'Science'
};
const CLASSES = [{
  id: 'bio3',
  name: 'Period 3 · Biology',
  code: 'QZ-8P4K',
  students: 28,
  color: 'var(--blueberry-500)'
}, {
  id: 'chem5',
  name: 'Period 5 · Chemistry',
  code: 'QZ-2M9R',
  students: 24,
  color: 'var(--coral-500)'
}, {
  id: 'home',
  name: 'Homeroom',
  code: 'QZ-5J7T',
  students: 30,
  color: 'var(--green-500)'
}];
const QUIZZES = [{
  id: 'water',
  title: 'The Water Cycle',
  subject: 'Science',
  questions: 8,
  status: 'published',
  avg: 86,
  submissions: 24,
  cls: 'Period 3 · Biology',
  due: 'Due Fri'
}, {
  id: 'cell',
  title: 'Cell Structure',
  subject: 'Biology',
  questions: 6,
  status: 'draft',
  avg: null,
  submissions: 0,
  cls: 'Period 3 · Biology',
  due: null
}, {
  id: 'photo',
  title: 'Photosynthesis',
  subject: 'Biology',
  questions: 10,
  status: 'published',
  avg: 78,
  submissions: 26,
  cls: 'Period 3 · Biology',
  due: 'Closed'
}];

/* Questions used by the editor + results screens */
const QUESTIONS = [{
  prompt: 'What process turns water vapour back into liquid water?',
  options: [{
    id: 'a',
    text: 'Evaporation'
  }, {
    id: 'b',
    text: 'Condensation'
  }, {
    id: 'c',
    text: 'Precipitation'
  }, {
    id: 'd',
    text: 'Collection'
  }],
  correct: 'b',
  correctPct: 71
}, {
  prompt: 'Which of these is powered by the sun?',
  options: [{
    id: 'a',
    text: 'Evaporation'
  }, {
    id: 'b',
    text: 'Runoff'
  }, {
    id: 'c',
    text: 'Groundwater flow'
  }],
  correct: 'a',
  correctPct: 96
}, {
  prompt: 'Where does most evaporation on Earth happen?',
  options: [{
    id: 'a',
    text: 'Lakes and rivers'
  }, {
    id: 'b',
    text: 'The oceans'
  }, {
    id: 'c',
    text: 'Plants and trees'
  }, {
    id: 'd',
    text: 'Puddles'
  }],
  correct: 'b',
  correctPct: 54
}];
const RESULT_STUDENTS = [{
  name: 'Ava Chen',
  initials: 'AC',
  score: 100,
  when: '2h ago'
}, {
  name: 'Marcus Bell',
  initials: 'MB',
  score: 88,
  when: '2h ago'
}, {
  name: 'Priya Nair',
  initials: 'PN',
  score: 88,
  when: '3h ago'
}, {
  name: 'Diego Fuentes',
  initials: 'DF',
  score: 75,
  when: '3h ago'
}, {
  name: 'Lena Okafor',
  initials: 'LO',
  score: 63,
  when: '5h ago'
}, {
  name: 'Sam Whitfield',
  initials: 'SW',
  score: 50,
  when: '1d ago'
}];

/* ------------------------------- Helpers -------------------------------- */
function scoreColor(pct) {
  if (pct >= 90) return 'var(--success)';
  if (pct >= 70) return 'var(--primary)';
  if (pct >= 50) return 'var(--accent)';
  return 'var(--rose-600)';
}
const chromeStyles = {
  shell: {
    display: 'flex',
    height: '100vh',
    width: '100%',
    background: 'var(--color-bg)',
    fontFamily: 'var(--font-body)',
    color: 'var(--text-body)',
    overflow: 'hidden'
  },
  side: {
    width: 250,
    flex: 'none',
    background: 'var(--surface-card)',
    borderRight: '1px solid var(--border)',
    display: 'flex',
    flexDirection: 'column',
    padding: '20px 16px'
  },
  brand: {
    display: 'flex',
    alignItems: 'center',
    gap: 9,
    padding: '4px 8px 22px'
  },
  brandWord: {
    font: '600 24px/1 var(--font-display)',
    letterSpacing: '-0.02em',
    color: 'var(--text-strong)'
  },
  brandDot: {
    width: 8,
    height: 8,
    borderRadius: '50%',
    background: 'var(--brand-mark)',
    alignSelf: 'flex-end',
    marginBottom: 6
  },
  nav: {
    display: 'flex',
    flexDirection: 'column',
    gap: 3,
    flex: 1
  },
  navItem: active => ({
    display: 'flex',
    alignItems: 'center',
    gap: 12,
    padding: '11px 12px',
    borderRadius: 'var(--radius-md)',
    border: 0,
    background: active ? 'var(--primary-soft)' : 'transparent',
    color: active ? 'var(--blueberry-800)' : 'var(--text-muted)',
    font: 'var(--type-label)',
    fontSize: 'var(--text-base)',
    cursor: 'pointer',
    width: '100%',
    textAlign: 'left'
  }),
  teacherCard: {
    display: 'flex',
    alignItems: 'center',
    gap: 11,
    padding: '10px 12px',
    borderRadius: 'var(--radius-md)',
    background: 'var(--surface-sunken)'
  },
  avatar: {
    width: 38,
    height: 38,
    borderRadius: '50%',
    background: 'var(--blueberry-600)',
    color: '#fff',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    font: '700 15px var(--font-body)',
    flex: 'none'
  },
  main: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    minWidth: 0
  },
  topbar: {
    display: 'flex',
    alignItems: 'center',
    gap: 16,
    padding: '20px 32px',
    borderBottom: '1px solid var(--border)',
    background: 'var(--surface-card)'
  },
  titleWrap: {
    display: 'flex',
    flexDirection: 'column',
    gap: 2,
    flex: 1,
    minWidth: 0
  },
  eyebrow: {
    font: '700 var(--text-2xs)/1 var(--font-body)',
    letterSpacing: 'var(--tracking-wide)',
    textTransform: 'uppercase',
    color: 'var(--text-subtle)'
  },
  title: {
    font: 'var(--type-title)',
    fontSize: 'var(--text-2xl)',
    color: 'var(--text-strong)'
  },
  content: {
    flex: 1,
    overflowY: 'auto',
    padding: '28px 32px'
  }
};
const NAV = [{
  id: 'dashboard',
  label: 'Home',
  icon: 'house'
}, {
  id: 'editor',
  label: 'Quizzes',
  icon: 'list-checks'
}, {
  id: 'results',
  label: 'Results',
  icon: 'chart-bar'
}, {
  id: 'classes',
  label: 'Classes',
  icon: 'users-three'
}];
function AppShell({
  active,
  onNav,
  eyebrow,
  title,
  actions,
  children
}) {
  return /*#__PURE__*/React.createElement("div", {
    style: chromeStyles.shell
  }, /*#__PURE__*/React.createElement("aside", {
    style: chromeStyles.side
  }, /*#__PURE__*/React.createElement("div", {
    style: chromeStyles.brand
  }, /*#__PURE__*/React.createElement("span", {
    style: chromeStyles.brandWord
  }, "Quiztin"), /*#__PURE__*/React.createElement("span", {
    style: chromeStyles.brandDot
  })), /*#__PURE__*/React.createElement("nav", {
    style: chromeStyles.nav
  }, NAV.map(n => /*#__PURE__*/React.createElement("button", {
    key: n.id,
    style: chromeStyles.navItem(active === n.id),
    onClick: () => onNav && onNav(n.id)
  }, /*#__PURE__*/React.createElement(Icon, {
    name: n.icon,
    weight: active === n.id ? 'fill' : 'regular',
    size: "1.3em"
  }), n.label))), /*#__PURE__*/React.createElement("div", {
    style: chromeStyles.teacherCard
  }, /*#__PURE__*/React.createElement("div", {
    style: chromeStyles.avatar
  }, TEACHER.initials), /*#__PURE__*/React.createElement("div", {
    style: {
      minWidth: 0
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      font: 'var(--type-body-strong)',
      color: 'var(--text-strong)',
      whiteSpace: 'nowrap',
      overflow: 'hidden',
      textOverflow: 'ellipsis'
    }
  }, TEACHER.name), /*#__PURE__*/React.createElement("div", {
    style: {
      font: 'var(--type-fine)',
      color: 'var(--text-muted)'
    }
  }, "Teacher")))), /*#__PURE__*/React.createElement("div", {
    style: chromeStyles.main
  }, /*#__PURE__*/React.createElement("header", {
    style: chromeStyles.topbar
  }, /*#__PURE__*/React.createElement("div", {
    style: chromeStyles.titleWrap
  }, eyebrow && /*#__PURE__*/React.createElement("span", {
    style: chromeStyles.eyebrow
  }, eyebrow), /*#__PURE__*/React.createElement("h1", {
    style: chromeStyles.title
  }, title)), actions), /*#__PURE__*/React.createElement("main", {
    style: chromeStyles.content
  }, children)));
}
window.TK = Object.assign(window.TK || {}, {
  TEACHER,
  CLASSES,
  QUIZZES,
  QUESTIONS,
  RESULT_STUDENTS,
  scoreColor,
  AppShell
});
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/teacher/chrome.jsx", error: String((e && e.message) || e) }); }

// ui_kits/teacher/dashboard.jsx
try { (() => {
/* Quiztin — Teacher kit: Home / Dashboard screen. */
const {
  Card: DCard,
  Button: DButton,
  IconButton: DIconButton,
  Badge: DBadge,
  Icon: DIcon
} = window.QuiztinDesignSystem_138691;
const dashStyles = {
  hero: {
    display: 'flex',
    alignItems: 'center',
    gap: 24,
    padding: '26px 28px',
    borderRadius: 'var(--radius-xl)',
    background: 'linear-gradient(120deg, var(--blueberry-600), var(--blueberry-800))',
    color: '#fff',
    marginBottom: 28,
    boxShadow: 'var(--shadow-md)'
  },
  heroText: {
    flex: 1,
    minWidth: 0
  },
  heroHi: {
    font: '600 var(--text-3xl)/1.1 var(--font-display)',
    letterSpacing: '-0.01em',
    marginBottom: 6
  },
  heroSub: {
    font: 'var(--type-body-lg)',
    color: 'rgba(255,255,255,0.82)'
  },
  stats: {
    display: 'flex',
    gap: 10
  },
  stat: {
    textAlign: 'center',
    padding: '12px 18px',
    borderRadius: 'var(--radius-lg)',
    background: 'rgba(255,255,255,0.14)',
    minWidth: 84
  },
  statNum: {
    font: '800 var(--text-2xl) var(--font-display)',
    lineHeight: 1
  },
  statLbl: {
    font: 'var(--type-fine)',
    color: 'rgba(255,255,255,0.82)',
    marginTop: 3
  },
  section: {
    marginBottom: 30
  },
  secHead: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: 14
  },
  secTitle: {
    font: 'var(--type-heading)',
    fontSize: 'var(--text-xl)',
    color: 'var(--text-strong)'
  },
  classGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(230px, 1fr))',
    gap: 14
  },
  classTop: {
    display: 'flex',
    alignItems: 'center',
    gap: 10,
    marginBottom: 14
  },
  classDot: c => ({
    width: 12,
    height: 12,
    borderRadius: 4,
    background: c,
    flex: 'none'
  }),
  className: {
    font: 'var(--type-body-strong)',
    fontSize: 'var(--text-lg)',
    color: 'var(--text-strong)'
  },
  classMeta: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between'
  },
  code: {
    font: '700 var(--text-sm) var(--font-mono)',
    letterSpacing: '0.08em',
    color: 'var(--text-muted)',
    background: 'var(--surface-sunken)',
    padding: '4px 10px',
    borderRadius: 'var(--radius-sm)'
  },
  students: {
    display: 'flex',
    alignItems: 'center',
    gap: 6,
    font: 'var(--type-caption)',
    color: 'var(--text-muted)'
  },
  quizList: {
    display: 'flex',
    flexDirection: 'column',
    gap: 10
  },
  quizRow: {
    display: 'flex',
    alignItems: 'center',
    gap: 16,
    padding: '16px 18px'
  },
  quizIcon: {
    width: 46,
    height: 46,
    borderRadius: 'var(--radius-md)',
    background: 'var(--primary-soft)',
    color: 'var(--primary)',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    fontSize: 24,
    flex: 'none'
  },
  quizMain: {
    flex: 1,
    minWidth: 0
  },
  quizTitle: {
    font: 'var(--type-body-strong)',
    fontSize: 'var(--text-lg)',
    color: 'var(--text-strong)'
  },
  quizMeta: {
    font: 'var(--type-caption)',
    color: 'var(--text-muted)',
    marginTop: 2
  },
  quizAvg: {
    textAlign: 'right',
    marginRight: 4
  },
  quizAvgNum: c => ({
    font: '800 var(--text-xl) var(--font-display)',
    color: c,
    lineHeight: 1
  }),
  quizAvgLbl: {
    font: 'var(--type-fine)',
    color: 'var(--text-subtle)',
    marginTop: 2
  },
  actions: {
    display: 'flex',
    gap: 8
  }
};
function ClassCard({
  c
}) {
  return /*#__PURE__*/React.createElement(DCard, {
    padding: "md"
  }, /*#__PURE__*/React.createElement("div", {
    style: dashStyles.classTop
  }, /*#__PURE__*/React.createElement("span", {
    style: dashStyles.classDot(c.color)
  }), /*#__PURE__*/React.createElement("span", {
    style: dashStyles.className
  }, c.name)), /*#__PURE__*/React.createElement("div", {
    style: dashStyles.classMeta
  }, /*#__PURE__*/React.createElement("span", {
    style: dashStyles.code
  }, c.code), /*#__PURE__*/React.createElement("span", {
    style: dashStyles.students
  }, /*#__PURE__*/React.createElement(DIcon, {
    name: "user",
    weight: "fill"
  }), " ", c.students)));
}
function QuizRow({
  q,
  onOpenQuiz,
  onOpenResults
}) {
  const TK = window.TK;
  const published = q.status === 'published';
  return /*#__PURE__*/React.createElement(DCard, {
    padding: "none"
  }, /*#__PURE__*/React.createElement("div", {
    style: dashStyles.quizRow
  }, /*#__PURE__*/React.createElement("div", {
    style: dashStyles.quizIcon
  }, /*#__PURE__*/React.createElement(DIcon, {
    name: "list-checks",
    weight: "fill"
  })), /*#__PURE__*/React.createElement("div", {
    style: dashStyles.quizMain
  }, /*#__PURE__*/React.createElement("div", {
    style: dashStyles.quizTitle
  }, q.title), /*#__PURE__*/React.createElement("div", {
    style: dashStyles.quizMeta
  }, q.cls, " \xB7 ", q.questions, " questions")), published ? /*#__PURE__*/React.createElement(DBadge, {
    tone: "success",
    icon: "broadcast"
  }, "Live") : /*#__PURE__*/React.createElement(DBadge, {
    tone: "warning",
    dot: true
  }, "Draft"), published && q.avg != null && /*#__PURE__*/React.createElement("div", {
    style: dashStyles.quizAvg
  }, /*#__PURE__*/React.createElement("div", {
    style: dashStyles.quizAvgNum(TK.scoreColor(q.avg))
  }, q.avg, "%"), /*#__PURE__*/React.createElement("div", {
    style: dashStyles.quizAvgLbl
  }, q.submissions, " done")), /*#__PURE__*/React.createElement("div", {
    style: dashStyles.actions
  }, /*#__PURE__*/React.createElement(DButton, {
    variant: "secondary",
    size: "sm",
    icon: "pencil-simple",
    onClick: () => onOpenQuiz && onOpenQuiz(q)
  }, "Edit"), published && /*#__PURE__*/React.createElement(DButton, {
    variant: "ghost",
    size: "sm",
    icon: "chart-bar",
    onClick: () => onOpenResults && onOpenResults(q)
  }, "Results"))));
}
function TeacherDashboard({
  onOpenQuiz,
  onOpenResults
}) {
  const TK = window.TK;
  return /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("div", {
    style: dashStyles.hero
  }, /*#__PURE__*/React.createElement("div", {
    style: dashStyles.heroText
  }, /*#__PURE__*/React.createElement("div", {
    style: dashStyles.heroHi
  }, "Good morning, Ms. Rivera"), /*#__PURE__*/React.createElement("div", {
    style: dashStyles.heroSub
  }, "Your classes are warming up \u2014 24 new submissions since yesterday.")), /*#__PURE__*/React.createElement("div", {
    style: dashStyles.stats
  }, /*#__PURE__*/React.createElement("div", {
    style: dashStyles.stat
  }, /*#__PURE__*/React.createElement("div", {
    style: dashStyles.statNum
  }, "3"), /*#__PURE__*/React.createElement("div", {
    style: dashStyles.statLbl
  }, "Classes")), /*#__PURE__*/React.createElement("div", {
    style: dashStyles.stat
  }, /*#__PURE__*/React.createElement("div", {
    style: dashStyles.statNum
  }, "2"), /*#__PURE__*/React.createElement("div", {
    style: dashStyles.statLbl
  }, "Live quizzes")), /*#__PURE__*/React.createElement("div", {
    style: dashStyles.stat
  }, /*#__PURE__*/React.createElement("div", {
    style: dashStyles.statNum
  }, "24"), /*#__PURE__*/React.createElement("div", {
    style: dashStyles.statLbl
  }, "New results")))), /*#__PURE__*/React.createElement("section", {
    style: dashStyles.section
  }, /*#__PURE__*/React.createElement("div", {
    style: dashStyles.secHead
  }, /*#__PURE__*/React.createElement("h2", {
    style: dashStyles.secTitle
  }, "Your classes"), /*#__PURE__*/React.createElement(DButton, {
    variant: "ghost",
    size: "sm",
    icon: "plus"
  }, "New class")), /*#__PURE__*/React.createElement("div", {
    style: dashStyles.classGrid
  }, TK.CLASSES.map(c => /*#__PURE__*/React.createElement(ClassCard, {
    key: c.id,
    c: c
  })))), /*#__PURE__*/React.createElement("section", {
    style: dashStyles.section
  }, /*#__PURE__*/React.createElement("div", {
    style: dashStyles.secHead
  }, /*#__PURE__*/React.createElement("h2", {
    style: dashStyles.secTitle
  }, "Quizzes"), /*#__PURE__*/React.createElement(DButton, {
    variant: "primary",
    size: "sm",
    icon: "plus",
    onClick: () => onOpenQuiz && onOpenQuiz(TK.QUIZZES[1])
  }, "New quiz")), /*#__PURE__*/React.createElement("div", {
    style: dashStyles.quizList
  }, TK.QUIZZES.map(q => /*#__PURE__*/React.createElement(QuizRow, {
    key: q.id,
    q: q,
    onOpenQuiz: onOpenQuiz,
    onOpenResults: onOpenResults
  })))));
}
window.TK = Object.assign(window.TK || {}, {
  TeacherDashboard
});
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/teacher/dashboard.jsx", error: String((e && e.message) || e) }); }

// ui_kits/teacher/editor.jsx
try { (() => {
/* Quiztin — Teacher kit: Quiz editor screen. */
const {
  Card: ECard,
  Button: EButton,
  IconButton: EIconButton,
  Badge: EBadge,
  Icon: EIcon,
  TextField: ETextField,
  Select: ESelect,
  Switch: ESwitch,
  AIFeedbackCard: EAIFeedbackCard
} = window.QuiztinDesignSystem_138691;
const editorStyles = {
  layout: {
    display: 'grid',
    gridTemplateColumns: 'minmax(0, 1fr) 300px',
    gap: 24,
    alignItems: 'start'
  },
  main: {
    display: 'flex',
    flexDirection: 'column',
    gap: 16,
    minWidth: 0
  },
  qHead: {
    display: 'flex',
    alignItems: 'center',
    gap: 12,
    marginBottom: 16
  },
  qNum: {
    width: 34,
    height: 34,
    borderRadius: '50%',
    background: 'var(--primary)',
    color: '#fff',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    font: '800 var(--text-base) var(--font-body)',
    flex: 'none'
  },
  qTitle: {
    font: 'var(--type-body-strong)',
    fontSize: 'var(--text-lg)',
    color: 'var(--text-strong)',
    flex: 1
  },
  prompt: {
    width: '100%',
    font: 'var(--type-subheading)',
    fontSize: 'var(--text-xl)',
    color: 'var(--text-strong)',
    border: '0',
    borderBottom: '2px solid var(--border)',
    background: 'transparent',
    padding: '4px 2px 10px',
    marginBottom: 18,
    fontFamily: 'var(--font-body)'
  },
  opts: {
    display: 'flex',
    flexDirection: 'column',
    gap: 10
  },
  optRow: {
    display: 'flex',
    alignItems: 'center',
    gap: 12
  },
  correctToggle: on => ({
    width: 30,
    height: 30,
    borderRadius: '50%',
    flex: 'none',
    cursor: 'pointer',
    border: on ? '2px solid var(--success)' : '2px solid var(--sand-300)',
    background: on ? 'var(--success)' : 'var(--surface-card)',
    color: '#fff',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center'
  }),
  optInput: {
    flex: 1,
    font: 'var(--type-body)',
    fontSize: 'var(--text-base)',
    color: 'var(--text-strong)',
    border: '1.5px solid var(--border)',
    borderRadius: 'var(--radius-md)',
    padding: '10px 14px',
    background: 'var(--surface-card)',
    fontFamily: 'var(--font-body)'
  },
  addOpt: {
    marginTop: 4
  },
  qFoot: {
    display: 'flex',
    alignItems: 'center',
    gap: 8,
    marginTop: 16,
    paddingTop: 14,
    borderTop: '1px solid var(--border)',
    color: 'var(--success-text)',
    font: 'var(--type-label)'
  },
  side: {
    display: 'flex',
    flexDirection: 'column',
    gap: 16,
    position: 'sticky',
    top: 0
  },
  sideTitle: {
    font: 'var(--type-label)',
    color: 'var(--text-strong)',
    marginBottom: 4
  },
  sideStack: {
    display: 'flex',
    flexDirection: 'column',
    gap: 16
  },
  switches: {
    display: 'flex',
    flexDirection: 'column',
    gap: 14
  }
};
function OptionRow({
  opt,
  correct,
  onCorrect,
  onRemove
}) {
  return /*#__PURE__*/React.createElement("div", {
    style: editorStyles.optRow
  }, /*#__PURE__*/React.createElement("button", {
    style: editorStyles.correctToggle(correct),
    onClick: onCorrect,
    "aria-label": correct ? 'Correct answer' : 'Mark as correct',
    title: "Mark as correct"
  }, correct && /*#__PURE__*/React.createElement(EIcon, {
    name: "check",
    weight: "bold",
    size: "0.85em"
  })), /*#__PURE__*/React.createElement("input", {
    style: editorStyles.optInput,
    defaultValue: opt.text,
    "aria-label": "Answer text"
  }), /*#__PURE__*/React.createElement(EIconButton, {
    icon: "x",
    label: "Remove answer",
    size: "sm",
    variant: "ghost",
    onClick: onRemove
  }));
}
function QuestionCard({
  q,
  index,
  onCorrect,
  onRemove
}) {
  return /*#__PURE__*/React.createElement(ECard, {
    padding: "lg"
  }, /*#__PURE__*/React.createElement("div", {
    style: editorStyles.qHead
  }, /*#__PURE__*/React.createElement("span", {
    style: editorStyles.qNum
  }, index + 1), /*#__PURE__*/React.createElement("span", {
    style: editorStyles.qTitle
  }, "Question ", index + 1), /*#__PURE__*/React.createElement(EBadge, {
    tone: "neutral",
    icon: "list-bullets"
  }, "Multiple choice"), /*#__PURE__*/React.createElement(EIconButton, {
    icon: "trash",
    label: "Delete question",
    size: "sm",
    variant: "ghost",
    onClick: onRemove
  })), /*#__PURE__*/React.createElement("input", {
    style: editorStyles.prompt,
    defaultValue: q.prompt,
    "aria-label": `Question ${index + 1} prompt`
  }), /*#__PURE__*/React.createElement("div", {
    style: editorStyles.opts
  }, q.options.map(o => /*#__PURE__*/React.createElement(OptionRow, {
    key: o.id,
    opt: o,
    correct: q.correct === o.id,
    onCorrect: () => onCorrect(q.id, o.id),
    onRemove: () => {}
  }))), /*#__PURE__*/React.createElement("div", {
    style: editorStyles.addOpt
  }, /*#__PURE__*/React.createElement(EButton, {
    variant: "ghost",
    size: "sm",
    icon: "plus"
  }, "Add answer")), /*#__PURE__*/React.createElement("div", {
    style: editorStyles.qFoot
  }, /*#__PURE__*/React.createElement(EIcon, {
    name: "check-circle",
    weight: "fill"
  }), "Correct answer set \xB7 tap a circle to change"));
}
function QuizEditor({
  quiz
}) {
  const seed = [{
    id: 1,
    prompt: 'Which part of the cell controls its activities?',
    options: [{
      id: 'a',
      text: 'Nucleus'
    }, {
      id: 'b',
      text: 'Cell wall'
    }, {
      id: 'c',
      text: 'Cytoplasm'
    }],
    correct: 'a'
  }, {
    id: 2,
    prompt: 'What gives plant cells their firm shape?',
    options: [{
      id: 'a',
      text: 'Cell membrane'
    }, {
      id: 'b',
      text: 'Cell wall'
    }],
    correct: 'b'
  }];
  const [questions, setQuestions] = React.useState(seed);
  const setCorrect = (qid, oid) => setQuestions(qs => qs.map(q => q.id === qid ? {
    ...q,
    correct: oid
  } : q));
  const addQuestion = () => setQuestions(qs => [...qs, {
    id: Date.now(),
    prompt: '',
    options: [{
      id: 'a',
      text: ''
    }, {
      id: 'b',
      text: ''
    }],
    correct: 'a'
  }]);
  const removeQuestion = qid => setQuestions(qs => qs.filter(q => q.id !== qid));
  return /*#__PURE__*/React.createElement("div", {
    style: editorStyles.layout
  }, /*#__PURE__*/React.createElement("div", {
    style: editorStyles.main
  }, questions.map((q, i) => /*#__PURE__*/React.createElement(QuestionCard, {
    key: q.id,
    q: q,
    index: i,
    onCorrect: setCorrect,
    onRemove: () => removeQuestion(q.id)
  })), questions.length >= 1 && /*#__PURE__*/React.createElement(EAIFeedbackCard, null, "Nice start! For question 1, you could add a hint like ", /*#__PURE__*/React.createElement("em", null, "\"think about the cell's control centre\""), " \u2014 it helps students who get stuck without giving the answer away."), /*#__PURE__*/React.createElement(EButton, {
    variant: "secondary",
    icon: "plus",
    fullWidth: true,
    onClick: addQuestion
  }, "Add a question")), /*#__PURE__*/React.createElement("aside", {
    style: editorStyles.side
  }, /*#__PURE__*/React.createElement(ECard, {
    padding: "lg"
  }, /*#__PURE__*/React.createElement("div", {
    style: editorStyles.sideTitle
  }, "Quiz details"), /*#__PURE__*/React.createElement("div", {
    style: editorStyles.sideStack
  }, /*#__PURE__*/React.createElement(ETextField, {
    label: "Title",
    defaultValue: quiz ? quiz.title : 'Cell Structure'
  }), /*#__PURE__*/React.createElement(ESelect, {
    label: "Class",
    defaultValue: "bio3",
    options: [{
      value: 'bio3',
      label: 'Period 3 · Biology'
    }, {
      value: 'chem5',
      label: 'Period 5 · Chemistry'
    }]
  }))), /*#__PURE__*/React.createElement(ECard, {
    padding: "lg"
  }, /*#__PURE__*/React.createElement("div", {
    style: editorStyles.sideTitle
  }, "Options"), /*#__PURE__*/React.createElement("div", {
    style: editorStyles.switches
  }, /*#__PURE__*/React.createElement(ESwitch, {
    label: "Shuffle questions"
  }), /*#__PURE__*/React.createElement(ESwitch, {
    label: "Show answers after submit",
    defaultChecked: true
  }), /*#__PURE__*/React.createElement(ESwitch, {
    label: "Allow retakes",
    defaultChecked: true
  })))));
}
window.TK = Object.assign(window.TK || {}, {
  QuizEditor
});
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/teacher/editor.jsx", error: String((e && e.message) || e) }); }

// ui_kits/teacher/results.jsx
try { (() => {
/* Quiztin — Teacher kit: Quiz results screen. */
const {
  Card: RCard,
  Badge: RBadge,
  Icon: RIcon,
  AIFeedbackCard: RAI,
  Tabs: RTabs,
  Button: RButton
} = window.QuiztinDesignSystem_138691;
const resultsStyles = {
  summary: {
    display: 'flex',
    alignItems: 'center',
    gap: 26,
    padding: '26px 28px',
    marginBottom: 26
  },
  ring: (pct, color) => ({
    width: 120,
    height: 120,
    borderRadius: '50%',
    flex: 'none',
    position: 'relative',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    background: `conic-gradient(${color} ${pct}%, var(--sand-200) 0)`
  }),
  ringInner: {
    position: 'absolute',
    inset: 12,
    background: 'var(--surface-card)',
    borderRadius: '50%'
  },
  ringNum: {
    position: 'relative',
    font: '800 var(--text-3xl) var(--font-display)',
    color: 'var(--text-strong)'
  },
  sumBody: {
    flex: 1,
    minWidth: 0
  },
  sumHead: {
    font: 'var(--type-heading)',
    fontSize: 'var(--text-2xl)',
    color: 'var(--text-strong)',
    marginBottom: 4
  },
  sumSub: {
    font: 'var(--type-body-lg)',
    color: 'var(--text-muted)',
    marginBottom: 14
  },
  sumStats: {
    display: 'flex',
    gap: 22
  },
  sumStat: {
    display: 'flex',
    flexDirection: 'column'
  },
  sumStatNum: {
    font: '800 var(--text-xl) var(--font-display)',
    color: 'var(--text-strong)'
  },
  sumStatLbl: {
    font: 'var(--type-fine)',
    color: 'var(--text-muted)'
  },
  section: {
    marginBottom: 28
  },
  secTitle: {
    font: 'var(--type-heading)',
    fontSize: 'var(--text-xl)',
    color: 'var(--text-strong)',
    marginBottom: 14
  },
  qItem: {
    padding: '16px 18px',
    marginBottom: 10
  },
  qTop: {
    display: 'flex',
    alignItems: 'center',
    gap: 12,
    marginBottom: 12
  },
  qTag: {
    width: 30,
    height: 30,
    borderRadius: '50%',
    background: 'var(--surface-sunken)',
    color: 'var(--text-muted)',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    font: '800 var(--text-sm) var(--font-body)',
    flex: 'none'
  },
  qText: {
    flex: 1,
    font: 'var(--type-body-strong)',
    color: 'var(--text-strong)'
  },
  qPct: {
    font: '800 var(--text-lg) var(--font-display)'
  },
  bar: {
    height: 12,
    borderRadius: 'var(--radius-pill)',
    background: 'var(--sand-200)',
    overflow: 'hidden'
  },
  barFill: (pct, color) => ({
    width: pct + '%',
    height: '100%',
    borderRadius: 'var(--radius-pill)',
    background: color
  }),
  studentRow: {
    display: 'flex',
    alignItems: 'center',
    gap: 14,
    padding: '13px 18px',
    borderBottom: '1px solid var(--border)'
  },
  sAvatar: {
    width: 40,
    height: 40,
    borderRadius: '50%',
    background: 'var(--blueberry-100)',
    color: 'var(--blueberry-800)',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    font: '700 var(--text-sm) var(--font-body)',
    flex: 'none'
  },
  sName: {
    flex: 1,
    font: 'var(--type-body-strong)',
    color: 'var(--text-strong)'
  },
  sWhen: {
    font: 'var(--type-caption)',
    color: 'var(--text-subtle)',
    marginRight: 6
  },
  scorePill: color => ({
    font: '800 var(--text-base) var(--font-body)',
    color: '#fff',
    background: color,
    borderRadius: 'var(--radius-pill)',
    padding: '4px 12px',
    minWidth: 54,
    textAlign: 'center'
  })
};
function QuestionBreakdown({
  q,
  i
}) {
  const TK = window.TK;
  const color = TK.scoreColor(q.correctPct);
  const low = q.correctPct < 60;
  return /*#__PURE__*/React.createElement(RCard, {
    padding: "none",
    style: resultsStyles.qItem
  }, /*#__PURE__*/React.createElement("div", {
    style: resultsStyles.qTop
  }, /*#__PURE__*/React.createElement("span", {
    style: resultsStyles.qTag
  }, i + 1), /*#__PURE__*/React.createElement("span", {
    style: resultsStyles.qText
  }, q.prompt), low && /*#__PURE__*/React.createElement(RBadge, {
    tone: "warning",
    icon: "warning-circle"
  }, "Worth revisiting"), /*#__PURE__*/React.createElement("span", {
    style: {
      ...resultsStyles.qPct,
      color
    }
  }, q.correctPct, "%")), /*#__PURE__*/React.createElement("div", {
    style: resultsStyles.bar
  }, /*#__PURE__*/React.createElement("div", {
    style: resultsStyles.barFill(q.correctPct, color)
  })));
}
function StudentRow({
  s
}) {
  const TK = window.TK;
  return /*#__PURE__*/React.createElement("div", {
    style: resultsStyles.studentRow
  }, /*#__PURE__*/React.createElement("div", {
    style: resultsStyles.sAvatar
  }, s.initials), /*#__PURE__*/React.createElement("span", {
    style: resultsStyles.sName
  }, s.name), /*#__PURE__*/React.createElement("span", {
    style: resultsStyles.sWhen
  }, s.when), /*#__PURE__*/React.createElement("span", {
    style: resultsStyles.scorePill(TK.scoreColor(s.score))
  }, s.score, "%"));
}
function QuizResults({
  quiz
}) {
  const TK = window.TK;
  const [tab, setTab] = React.useState('all');
  const students = tab === 'help' ? TK.RESULT_STUDENTS.filter(s => s.score < 70) : TK.RESULT_STUDENTS;
  const avg = 86;
  return /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(RCard, {
    style: resultsStyles.summary
  }, /*#__PURE__*/React.createElement("div", {
    style: resultsStyles.ring(avg, TK.scoreColor(avg))
  }, /*#__PURE__*/React.createElement("div", {
    style: resultsStyles.ringInner
  }), /*#__PURE__*/React.createElement("span", {
    style: resultsStyles.ringNum
  }, avg, "%")), /*#__PURE__*/React.createElement("div", {
    style: resultsStyles.sumBody
  }, /*#__PURE__*/React.createElement("div", {
    style: resultsStyles.sumHead
  }, "The class is doing well!"), /*#__PURE__*/React.createElement("div", {
    style: resultsStyles.sumSub
  }, "24 of 28 students have submitted The Water Cycle."), /*#__PURE__*/React.createElement("div", {
    style: resultsStyles.sumStats
  }, /*#__PURE__*/React.createElement("div", {
    style: resultsStyles.sumStat
  }, /*#__PURE__*/React.createElement("span", {
    style: resultsStyles.sumStatNum
  }, "86%"), /*#__PURE__*/React.createElement("span", {
    style: resultsStyles.sumStatLbl
  }, "Average")), /*#__PURE__*/React.createElement("div", {
    style: resultsStyles.sumStat
  }, /*#__PURE__*/React.createElement("span", {
    style: resultsStyles.sumStatNum
  }, "24"), /*#__PURE__*/React.createElement("span", {
    style: resultsStyles.sumStatLbl
  }, "Submitted")), /*#__PURE__*/React.createElement("div", {
    style: resultsStyles.sumStat
  }, /*#__PURE__*/React.createElement("span", {
    style: resultsStyles.sumStatNum
  }, "1"), /*#__PURE__*/React.createElement("span", {
    style: resultsStyles.sumStatLbl
  }, "To revisit"))))), /*#__PURE__*/React.createElement(RAI, {
    style: {
      marginBottom: 26
    }
  }, "Most of the class nailed the sun-powered steps. ", /*#__PURE__*/React.createElement("em", null, "Question 3"), " tripped up nearly half \u2014 a quick recap on where most evaporation happens could help before the next lesson."), /*#__PURE__*/React.createElement("section", {
    style: resultsStyles.section
  }, /*#__PURE__*/React.createElement("h2", {
    style: resultsStyles.secTitle
  }, "Question breakdown"), TK.QUESTIONS.map((q, i) => /*#__PURE__*/React.createElement(QuestionBreakdown, {
    key: i,
    q: q,
    i: i
  }))), /*#__PURE__*/React.createElement("section", {
    style: resultsStyles.section
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'space-between',
      marginBottom: 14
    }
  }, /*#__PURE__*/React.createElement("h2", {
    style: {
      ...resultsStyles.secTitle,
      marginBottom: 0
    }
  }, "Students"), /*#__PURE__*/React.createElement(RTabs, {
    variant: "pill",
    value: tab,
    onChange: setTab,
    tabs: [{
      id: 'all',
      label: 'All 24'
    }, {
      id: 'help',
      label: 'Need a hand'
    }]
  })), /*#__PURE__*/React.createElement(RCard, {
    padding: "none"
  }, students.map(s => /*#__PURE__*/React.createElement(StudentRow, {
    key: s.name,
    s: s
  })))));
}
window.TK = Object.assign(window.TK || {}, {
  QuizResults
});
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/teacher/results.jsx", error: String((e && e.message) || e) }); }

__ds_ns.Button = __ds_scope.Button;

__ds_ns.IconButton = __ds_scope.IconButton;

__ds_ns.Badge = __ds_scope.Badge;

__ds_ns.ProgressBar = __ds_scope.ProgressBar;

__ds_ns.Toast = __ds_scope.Toast;

__ds_ns.Tooltip = __ds_scope.Tooltip;

__ds_ns.Checkbox = __ds_scope.Checkbox;

__ds_ns.Radio = __ds_scope.Radio;

__ds_ns.Select = __ds_scope.Select;

__ds_ns.Switch = __ds_scope.Switch;

__ds_ns.TextField = __ds_scope.TextField;

__ds_ns.Icon = __ds_scope.Icon;

__ds_ns.Tabs = __ds_scope.Tabs;

__ds_ns.AIFeedbackCard = __ds_scope.AIFeedbackCard;

__ds_ns.AnswerChoice = __ds_scope.AnswerChoice;

__ds_ns.ResultSummary = __ds_scope.ResultSummary;

__ds_ns.Card = __ds_scope.Card;

__ds_ns.Dialog = __ds_scope.Dialog;

})();
