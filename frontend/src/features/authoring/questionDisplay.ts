import type { QuestionType } from './authoring.schemas';

/** How each type is named to a teacher. The wire names are not for reading. */
export const TYPE_LABEL: Record<QuestionType, string> = {
  MultipleChoice: 'Multiple choice',
  TrueFalse: 'True or false',
  ShortAnswer: 'Short answer',
};

/**
 * The shape both a saved question and a generated candidate share. Written structurally so the
 * editor and the review of a pending batch describe an answer with one piece of code.
 */
export interface AnswerBearing {
  questionType: QuestionType;
  options: string[] | null;
  correctOptionIndex: number | null;
  correctAnswerBool: boolean | null;
  correctAnswerText: string | null;
}

/** The answer, written the way the teacher will recognise it. */
export function answerSummary(question: AnswerBearing): string {
  if (question.questionType === 'MultipleChoice') {
    const index = question.correctOptionIndex ?? 0;
    return question.options?.[index] ?? `Choice ${index + 1}`;
  }
  if (question.questionType === 'TrueFalse') {
    return question.correctAnswerBool === true ? 'True' : 'False';
  }
  return question.correctAnswerText ?? '';
}

/** "1 point" or "3 points", never "1 points". */
export function pointsLabel(points: number): string {
  return points === 1 ? '1 point' : `${points} points`;
}
