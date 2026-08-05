<template>
  <component
    :is="href ? 'a' : 'button'"
    class="vs-button"
    :class="[`is-${variant}`, `is-${size}`, { 'is-loading': loading, 'is-icon-only': iconOnly }]"
    :type="href ? undefined : type"
    :href="href || undefined"
    :disabled="href ? undefined : disabled || loading"
    :aria-disabled="disabled || loading || undefined"
    :aria-busy="loading || undefined"
    :aria-label="ariaLabel || undefined"
  >
    <span v-if="loading" class="vs-button__spinner" aria-hidden="true"></span>
    <span v-if="$slots.icon" class="vs-button__icon" aria-hidden="true"><slot name="icon" /></span>
    <span v-if="!iconOnly" class="vs-button__label"><slot /></span>
  </component>
</template>

<script setup>
defineProps({
  variant: { type: String, default: 'primary' },
  size: { type: String, default: 'medium' },
  type: { type: String, default: 'button' },
  href: { type: String, default: '' },
  disabled: Boolean,
  loading: Boolean,
  iconOnly: Boolean,
  ariaLabel: { type: String, default: '' },
})
</script>

<style scoped>
.vs-button {
  min-height: var(--control-height-md);
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: var(--space-2);
  padding: 0 var(--space-4);
  border: 1px solid transparent;
  border-radius: var(--radius-control);
  font: inherit;
  font-weight: 700;
  line-height: 1;
  text-decoration: none;
  transition: background-color var(--transition-fast), border-color var(--transition-fast), color var(--transition-fast);
}
.vs-button:focus-visible { outline: 3px solid color-mix(in srgb, var(--border-focus) 40%, transparent); outline-offset: 2px; }
.vs-button.is-primary { background: var(--interactive-primary); color: var(--text-on-interactive); }
.vs-button.is-primary:hover:not(:disabled) { background: var(--interactive-primary-hover); }
.vs-button.is-primary:active:not(:disabled) { background: var(--interactive-primary-active); }
.vs-button.is-secondary { background: var(--surface-default); border-color: var(--border-default); color: var(--text-primary); }
.vs-button.is-secondary:hover:not(:disabled) { background: var(--surface-hover); border-color: var(--border-strong); }
.vs-button.is-ghost { color: var(--text-link); }
.vs-button.is-ghost:hover:not(:disabled) { background: var(--surface-hover); }
.vs-button.is-danger { background: var(--status-danger-text); color: var(--text-on-interactive); }
.vs-button.is-link { min-height: auto; padding: 0; color: var(--text-link); }
.vs-button.is-small { min-height: var(--control-height-sm); padding-inline: var(--space-3); font-size: var(--type-dense-size); }
.vs-button.is-large { min-height: var(--control-height-lg); padding-inline: var(--space-6); }
.vs-button.is-icon-only { width: var(--control-height-md); padding: 0; }
.vs-button.is-icon-only.is-small { width: var(--control-height-sm); }
.vs-button:disabled, .vs-button[aria-disabled='true'] { cursor: not-allowed; opacity: .62; }
.vs-button__icon { display: grid; place-items: center; width: 1.125rem; height: 1.125rem; }
.vs-button__spinner { width: 1rem; height: 1rem; border: 2px solid currentColor; border-right-color: transparent; border-radius: 50%; animation: vs-spin .7s linear infinite; }
@keyframes vs-spin { to { transform: rotate(360deg); } }
@media (prefers-reduced-motion: reduce) { .vs-button__spinner { animation-duration: 1.4s; } }
</style>
