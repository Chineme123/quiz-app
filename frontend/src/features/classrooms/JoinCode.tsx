import { useState } from 'react';
import { Button } from '@/components/ui';

/**
 * A class join code, plus the one action a teacher actually wants: the shareable link on the
 * clipboard (spec 0008, AC-2).
 *
 * The code is set in Space Mono (`font-mono`) and spaced out, because it gets read off a board
 * and dictated over a call. The link is composed here from the current origin rather than
 * returned by the server, so the API needs no public base URL configured.
 */
export function JoinCode({ code }: { code: string }) {
  const [copied, setCopied] = useState<'link' | 'code' | null>(null);

  async function copy(what: 'link' | 'code') {
    const text = what === 'link' ? `${window.location.origin}/join/${code}` : code;
    try {
      await navigator.clipboard.writeText(text);
      setCopied(what);
      window.setTimeout(() => setCopied(null), 2000);
    } catch {
      // Clipboard access can be refused (permissions, an insecure origin). The code is on
      // screen either way, so there is nothing to recover: stay quiet rather than alarm.
    }
  }

  return (
    <div className="flex flex-col items-start gap-2">
      <div>
        <span className="font-body text-xs text-text-muted">Join code</span>
        <p className="font-mono text-lg tracking-widest text-text-strong">{code}</p>
      </div>

      <div className="flex gap-2">
        <Button size="sm" variant="secondary" onClick={() => void copy('code')}>
          Copy code
        </Button>
        <Button size="sm" variant="secondary" onClick={() => void copy('link')}>
          Copy link
        </Button>
      </div>

      {/* Announced politely so a screen reader confirms the copy without stealing focus. */}
      <p aria-live="polite" className="font-body text-xs text-text-muted">
        {copied === 'link' ? 'Link copied.' : copied === 'code' ? 'Code copied.' : ''}
      </p>
    </div>
  );
}
