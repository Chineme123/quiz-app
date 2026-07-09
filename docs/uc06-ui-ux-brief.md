# UC6 — UI/UX Design Brief

> Converted from the original Word design doc (AUM corpus). Faithful extraction; original preserved in `quiz-trash/`.

## 🎨 UI/UX Design Brief — UC6: Create Classroom Quiz (Teacher)

### 🔰 Actor

Role: Teacher  Goal: Create a quiz for students in a specific classroom — either manually or with AI assistance.  Outcome: The quiz is published, ready to be taken by students.

### 📌 Key Screens & Flows

#### 1. Classroom Dashboard

Entry point for the teacher.

Must include a visible “Add Quiz” button per classroom.

#### 2. Quiz Setup Form

When teacher clicks “Add Quiz”, system shows a form to configure the quiz:

Required Fields:

Quiz Title (text)

Instructions (rich text)

Total number of questions (numeric)

Time Duration (dropdown or input)

Availability Window (start + end date/time)

Scoring Settings (e.g., per question, per type)

Question Type Composition:

Teacher specifies % breakdown for question types

Multiple Choice

Multi-selection

True/False

Short Answer

AI Generation Toggle (FR8 Support):

OFF: System creates empty template with pre-arranged placeholders for question types.

ON: Additional controls appear:

Select Difficulty (dropdown)

Upload or Paste Study Material (file input + text box)

Choose Generation Mode:

Use Base LLM Only

Use Provided Material

Web Search Assistance

CTA: [Next]

#### 3. Quiz Template Page

Depending on AI setting:

AI Off:

System displays an empty template (e.g., 10 placeholders: 5 MCQ, 3 T/F, 2 Short Answer).

Each placeholder includes controls to:

Edit prompt

Add options (for MCQ/MS)

Select correct answer

AI On:

System displays editable generated questions. Same interaction as above, but with prefilled content.

Global Controls:

Add/Remove questions

Reorder questions

Preview quiz

CTA: [Publish Quiz] (and optionally [Save Draft] if supported)

#### 4. Post-Publish Confirmation

Once published:

Show success message: “Quiz Published”

Optionally redirect to:

Quiz List

Classroom Dashboard

View Quiz Page

### 🧠 Flows and Variations

#### ✅ Success Flow

Teacher enters valid config

Chooses AI or not

Reviews or edits questions

Publishes quiz

Sees confirmation

#### 🔁 Alternate Flow – AI Generation

AI generates questions from selected mode (materials, web, LLM only)

Teacher reviews and edits

Continues to publish as usual

#### ⚠️ Exceptions

Invalid Composition: %s don't add to 100 — show inline error

Incomplete Questions: Can’t publish if questions are missing correct answers or invalid fields

AI Failure: Notify + give option to retry or switch to manual mode

### 🧩 Design Notes

User Role: Always authenticated as teacher  Context: Quiz is being created within a specific classroom

#### Patterns (for devs, optional hint for component structure)

Use wizard flow: Setup → Edit → Publish

Validation should be instant where possible (client-side)

Each question component can be thought of as a card (collapsible/editable)
