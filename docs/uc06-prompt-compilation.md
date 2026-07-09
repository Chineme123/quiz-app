# UC6 — Prompt Compilation

> Converted from the original Word design doc (AUM corpus). Faithful extraction; original preserved in `quiz-trash/`.

📚 Prompt Compilation for UC6 Documentation

#### 1. Business Description Initiation

"Let's start with a business description."

#### 2. Describing the Flow for Quiz Creation

"This is my idea of the process of creating a quiz..."  (described how the teacher sets config, chooses AI or manual, reviews questions, publishes quiz)

#### 3. Clean Rewrite of Business Description

"Now rewrite the business description."

#### 4. Domain Model Review

"This is the domain model I created for this particular use case..."  → (you requested structural feedback and best practices)

#### 5. Clarifying Teacher–Quiz Relationship

"Since teachers create quizzes..."  → (we discussed whether to add a direct relationship or rely on classroom ownership)

#### 6. UC6 Use Case Formatting

"This is how we write high-level and expanded use cases in class..."  → (you shared formatting and structure, and I conformed the expanded UC6 accordingly)

#### 7. Design Pattern Selection

"The next step is to identify design patterns... our professor usually tells us which ones."  → (we identified applicable GRASP + GoF patterns)

#### 8. Question about Composite Pattern Client

"In the composite pattern, who is the client? Will it be the controller?"

#### 9. Clarifying Interface Methods

"For this use case, these are the methods I think should be in the QuizComponent interface..."

#### 10. What Does the Leaf Get?

"So what does the leaf (question) get?"

#### 11. Method Allocation by Class

"Give me a list (not code) of the methods that will be in QuizComponent, Quiz, and Question."

#### 12. Factory Pattern Clarification

"Implementing the factory pattern like you suggested, would the controller interact with the QuestionFactory? Should it include setPrompt and others?"

#### 13. Question about Factory Subclassing

"So each subclass won't implement its own version of createQuestion()??"

#### 14. What Should the UML Look Like for This Factory Setup?

"So what would the diagram look like?"

#### 15. Implementing Composite Pattern UML

"Now do the UML for the composite pattern."

#### 16. Controller and Strategy Pattern

"For the strategy pattern, will the controller call QuestionGenerationStrategy which then picks the right algorithm...?"

#### 17. UML for Strategy Pattern

"Yeah help me out with the UML."

#### 18. Scenario Description Table (Non-trivial Step 1)

"Let’s start with the first in which I provided an incomplete version..."

#### 19. Scenario Description Table (Non-trivial Step 2)

"Let’s do step 6 now."

#### 20. Formal Sequence Diagrams

"Design the formal sequence diagrams for both..."

#### 21. Error Troubleshooting (PlantUML)

"I have a syntax error."

#### 22. UML A, C, I Meaning

"In UML diagrams, I see A, C, and I tags. I know Interface and Class but what is A?"

#### 23. UI/UX Handoff Brief Setup

"Now generate a document that I can send to the UI/UX guy so he can design the UI and flow on Figma."

#### 24. PDF Export

"I need a PDF."

#### 25. Final Prompt - Documentation Purpose

"Can you give me a compilation of prompts based on this chat that allowed me to complete my UC6 document."
