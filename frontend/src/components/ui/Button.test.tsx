import { render, screen } from '@testing-library/react';
import { axe } from 'vitest-axe';
import { Button } from './Button';

// Smoke test for the whole UI test harness: Vitest + jsdom + Testing Library +
// vitest-axe + the token CSS import path. Fuller primitive coverage is task 8.
describe('Button', () => {
  it('renders its label as an accessible button', () => {
    render(<Button>Save changes</Button>);
    expect(screen.getByRole('button', { name: 'Save changes' })).toBeInTheDocument();
  });

  it('is disabled and marked busy while loading', () => {
    render(<Button loading>Save</Button>);
    const button = screen.getByRole('button');
    expect(button).toBeDisabled();
    expect(button).toHaveAttribute('aria-busy', 'true');
  });

  it('has no accessibility violations', async () => {
    const { container } = render(<Button>Continue</Button>);
    expect(await axe(container)).toHaveNoViolations();
  });
});
