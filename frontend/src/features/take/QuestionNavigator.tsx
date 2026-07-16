interface QuestionNavigatorProps {
  total: number;
  currentIndex: number;
  answeredIndexes: Set<number>;
  onJump: (index: number) => void;
}

/**
 * The grid that shows where you are, what you have answered, and lets you jump anywhere
 * (spec 0006, AC-17). Real buttons in a list, so it is keyboard operable and a screen reader
 * announces both the number and its state, rather than colour being the only signal (AC-19).
 */
export function QuestionNavigator({ total, currentIndex, answeredIndexes, onJump }: QuestionNavigatorProps) {
  return (
    <nav aria-label="Questions">
      <ul className="grid grid-cols-5 gap-2 lg:grid-cols-4">
        {Array.from({ length: total }, (_, index) => {
          const answered = answeredIndexes.has(index);
          const current = index === currentIndex;
          // Colour alone never carries this: the label spells the state out for a screen reader.
          const state = answered ? 'answered' : 'not answered yet';

          return (
            <li key={index}>
              <button
                type="button"
                onClick={() => onJump(index)}
                aria-current={current ? 'step' : undefined}
                aria-label={`Question ${index + 1}, ${state}`}
                className={[
                  'flex size-11 items-center justify-center rounded-chip font-body text-sm transition-colors',
                  'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary/50',
                  current
                    ? 'bg-primary text-white ring-2 ring-primary ring-offset-2'
                    : answered
                      ? 'bg-primary/10 text-primary hover:bg-primary/20'
                      : 'bg-surface-card text-text-muted hover:text-text-body border border-border',
                ].join(' ')}
              >
                {index + 1}
              </button>
            </li>
          );
        })}
      </ul>
    </nav>
  );
}
