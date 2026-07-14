/**
 * All landing page copy in one place, so the words stay easy to review and edit.
 * Voice follows ui-rules §1: sentence case, address the reader as "you", warm and
 * encouraging, never exam or testing jargon, no emoji.
 */
import type { Role } from '@/lib/auth/session';

export type Audience = 'students' | 'teachers';

export interface HeroContent {
  audience: Audience;
  tabLabel: string;
  eyebrow: string;
  /** The headline, split so one run can carry the coral highlighter mark. */
  headlineLead: string;
  headlineMark: string;
  headlineTail: string;
  subcopy: string;
  primaryCta: { label: string; to: string };
  secondaryCta: { label: string; to: string };
  /** Persona image alt text (the image itself is imported in the Hero). */
  imageAlt: string;
}

export const HERO: Record<Audience, HeroContent> = {
  students: {
    audience: 'students',
    tabLabel: 'For students',
    eyebrow: 'For students',
    headlineLead: 'Learning that feels like a ',
    headlineMark: 'game',
    headlineTail: ', not a test.',
    subcopy:
      'Join your class with a code, answer at your own pace, and get friendly feedback on every question. No red ink, no pressure, just a calmer way to learn.',
    primaryCta: { label: 'Join your class', to: '/register?role=Student' },
    secondaryCta: { label: 'See how it works', to: '#how-it-works' },
    imageAlt: 'A student smiling while working on a laptop in a library.',
  },
  teachers: {
    audience: 'teachers',
    tabLabel: 'For teachers',
    eyebrow: 'For teachers',
    headlineLead: 'Write a quiz once. Let Quiztin do the ',
    headlineMark: 'grading',
    headlineTail: '.',
    subcopy:
      'Build a quiz in minutes, publish it to your classroom, and see every answer scored the moment a student submits, with AI feedback that saves you hours of marking.',
    primaryCta: { label: 'Create your first quiz', to: '/register?role=Teacher' },
    secondaryCta: { label: 'See how it works', to: '#how-it-works' },
    imageAlt: 'A professor smiling warmly in a library.',
  },
};

/** The role a hero CTA hints on the register screen, so the form can preselect it. */
export const AUDIENCE_ROLE: Record<Audience, Role> = {
  students: 'Student',
  teachers: 'Teacher',
};

/* ---- How it works (the core loop, three steps) --------------------------- */
export type StepKey = 'create' | 'take' | 'feedback';

export interface Step {
  key: StepKey;
  n: string;
  title: string;
  body: string;
}

export const STEPS: Step[] = [
  {
    key: 'create',
    n: '01',
    title: 'Teachers build a quiz',
    body: 'Write your own questions, or start from an AI draft and make it yours, then publish to your classroom in a couple of clicks.',
  },
  {
    key: 'take',
    n: '02',
    title: 'Students take it, calmly',
    body: 'They join with a short class code and answer at their own pace. A gentle progress bar, never a countdown.',
  },
  {
    key: 'feedback',
    n: '03',
    title: 'Everyone gets feedback',
    body: 'Answers are scored the moment a student submits, with supportive AI feedback on every question, for both sides.',
  },
];

/* ---- Value split (one block per audience) -------------------------------- */
export type ValueId = 'for-teachers' | 'for-students';

export interface ValueBlock {
  id: ValueId;
  audience: Audience;
  eyebrow: string;
  title: string;
  body: string;
  points: string[];
  cta: { label: string; to: string };
  imageAlt: string;
}

