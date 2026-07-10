import { useState } from 'react';
import type { ReactNode } from 'react';
import { useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Button, Card, Select, TextField, useToast } from '@/components/ui';
import { useAuth } from '@/lib/auth/useAuth';
import type { Role } from '@/lib/auth/session';
import { ValidationError } from '@/lib/api/errors';
import { toUserMessage } from '@/lib/api/errorMessage';
import { useProfileQuery, useUpdateProfile } from './useProfile';
import { mapProfileErrors } from './profile.errors';
import type { ProfileFieldKey } from './profile.errors';
import {
  ACADEMIC_LEVEL_OPTIONS,
  INSTRUCTOR_TYPE_OPTIONS,
  profileFormSchema,
  toFormValues,
  toUpdateRequest,
} from './profile.schemas';
import type { Profile, ProfileFormValues } from './profile.schemas';

export function ManageProfilePage() {
  const { user } = useAuth();
  const query = useProfileQuery();

  // The route is guarded, so a signed-in user is always present; this is defensive.
  if (!user) return null;

  if (query.isPending) {
    return <ProfileState>Loading your profile…</ProfileState>;
  }
  if (query.isError) {
    return (
      <ProfileState>
        <p className="text-text-body">We couldn&rsquo;t load your profile.</p>
        <Button className="mt-4" onClick={() => void query.refetch()}>
          Try again
        </Button>
      </ProfileState>
    );
  }

  // A fresh key per loaded identity/version remounts the form so its defaults reseed.
  return <ProfileForm key={query.data?.updatedAt ?? 'new'} initial={query.data} role={user.role} />;
}

function ProfileState({ children }: { children: ReactNode }) {
  return (
    <div className="mx-auto max-w-reading">
      <h1 className="font-display text-3xl text-text-strong">Manage your profile</h1>
      <div className="mt-8 text-text-muted">{children}</div>
    </div>
  );
}

function ProfileForm({ initial, role }: { initial: Profile | null; role: Role }) {
  const toast = useToast();
  const updateProfile = useUpdateProfile();

  const {
    register,
    handleSubmit,
    setError,
    setFocus,
    reset,
    control,
    formState: { errors },
  } = useForm<ProfileFormValues>({
    resolver: zodResolver(profileFormSchema),
    defaultValues: toFormValues(initial),
  });

  // Drive the avatar preview off the live field values (useWatch, not watch(), so
  // the subscription is memo-safe).
  const displayName = useWatch({ control, name: 'displayName' });
  const avatarUrl = useWatch({ control, name: 'avatarUrl' });

  const onSubmit = (values: ProfileFormValues) => {
    updateProfile.mutate(toUpdateRequest(values, role), {
      onSuccess: (saved) => {
        reset(toFormValues(saved));
        toast.show({ tone: 'success', title: 'Saved', message: 'Your profile has been updated.' });
      },
      onError: (error) => {
        // AC-12: the server's string[] validation errors land on their own fields.
        if (error instanceof ValidationError) {
          const mapped = mapProfileErrors(error.errors);
          const keys = Object.keys(mapped.fields) as ProfileFieldKey[];
          for (const key of keys) {
            setError(key, { type: 'server', message: mapped.fields[key] });
          }
          if (keys[0]) setFocus(keys[0]);
          if (mapped.form.length > 0) {
            toast.show({ tone: 'danger', message: mapped.form.join(' ') });
          }
          return;
        }
        // AC / UC14 E3: a persistence failure keeps every input and shows a neutral line.
        toast.show({
          tone: 'danger',
          message: toUserMessage(error, 'We couldn’t save your changes. Please try again.'),
        });
      },
    });
  };

  return (
    <div className="mx-auto max-w-reading">
      <header>
        <h1 className="font-display text-3xl text-text-strong">Manage your profile</h1>
        <p className="mt-2 text-text-body">
          This is your space in Quiztin. Only the fields that apply to you are shown.
        </p>
      </header>

      <form
        onSubmit={(e) => void handleSubmit(onSubmit)(e)}
        noValidate
        className="mt-8 flex flex-col gap-8"
      >
        <Card padding="lg">
          <SectionHeading title="Personal information" description="How you show up across Quiztin." />
          <div className="flex flex-col gap-5">
            <AvatarField displayName={displayName} avatarUrl={avatarUrl}>
              <TextField
                label="Avatar image URL"
                type="url"
                inputMode="url"
                placeholder="https://…"
                hint="Paste a link to an image (jpg or png)."
                autoComplete="off"
                error={errors.avatarUrl?.message}
                {...register('avatarUrl')}
              />
            </AvatarField>
            <TextField
              label="Display name"
              autoComplete="name"
              required
              error={errors.displayName?.message}
              {...register('displayName')}
            />
            <TextField
              label="Bio"
              multiline
              rows={4}
              optional
              hint="A sentence or two about you."
              error={errors.bio?.message}
              {...register('bio')}
            />
          </div>
        </Card>

        <Card padding="lg">
          <SectionHeading title="Academic information" description="Where you study or teach." />
          <div className="flex flex-col gap-5">
            <TextField label="School" optional autoComplete="organization" {...register('school')} />
            <TextField label="Department" optional {...register('department')} />
            {role === 'Student' ? (
              <Select
                label="Academic level"
                placeholder="Select your academic level"
                options={ACADEMIC_LEVEL_OPTIONS}
                required
                error={errors.academicLevel?.message}
                {...register('academicLevel')}
              />
            ) : (
              <Select
                label="Instructor type"
                placeholder="Select your instructor type"
                options={INSTRUCTOR_TYPE_OPTIONS}
                required
                error={errors.instructorType?.message}
                {...register('instructorType')}
              />
            )}
          </div>
        </Card>

        <div className="flex justify-end">
          <Button type="submit" size="lg" loading={updateProfile.isPending}>
            Save changes
          </Button>
        </div>
      </form>
    </div>
  );
}

function SectionHeading({ title, description }: { title: string; description: string }) {
  return (
    <div className="mb-5">
      <h2 className="font-display text-lg text-text-strong">{title}</h2>
      <p className="mt-1 text-text-muted">{description}</p>
    </div>
  );
}

/** Live avatar preview beside the URL control. A broken link falls back to the initial. */
function AvatarField({
  displayName,
  avatarUrl,
  children,
}: {
  displayName: string;
  avatarUrl: string;
  children: ReactNode;
}) {
  // Track the URL that failed rather than a boolean, so a new URL is retried without
  // an effect: showImage recomputes from the current url vs. the last broken one.
  const [brokenUrl, setBrokenUrl] = useState<string | null>(null);

  const url = avatarUrl.trim();
  const initial = (displayName.trim().charAt(0) || '?').toUpperCase();
  const showImage = url !== '' && brokenUrl !== url;

  return (
    <div className="flex items-start gap-4">
      <span
        className="grid size-16 shrink-0 place-items-center overflow-hidden rounded-full bg-primary-soft font-display text-2xl text-primary"
        aria-hidden="true"
      >
        {showImage ? (
          <img
            src={url}
            alt=""
            className="size-full object-cover"
            onError={() => setBrokenUrl(url)}
          />
        ) : (
          initial
        )}
      </span>
      <div className="flex-1">{children}</div>
    </div>
  );
}
