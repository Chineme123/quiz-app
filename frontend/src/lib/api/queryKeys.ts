/** Central query-key registry, so keys stay consistent across the app. */
export const qk = {
  profile: ['profile'] as const,
  attemptResult: (attemptId: string) => ['attemptResult', attemptId] as const,
};
