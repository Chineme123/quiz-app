# UC6 — Create Classroom Quiz (Teacher)

> Converted from the original Word design doc (AUM corpus). Faithful extraction; original preserved in `quiz-trash/`.

## UC6: Create Classroom Quiz (Teacher)

Supports: FR8 (question types are part of the main flow / variations)

### Business Description:

The Quiz Web Application allows teachers to create and publish quizzes within a classroom to evaluate student understanding in a structured and flexible way. In this use case, a teacher initiates quiz creation from an existing classroom and configures the quiz by defining its key properties, including the quiz title, instructions, duration, availability period, scoring settings, total number of questions, and the distribution of supported question types.

The system supports two quiz creation approaches. A teacher may choose to manually author questions or to generate an initial set of questions using artificial intelligence. When AI generation is selected, the teacher specifies generation preferences such as difficulty level, source materials, and generation mode. The system then produces an editable draft quiz that conforms to the configured structure and question type distribution.

After the quiz structure or draft questions are presented, the teacher reviews and edits the questions as needed to ensure accuracy, clarity, and alignment with learning objectives. Once satisfied, the teacher publishes the quiz, making it available within the classroom according to the configured availability rules.

This use case supports the business goal of streamlining quiz creation, enabling consistent and reusable assessments, and giving teachers flexibility to create quizzes efficiently while maintaining full control over final content.

### Domain Model:

### UC6 High-Level Use Case:

TUCBW (The Use Case Begins With) The teacher clicks “Add Quiz” Button

TUCEW (The Use Case Ends With) The teacher views “Quiz Published” Message

### UC6 Expanded Use Case:

| Actor(Teacher) | System(Quiz App) |
| --- | --- |
|   | 0) Quiz app displays classroom dashboard |
| 1) TUCBW Teacher clicks the “Add Quiz” button. | 2)Quiz App displays the Quiz Setup form (quiz details and composition fields). |
| 3) Teacher enters required quiz setup information and clicks “Create Quiz”. | 4) Quiz App validates the entries, creates the quiz template, and displays the editable quiz template.* |
| 5) Teacher edits the quiz template (adds/edits questions) and clicks “Publish Quiz”. | 6) Quiz App validates the quiz questions and displays the “Quiz Published” message.* |
| 7) TUCEW Teacher views the “Quiz Published” message. |   |

### Scenario Description:

| Step | Subject | Subject Action | Other Data/Objects | Object Acted Upon |
| --- | --- | --- | --- | --- |
| 3.1 | Teacher | Fills | Form Fields | QuizConfigurationForm |
| 3.2 | Teacher | Clicks | “Create Quiz” | QuizConfigurationForm |
| 4.1 | QuizConfigurationForm | Sends | QuizConfig | QuizController |
| 4.2 | QuizController | Validates | QuizConfig | QuizConfig |
| 4.3 | QuizController | Selects | generationMode | QuestionGenerationStrategy |
| 4.4 | QuizController | generateQuestions | QuizConfig | generationMode |
| 4.5 | QuestionGenerationStrategy | Returns generated questions | List of Questions | QuizController |
| 4.6 | QuizController | Creates Quiz object | QuizConfig | Classroom |
| 4.7 | QuizController | Adds questions to quiz | List of Questions | Quiz |
| 4.8 | QuizController | Returns quiz for display | Quiz | QuizTemplate |

| Step | Subject | Subject Action | Other Data/Objects | Object Acted Upon |
| --- | --- | --- | --- | --- |
| 5.1 | Teacher | Edits | Quiz | QuizTemplate |
| 5.2 | Teacher | Clicks “Publish Quiz” | QuizTemplate | QuizController |
| 6.1 | QuizController | Validates | Quiz | Quiz |
| 6.2 | Quiz | Iterates over questions | List of QuestionComponents | Question |
| 6.3 | Question | Validates own fields | prompt, answer, options | Question |
| 6.4 | Quiz | Aggregates results | Valid/Invalid flags | Quiz |
| 6.5 | QuizController | Publishes quiz | Validated Quiz | Quiz |
| 6.6 | Quiz | Updates status | State = “published” | Quiz |
| 6.7 | QuizController | Returns success message | "Quiz Published" message | QuizApp UI |

### Applying Patterns:

#### Composite Pattern:

#### Simple Factory Pattern:

#### Strategy Pattern:

### Sequence Diagram:

### Design Class Diagram:

### Extra Notes (AI generated):

#### Preconditions

The teacher is authenticated.

The teacher has access rights to the selected classroom (is the classroom’s teacher or approved instructor).

The classroom exists and is active.

#### Postconditions (Success)

A new quiz is created, saved, and published for the classroom.

Quiz contains the configured number of questions and allowed question types.

Quiz settings (title, duration, availability window, scoring rules, etc.) are stored.

#### Postconditions (Failure)

No quiz is published. Any draft content may be saved as a draft only if the system supports drafts (optional; you can decide this as a business rule).

#### Main Success Flow (Manual Authoring, AI off)

The teacher opens the target classroom.

Teacher clicks Create Quiz.

The system displays the Quiz Setup form.

The teacher enters quiz details (e.g., title, instructions, duration, availability window, scoring settings).

Teacher specifies quiz composition:

total number of questions

distribution by question type (e.g., multiple choice, multi-selection, short answer, long answer, etc.)

Teacher selects AI generation = Off.

Teacher clicks Next.

The system generates an empty quiz template matching the requested structure (correct number of question placeholders per type).

The teacher fills in each question and required fields (e.g., choices + correct answers where applicable).

The teacher reviews the completed quiz.

Teacher clicks Publish Quiz.

System validates the quiz (required fields, type rules, totals).

The system publishes the quiz and confirms success.

#### Alternate Flow A1 — AI Generation On (Generate Draft Questions)

A1.  At step 6, the teacher selects AI generation = On.

A2.  The system displays AI options (difficulty, source materials, whether to allow web search vs base model only).

A3.  The teacher selects difficulty, attaches/pastes materials (optional), and chooses generation mode (web search allowed or base model only).

A4.  Teacher clicks Next.

A5.  The system generates questions that match the configured composition.

A6.  The system displays the generated quiz in editable form.

A7.  The teacher edits, replaces, deletes, or adds questions as needed.

A8.  Continue at step 10 of the main flow.

#### Exception Flow E1 — Invalid Quiz Composition

If question type percentages don’t sum to 100% (or total questions can’t be allocated cleanly),

The system highlights the issue and requests correction.

Teacher updates composition.

Resume at step 5.

#### Exception Flow E2 — Validation Fails on Publish

If required fields are missing (e.g., correct answer not set, duration invalid, availability window invalid),

The system shows validation errors.

The teacher fixes issues.

Resume at step 10 or step 9 depending on where the error occurred.

#### Exception Flow E3 — AI Generation Fails

If AI generation cannot complete (timeout, invalid materials, service unavailable),

System notifies teacher and offers:

retry generation, or

switch AI generation off and continue with manual template.

The teacher selects an option and continues accordingly.
