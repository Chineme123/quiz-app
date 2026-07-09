# UC8 — Take Classroom Quiz (Student)

> Converted from the original Word design doc (AUM corpus). Faithful extraction; original preserved in `quiz-trash/`.

## UC8: Take Classroom Quiz (Student)

Supports: FR7 (FR7 is a precondition/rule: must be enrolled)

### Business Description:

This use case defines how the system enables a student to take an assessment within a classroom they are enrolled in, ensuring controlled access, accurate evaluation, immediate feedback, and result transparency.

The system allows a student to access their classroom dashboard and retrieve all quizzes associated with that classroom. Quizzes are presented according to their availability state: completed, currently available, or scheduled for future availability. The system enforces quiz availability rules defined by the instructor, preventing students from starting quizzes outside the permitted time window.

When a student selects an available quiz, the system displays the quiz’s general information, including its title, description, and availability constraints, and requires explicit confirmation before the quiz session begins. Upon confirmation, the system initiates the quiz attempt and records the student’s responses.

During the quiz attempt, the system ensures that all responses are associated with the correct student, quiz, and classroom context. Once the student submits the quiz, the system evaluates the submission, calculates the score, and finalizes the attempt to prevent further modification.

The system immediately provides the student with their results and updates the classroom dashboard to reflect the completed quiz and achieved score. The system also allows the student to reopen the completed quiz to review detailed performance, including correct and incorrect responses. For each question, the system generates contextual AI-driven feedback to support learning and reinforce understanding.

This use case supports the business objectives of fair assessment delivery, timely performance feedback, and enhanced learning outcomes through guided review, while maintaining integrity and consistency across classroom evaluations.

### Domain Model:

### UC8 High-Level Use Case:

TUCBW (The Use Case Begins With) Student selects the quiz they want to take and clicks the “Take Quiz” button.

TUCEW (The Use Case Ends With) The Student views Feedback/Performance

### UC8 Expanded Use Case:

| Actor (Student) | System (Quiz App) |
| --- | --- |
|   | 0. Quiz App displays the classroom dashboard. |
| 1. TUCBW Student selects the quiz they want to take and clicks the “Take Quiz” button. | 2. Quiz App displays the quiz information dialog |
| 3. Student confirms the quiz information and clicks “Start Quiz”. | 4. Quiz App creates a new QuizAttempt and displays the quiz questions.* |
| 5. Student answers the questions and clicks “Submit Quiz”. | 6a. Quiz App validates the submission, evaluates the responses, calculates the score, and finalizes the QuizAttempt.  6b. Quiz App displays the student’s results and updates the classroom dashboard with the achieved score.* |
| 7. Student selects the completed quiz to review performance details. | 8. Quiz App displays detailed question-by-question results and AI-generated feedback.* |
| 9. TUCEW Student views Feedback/Performance |   |

### Scenario Description:

| Step | Subject | Subject Action | Other Data / Objects | Object Acted Upon |
| --- | --- | --- | --- | --- |
| 4.1 | Quiz App (Controller) | Initiates quiz attempt creation | studentId, quizId | TakeQuizFacade |
| 4.2 | TakeQuizFacade | Coordinates creation workflow | studentId, quizId | QuizAttemptService |
| 4.3 | QuizAttemptService | Creates in-progress attempt | attempt metadata (studentId, quizId) | QuizAttempt |
| 4.4 | QuizAttempt | Sets initial state | state = InProgress | QuizAttempt |
| 4.5 | TakeQuizFacade | Returns questions | QuizAttempt, Quiz definition | Quiz UI |

| Step | Subject | Subject Action | Other Data / Objects | Object Acted Upon |
| --- | --- | --- | --- | --- |
| 6a.1 | Quiz App (Controller) | Sends submission request | attemptId, responses | SubmitQuizCommand |
| 6a.2 | SubmitQuizCommand | Delegates submission workflow | attemptId, responses | TakeQuizFacade |
| 6a.3 | TakeQuizFacade | Finalizes attempt & locks responses | attemptId, responses | QuizAttemptService |
| 6a.4 | QuizAttemptService | Updates state to Submitted | attempt status | QuizAttempt |
| 6a.5 | TakeQuizFacade | Delegates evaluation | QuizAttempt | AttemptEvaluator |
| 6a.6 | AttemptEvaluator | Applies scoring strategy | QuizAttempt.responses | ScoringStrategy |
| 6a.7 | ScoringStrategy | Returns score report | QuizAttempt data | AttemptEvaluator |
| 6a.8 | AttemptEvaluator | Persists score & updates state | ScoreReport | QuizAttempt |

| Step | Subject | Subject Action | Other Data / Objects | Object Acted Upon |
| --- | --- | --- | --- | --- |
| 6b.1 | TakeQuizFacade | Constructs result view | ScoreReport, attemptId | QuizResultFormatter |
| 6b.2 | QuizResultFormatter | Renders results | ScoreReport | Quiz App UI |
| 6b.3 | EventDispatcher | Emits graded event | QuizAttemptGradedEvent | Observers |
| 6b.4 | DashboardProjectionUpdater | Updates dashboard state | QuizAttemptGradedEvent | ClassroomDashboard |
| 6b.5 | Quiz App UI | Displays updated quiz score | Dashboard data | Student view |

