import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { axe } from 'vitest-axe';
import { TextField } from './TextField';

// AC-6 / AC-15: the form primitives must be labelled, wire their hint/error to the
// control for assistive tech, and carry no axe violations.
describe('TextField', () => {
  it('associates the visible label with the input', () => {
    render(<TextField label="Display name" />);
    expect(screen.getByLabelText('Display name')).toBeInstanceOf(HTMLInputElement);
  });

  it('exposes the hint as the accessible description of the input', () => {
    render(<TextField label="Bio" hint="A sentence or two about you." />);
    expect(screen.getByLabelText(/bio/i)).toHaveAccessibleDescription('A sentence or two about you.');
  });

  it('marks the control invalid and describes it with the error when in error', () => {
    render(<TextField label="Email" error="Enter a valid email address." />);
    const input = screen.getByLabelText(/email/i);
    expect(input).toBeInvalid();
    expect(input).toHaveAccessibleDescription('Enter a valid email address.');
  });

  it('reflects required to assistive tech', () => {
    render(<TextField label="Display name" required />);
    expect(screen.getByLabelText(/display name/i)).toBeRequired();
  });

  it('renders a textarea when multiline', () => {
    render(<TextField label="Bio" multiline />);
    expect(screen.getByLabelText(/bio/i)).toBeInstanceOf(HTMLTextAreaElement);
  });

  it('has no accessibility violations', async () => {
    const { container } = render(<TextField label="Display name" hint="Your name" />);
    expect(await axe(container)).toHaveNoViolations();
  });

  it('has no accessibility violations in the error state', async () => {
    const { container } = render(<TextField label="Email" error="Enter a valid email address." />);
    expect(await axe(container)).toHaveNoViolations();
  });
});
