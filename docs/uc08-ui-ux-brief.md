# UC8 — UI/UX Design Brief

> Converted from the original Word design doc (AUM corpus). Faithful extraction; original preserved in `quiz-trash/`.

## 🎨 UI/UX Design Brief — UC8: Take Classroom Quiz (Student)

### 🔰 Actor

Role: Student  Goal: Take a published quiz within a classroom they are enrolled in, receive immediate results, and review detailed performance with AI-generated feedback.  Outcome: The quiz attempt is completed, scored, reflected on the dashboard, and available for detailed review.

## 📌 Key Screens & Flows

### 1. Classroom Dashboard

Entry point for the student.

#### Must Include:

List of quizzes associated with the classroom.

Each quiz displays:

Title

Status badge:

Available

Scheduled (future)

Completed

Closed

Availability window (if applicable)

Score (if completed)

Attempt indicator (e.g., 1/2 attempts used, if supported)

#### Primary Action:

Take Quiz (only enabled if quiz is currently available)

#### Secondary Action:

View Results (if completed)

#### Disabled States:

Future quizzes → disabled with “Opens on [date]”

Closed quizzes → disabled with “Closed”

### 2. Quiz Information Dialog

Triggered when student clicks Take Quiz.

#### Displays:

Quiz title

Description / instructions

Time limit

Number of questions

Availability window

Attempt limits (if applicable)

#### Actions:

Primary CTA: Start Quiz

Secondary: Cancel

#### Validation:

If quiz becomes unavailable (expired during dialog), show:

“This quiz is no longer available.”

Redirect to dashboard

### 3. Quiz Taking Screen

Displayed after student confirms Start Quiz.

#### Layout Structure

Top Bar:

Quiz title

Countdown timer (if timed)

Progress indicator (e.g., Question 3 of 10)

Left Sidebar (Optional but Recommended):

Question navigation list

Status indicators:

Answered

Unanswered

Flagged

Main Content Area:

Question text

Response input based on question type:

Multiple choice (radio buttons)

Multi-selection (checkboxes)

Short answer (text input)

Long answer (textarea)

Bottom Controls:

Previous

Next

Flag Question

Submit Quiz (persistent CTA)

#### Behavior:

Responses are saved automatically (if supported)

Highlight unanswered questions before submission

Warn when timer is near expiration

### 4. Submission Confirmation Modal

Triggered when student clicks Submit Quiz.

#### Displays:

“Are you sure you want to submit?”

Summary:

X answered

Y unanswered

Warning:

“You will not be able to edit your answers after submission.”

#### Actions:

Confirm Submit (Primary)

Go Back (Secondary)

### 5. Results Summary Screen

Immediately shown after successful submission.

#### Displays:

Final Score (large visual emphasis)

Percentage

Visual indicator (progress bar or badge)

Short performance message

Attempt status: Completed

#### Dashboard Update:

The classroom dashboard should now reflect:

Completed state

Score displayed

“View Results” enabled

#### Actions:

View Detailed Results

Return to Dashboard

### 6. Detailed Review Screen

Accessed when student clicks completed quiz.

#### For Each Question:

Question text

Student’s answer

Correct answer (if policy allows)

Points earned

AI-generated feedback section

#### AI Feedback Block:

Clearly separated visually

Label: “AI Feedback”

Expand/collapse toggle

Distinct styling (card or highlighted container)

#### If Feedback is Generating (Async Flow):

Show:

“Generating feedback…”

Placeholder loader

Optional refresh button

## 🧠 Flows and Variations

### ✅ Success Flow

Student opens dashboard  Selects available quiz  Reviews quiz info  Clicks Start Quiz  Completes questions  Submits quiz  Sees score immediately  Dashboard updates  Student can review detailed results and AI feedback

### 🔁 Alternate Flow — Async AI Feedback

If feedback generation runs in background:

Student sees score immediately.

Feedback section shows “Feedback pending.”

Once ready, feedback appears on refresh or revisit.

### ⚠️ Exceptions

#### E1 — Quiz Not Available

If quiz is outside availability window:

Show message:  “This quiz is not currently available.”

Disable Take button

Return to dashboard

#### E2 — Submission Validation Fails

If required answers missing:

Highlight problematic questions

Scroll to first issue

Prevent submission

#### E3 — Evaluation Error

If scoring service fails:

Show:  “We encountered an issue grading your quiz.”

Provide retry or return option

#### E4 — Feedback Generation Fails

If AI service fails:

Display score normally

Feedback block shows:  “Feedback currently unavailable.”

## 🧩 Design Notes

User Role: Authenticated student enrolled in classroom  Context: Quiz is taken within a specific classroom environment

#### Interaction Style:

Clean, minimal distractions

Clear progression and state feedback

Avoid anxiety-inducing UI

Confirmation before irreversible actions

#### Status Visualization:

Color-coded quiz states:

Green → Completed

Blue → Available

Gray → Scheduled

Red → Closed

#### Accessibility:

High contrast

Keyboard navigable

Screen reader friendly

Timer should have visual + accessible cues

## 🎯 Deliverables for Figma

Designer should provide:

Wireframes for each screen

High-fidelity UI components

All states (default, loading, disabled, error)

Confirmation modals

Empty states

Mobile-responsive layout

Clear CTA hierarchy