| Step | Subject | Subject Action | Other Data / Objects | Object Acted Upon |
| --- | --- | --- | --- | --- |
| 8.1 | Student | Requests review of results | attemptId | Quiz Review UI |
| 8.2 | Quiz Review UI | Requests detailed feedback | attemptId | TakeQuizController |
| 8.3 | TakeQuizController | Delegates review fetch | attemptId | TakeQuizFacade |
| 8.4 | TakeQuizFacade | Fetches attempt & feedback | attemptId | QuizAttemptService |
| 8.5 | QuizAttemptService | Retrieves responses, score, feedback | QuizAttempt, Feedback entities | ReviewData |
| 8.6 | Quiz Review UI | Displays detailed feedback | ReviewData | Student view |

### Applying Patterns:

#### ➡️ State Pattern:

#### ➡️ Facade Pattern:

#### ➡️ Observer Pattern:

#### ➡️ Command Pattern:

#### ➡️ Strategy Pattern:

### Sequence Diagram:

### Design Class Diagram:

### Extra Notes (AI generated):

#### Preconditions

The student is authenticated.

The student is enrolled in the classroom associated with the quiz.

The classroom exists and is active.

The quiz exists, is published, and is currently within its availability window.

The student has not exceeded any attempt limits defined by business rules (if applicable).

#### Postconditions (Success)

A new QuizAttempt is created with correct links to student and quiz.

The student’s responses are recorded and stored.

The student’s attempt transitions through appropriate states (e.g., NotStarted → InProgress → Submitted → Graded → Reviewable).

A score report is calculated and stored with the attempt.

The classroom dashboard reflection of the student’s quiz score is updated.

AI-generated feedback for each question is available for review (if feedback generation strategy is configured).

Observers such as dashboard projection and notifications are triggered following grading.

#### Postconditions (Failure)

No quiz attempt is finalized if the submission fails or validation fails.

Partial or in-progress attempt data is preserved only if supported by business rules (e.g., auto-save or drafts).

Appropriate error states and messages are returned to the student, allowing correction and reattempt where applicable.

#### Main Success Flow

Student opens the classroom dashboard.

Student selects an available quiz and clicks Take Quiz.

System displays quiz information (title, instructions, availability timeframe).

Student confirms and clicks Start Quiz.

System creates a new QuizAttempt (status = InProgress) and displays questions.

Student answers the quiz questions.

Student clicks Submit Quiz.

System validates the submission and transitions the attempt to the Submitted state.

System evaluates the attempt using a configured scoring strategy (e.g., auto, partial credit, rubric).

System calculates a score and transitions the attempt to the Graded state.

System triggers related domain events (e.g., QuizAttemptGradedEvent) for downstream consumers.

System updates the classroom dashboard to reflect the student's quiz score.

System generates AI feedback for each question according to configured feedback strategy (e.g., explain mistakes, hint-only, AI-based).

Student views preliminary results including score summary.

Student can click the completed quiz attempt to view detailed results and feedback.

#### Alternate Flow A1 — Async Feedback Generation

A1. At step 13, if detailed AI feedback is enabled and runs asynchronously:  A2. System immediately returns the score and a placeholder indicating “feedback pending.”  A3. System enqueues feedback generation via a background job process.  A4. Once feedback is ready, a notification is issued (if configured).  A5. Student refreshes or revisits the review page to see completed feedback.  A6. System displays full per-question feedback along with scores.

#### Exception Flow E1 — Quiz Not Within Availability Window

E1.1 Student selects a quiz that has not yet opened or is already closed.  E1.2 System displays a message stating the quiz is not currently available.  E1.3 Student returns to the classroom dashboard without creating an attempt.  E1.4 Use case ends.

#### Exception Flow E2 — Submission Validation Fails

E2.1 Student submits quiz with missing or invalid responses (based on question policies).  E2.2 System highlights errors (e.g., required answer missing, invalid format).  E2.3 Student corrects responses and clicks Submit Quiz.  E2.4 System resumes at step 8 of the Main Success Flow.

#### Exception Flow E3 — Evaluation or Scoring Error

E3.1 After submission, system fails to execute the scoring strategy (e.g., scoring subsystem unavailable).  E3.2 System returns an error message and logs the issue.  E3.3 Student is returned to dashboard or shown retry option based on business rules.  E3.4 If configured, an administrative alert or retry queue is triggered.

#### Exception Flow E4 — Feedback Generation Fails

E4.1 System fails to generate feedback due to AI service issues, timeouts, or invalid attempt data.  E4.2 System logs the issue and informs the student “Feedback currently unavailable.”  E4.3 Student’s score is shown without detailed feedback.  E4.4 System optionally retries feedback generation based on backoff or scheduling policy.

#### Business Rules (Contextual Notes)

Quiz availability is strictly enforced based on configured open/close times.

If multiple attempts are allowed, tracking is maintained per student and enforced.

Scoring strategies must be configured per quiz or globally by instructor policy.

Feedback strategies can vary by question type or global setting.

Observers may trigger downstream updates such as dashboard refresh, notification delivery, or analytics logging.

Commands representing submission operations are idempotent using unique command identifiers to avoid duplicate application.

#### Usability Notes

The quiz interface auto-saves intermediate responses if supported to minimize data loss.

The dashboard displays color-coded quiz statuses (e.g., Completed, Available, Scheduled).

Feedback pages show question-by-question statuses and optionally expandable explanations.
