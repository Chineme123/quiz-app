import { useState } from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe } from 'vitest-axe';
import { Dialog } from './Dialog';

/**
 * The modal's contract (design system `surfaces/Dialog`). Worth its own test because it is the
 * required shape for confirming anything irreversible: if Escape stops closing it, or focus
 * escapes it, every confirm flow in the app degrades at once.
 */
describe('Dialog', () => {
  function renderDialog(props: Partial<React.ComponentProps<typeof Dialog>> = {}) {
    const onClose = vi.fn();
    const result = render(
      <>
        <button type="button">Outside before</button>
        <Dialog
          open
          onClose={onClose}
          title="Archive this class?"
          description="Nothing is deleted."
          footer={
            <>
              <button type="button">Keep it open</button>
              <button type="button">Yes, archive it</button>
            </>
          }
          {...props}
        />
        <button type="button">Outside after</button>
      </>,
    );
    return { onClose, ...result };
  }

  it('renders as a modal dialog, labelled and described by its own text', () => {
    renderDialog();

    const dialog = screen.getByRole('dialog');
    expect(dialog).toHaveAttribute('aria-modal', 'true');
    expect(dialog).toHaveAccessibleName('Archive this class?');
    expect(dialog).toHaveAccessibleDescription('Nothing is deleted.');
  });

  it('renders nothing when closed', () => {
    renderDialog({ open: false });
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('closes on Escape, the close button, and a backdrop click alike', async () => {
    const user = userEvent.setup();

    const escape = renderDialog();
    await user.keyboard('{Escape}');
    expect(escape.onClose).toHaveBeenCalledTimes(1);
    escape.unmount();

    const closeButton = renderDialog();
    await user.click(screen.getByRole('button', { name: 'Close' }));
    expect(closeButton.onClose).toHaveBeenCalledTimes(1);
    closeButton.unmount();

    const backdrop = renderDialog();
    // The scrim is the dialog's parent element; a press on it and nowhere else closes.
    await user.click(screen.getByRole('dialog').parentElement!);
    expect(backdrop.onClose).toHaveBeenCalledTimes(1);
  });

  it('does not close when the press starts inside the panel', async () => {
    const user = userEvent.setup();
    const { onClose } = renderDialog();

    await user.click(screen.getByRole('button', { name: 'Keep it open' }));

    // Only the footer button's own handler ran; the dialog did not close under the user.
    expect(onClose).not.toHaveBeenCalled();
  });

  it('keeps Tab inside the panel so focus cannot wander to the page behind', async () => {
    const user = userEvent.setup();
    renderDialog();

    const dialog = screen.getByRole('dialog');

    // Tab well past the end of the dialog's three controls. Focus must wrap back into the panel
    // every time rather than reaching the buttons rendered outside it.
    for (let i = 0; i < 6; i++) {
      await user.tab();
      expect(dialog.contains(document.activeElement)).toBe(true);
    }

    // And the same walking backwards.
    for (let i = 0; i < 6; i++) {
      await user.tab({ shift: true });
      expect(dialog.contains(document.activeElement)).toBe(true);
    }
  });

  it('returns focus to whatever opened it', async () => {
    const user = userEvent.setup();

    function Harness() {
      const [open, setOpen] = useState(false);
      return (
        <>
          <button type="button" onClick={() => setOpen(true)}>
            Archive this class
          </button>
          <Dialog open={open} onClose={() => setOpen(false)} title="Archive this class?" />
        </>
      );
    }

    render(<Harness />);
    const opener = screen.getByRole('button', { name: 'Archive this class' });
    await user.click(opener);
    expect(screen.getByRole('dialog')).toBeInTheDocument();

    await user.keyboard('{Escape}');

    // A keyboard user is put back where they were, not dropped at the top of the page.
    expect(opener).toHaveFocus();
  });

  it('has no accessibility violations', async () => {
    const { container } = renderDialog();
    expect(await axe(container)).toHaveNoViolations();
  });
});