export const VALUE: ValueBlock[] = [
  {
    id: 'for-teachers',
    audience: 'teachers',
    eyebrow: 'For teachers',
    title: 'Less marking, more teaching.',
    body: 'Quiztin grades the second a student submits, and writes the specific, encouraging feedback you never have time for.',
    points: [
      'Build a quiz in minutes, or start from an AI draft and make it yours.',
      'Publish to a classroom, so only enrolled students can take it.',
      'Every submission scored instantly, with no red pen in sight.',
      'See who is thriving and who needs a hand, at a glance.',
    ],
    cta: { label: 'Create your first quiz', to: '/register?role=Teacher' },
    imageAlt: 'A professor smiling in her office in front of full bookshelves.',
  },
  {
    id: 'for-students',
    audience: 'students',
    eyebrow: 'For students',
    title: 'A quiz that is on your side.',
    body: 'Join with a code, take your time, and learn from feedback that points at the idea, not the grade.',
    points: [
      'Join a class with a short code. Nothing to install.',
      'Answer at your own pace, with a calm progress bar and no timer.',
      'Get friendly feedback on every question, right after you submit.',
      'Misses are framed as things to review, never as failures.',
    ],
    cta: { label: 'Join your class', to: '/register?role=Student' },
    imageAlt: 'Two students studying together over a laptop at a library table.',
  },
];

/* ---- AI feedback spotlight ---------------------------------------------- *
 * The vignette below is an on brand mock of the AI feedback card, built from
 * the design tokens (AC-9), because the real quiz and results screens are not
 * built yet. It is illustrative of the experience, not a screenshot.          */
export const AI_SPOTLIGHT = {
  eyebrow: 'AI feedback',
  title: 'Feedback that sounds like a person, not a red pen.',
  body: 'After every question, Quiztin explains the idea in a warm, encouraging voice. A miss becomes something to review, never a mark against you. And every quiz still scores on its own if the AI is ever away.',
  vignette: {
    question: 'Which layer of the OSI model routes packets between networks?',
    theirAnswer: 'The transport layer',
    status: 'to review',
    feedback:
      'So close. The transport layer carries data end to end, but choosing a route between networks happens one layer down, at the network layer, where IP addresses do their work. Worth another look.',
  },
} as const;

/* ---- FAQ ----------------------------------------------------------------- */
export const FAQ: ReadonlyArray<{ q: string; a: string }> = [
  {
    q: 'Is Quiztin free?',
    a: 'Yes. Quiztin is free for classrooms. Create an account and start building quizzes right away.',
  },
  {
    q: 'How do students join a class?',
    a: 'A teacher shares a short class code. A student creates an account, enters the code, and they are enrolled. Nothing to install.',
  },
  {
    q: 'Is the AI feedback real?',
    a: 'Yes. Quiztin uses a language model to write feedback on each question, and every quiz still scores correctly on its own if the model is ever unavailable.',
  },
  {
    q: 'Do you send student data to the AI?',
    a: 'No. Only the academic content of a question is ever sent for feedback, never a student name, email, or identity.',
  },
  {
    q: 'What kinds of questions can I ask?',
    a: 'Multiple choice to start, with more types on the way. Write your own, or begin from an AI draft and edit it.',
  },
];

/* ---- Closing call to action --------------------------------------------- */
export const CTA_BAND = {
  freeLine: 'Free for your classroom',
  title: 'Ready to make quizzes feel calmer?',
  body: 'Create an account in under a minute. Build a quiz, invite your class, and let Quiztin handle the scoring and the feedback.',
  primary: { label: 'Get started', to: '/register' },
  secondary: { label: 'Sign in', to: '/sign-in' },
} as const;

export const NAV_LINKS: ReadonlyArray<{ label: string; href: string }> = [
  { label: 'How it works', href: '#how-it-works' },
  { label: 'For teachers', href: '#for-teachers' },
  { label: 'For students', href: '#for-students' },
  { label: 'Questions', href: '#faq' },
];

export const FOOTER = {
  tagline: 'A calmer way to run classroom quizzes.',
  /** Honest disclosure: the persona photographs are illustrative, not real users (AC-15). */
  personaDisclosure:
    'The people pictured on this page are illustrative and were generated for the design; they are not real Quiztin users.',
  columns: [
    {
      title: 'Product',
      links: [
        { label: 'How it works', href: '#how-it-works' },
        { label: 'For teachers', href: '#for-teachers' },
        { label: 'For students', href: '#for-students' },
        { label: 'Questions', href: '#faq' },
      ],
    },
    {
      title: 'Get started',
      links: [
        { label: 'Create an account', href: '/register' },
        { label: 'Sign in', href: '/sign-in' },
      ],
    },
  ],
} as const;
