import '@testing-library/jest-dom/vitest';
import { cleanup } from '@testing-library/react';
import { afterEach, expect } from 'vitest';
import * as matchers from 'vitest-axe/matchers';

// Accessibility assertions: expect(await axe(container)).toHaveNoViolations().
expect.extend(matchers);

afterEach(() => {
  cleanup();
});
