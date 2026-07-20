/** Central query-key registry, so keys stay consistent across the app. */
export const qk = {
  profile: ['profile'] as const,
  attemptResult: (attemptId: string) => ['attemptResult', attemptId] as const,
  availableQuizzes: ['availableQuizzes'] as const,
  attemptQuestions: (attemptId: string) => ['attemptQuestions', attemptId] as const,
  ownedClassrooms: ['ownedClassrooms'] as const,
  enrolledClassrooms: ['enrolledClassrooms'] as const,
  classroomByCode: (code: string) => ['classroomByCode', code] as const,
  classroom: (classroomId: string) => ['classroom', classroomId] as const,
  classroomRoster: (classroomId: string, page: number) =>
    ['classroomRoster', classroomId, page] as const,
};
