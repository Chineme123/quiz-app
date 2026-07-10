import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { qk } from '@/lib/api/queryKeys';
import { getProfile, updateProfile } from './profile.api';
import type { Profile } from './profile.schemas';

/** The current user's profile. `data` is null for a first-time user (404). */
export function useProfileQuery() {
  return useQuery({ queryKey: qk.profile, queryFn: getProfile });
}

/** Save the profile, then prime the cache with what came back so a later revisit
 *  reads the saved values without a refetch. */
export function useUpdateProfile() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: updateProfile,
    onSuccess: (saved) => {
      queryClient.setQueryData<Profile | null>(qk.profile, saved);
    },
  });
}
