import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { axe } from 'vitest-axe';
import { Select } from './Select';

const LEVELS = [
  { value: 'Freshman', label: 'Freshman' },
  { value: 'Senior', label: 'Senior' },
];

// AC-6 / AC-15: the role dropdown must be labelled, expose its options, and be clean.
describe('Select', () => {
  it('associates the label with a combobox and lists its options', () => {
    render(<Select label="Academic level" options={LEVELS} />);
    const select = screen.getByLabelText('Academic level');
    expect(select).toBeInstanceOf(HTMLSelectElement);
    expect(screen.getByRole('option', { name: 'Freshman' })).toBeInTheDocument();
    expect(screen.getByRole('option', { name: 'Senior' })).toBeInTheDocument();
  });

  it('renders a disabled placeholder option when nothing is selected', () => {
    render(<Select label="Academic level" placeholder="Select your academic level" options={LEVELS} />);
    const placeholder = screen.getByRole('option', { name: 'Select your academic level' });
    expect(placeholder).toBeDisabled();
    expect((placeholder as HTMLOptionElement).selected).toBe(true);
  });

  it('marks the control invalid and describes it with the error', () => {
    render(<Select label="Academic level" options={LEVELS} error="Select your academic level." />);
    const select = screen.getByLabelText(/academic level/i);
    expect(select).toBeInvalid();
    expect(select).toHaveAccessibleDescription('Select your academic level.');
  });

  it('has no accessibility violations', async () => {
    const { container } = render(
      <Select label="Academic level" placeholder="Select your academic level" options={LEVELS} />,
    );
    expect(await axe(container)).toHaveNoViolations();
  });
});
