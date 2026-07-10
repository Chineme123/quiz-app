import type { ReactNode } from 'react';

/** Centered full-height message, for loading / empty / error whole-page states. */
export function FullPageMessage({ title, children }: { title: string; children?: ReactNode }) {
  return (
    <div className="grid min-h-screen place-items-center bg-bg p-6">
      <div className="text-center">
        <p className="font-display text-xl text-text-strong">{title}</p>
        {children && <div className="mt-2 text-text-muted">{children}</div>}
      </div>
    </div>
  );
}
