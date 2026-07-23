import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe } from 'vitest-axe';
import { MemoryRouter, Routes, Route } from 'react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ToastProvider } from '@/components/ui';
import { ClassQuizListPage } from './ClassQuizListPage';
import * as api from './authoring.api';

vi.mock('./authoring.api');

const CLASS_ID = '33333333-0000-0000-0000-000000000003';
const QUIZ_ID = '44444444-0000-0000-0000-000000000004';
const TEACHER_ID = '11111111-0000-0000-0000-000000000001';

function renderList() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <MemoryRouter initialEntries={[`/classrooms/${CLASS_ID}/quizzes`]}>
          <Routes>
            <Route path="classrooms/:classroomId/quizzes" element={<ClassQuizListPage />} />
            <Route path="classrooms/:classroomId" element={<h1>The class</h1>} />
            <Route path="quizzes/:quizId/edit" element={<h1>Quiz editor</h1>} />
            <Route path="dashboard" element={<h1>Your classes</h1>} />
          </Routes>
        </MemoryRouter>
      </ToastProvider>
    </QueryClientProvider>,
  );
}

function createdQuiz() {
  return {
    id: QUIZ_ID,
    title: 'Photosynthesis',
    durationMinutes: 10,
    classroomId: CLASS_ID,
    teacherId: TEACHER_ID,
    isPublished: false,
    availableFrom: null,
    availableTo: null,
    maxAttempts: 1,
    isLocked: false,
    questions: [],
  };
}

describe('ClassQuizListPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(api.getClassroomQuizzes).mockResolvedValue([
      {
        id: QUIZ_ID,
        title: 'Cells and organelles',
        isPublished: true,
        questionCount: 3,
        attemptCount: 2,
      },
    ]);
  });

  it('lists each quiz with its state and counts', async () => {
    renderList();

    expect(await screen.findByText('Cells and organelles')).toBeInTheDocument();
    expect(screen.getByText(/Published/)).toBeInTheDocument();
    expect(screen.getByText(/3 questions/)).toBeInTheDocument();
    expect(screen.getByText(/2 attempts so far/)).toBeInTheDocument();
  });

  it('points a teacher at the next step when the class has no quizzes yet', async () => {
    vi.mocked(api.getClassroomQuizzes).mockResolvedValue([]);

    renderList();

    expect(await screen.findByText(/create your first one/i)).toBeInTheDocument();
  });

  it('creates a quiz and opens it in the editor', async () => {
    vi.mocked(api.createQuiz).mockResolvedValue(createdQuiz());
    renderList();
    await screen.findByText('Cells and organelles');

    await userEvent.type(screen.getByLabelText(/title/i), 'Photosynthesis');
    await userEvent.click(screen.getByRole('button', { name: /create quiz/i }));

    expect(api.createQuiz).toHaveBeenCalledWith(CLASS_ID, 'Photosynthesis', 10);
    expect(await screen.findByRole('heading', { name: 'Quiz editor' })).toBeInTheDocument();
  });

  it('asks for a title before it will create anything', async () => {
    renderList();
    await screen.findByText('Cells and organelles');

    await userEvent.click(screen.getByRole('button', { name: /create quiz/i }));

    expect(await screen.findByText(/give your quiz a title/i)).toBeInTheDocument();
    expect(api.createQuiz).not.toHaveBeenCalled();
  });

  it('reads as missing when the class is not yours', async () => {
    vi.mocked(api.getClassroomQuizzes).mockResolvedValue(null);

    renderList();

    expect(
      await screen.findByRole('heading', { name: /couldn.t find that class/i }),
    ).toBeInTheDocument();
  });

  it('has no accessibility violations', async () => {
    const { container } = renderList();
    await screen.findByText('Cells and organelles');

    expect(await axe(container)).toHaveNoViolations();
  });
});
