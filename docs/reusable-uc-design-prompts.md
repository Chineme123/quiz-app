# Reusable Use Case Design Prompts

> Converted from the original Word design doc (AUM corpus). Faithful extraction; original preserved in `quiz-trash/`.

## 🔁 Reusable Use Case Design Prompts

(Based on UC6 design workflow — generalized for any use case or project)

### 1. 🧾 Business Description

"Help me write a business description for this use case. Here's what the use case is about..."  → (Explain the actor, system responsibility, and business goal.)

### 2. 🎯 Clarify and Structure the Use Case

"Help me write the high-level and expanded use case for this scenario. Here's how it was taught in my class..."  → (Describe the TUCBW/TUCEW format, and ask for success/alternate/exception flows.)

### 3. 🧠 Domain Modeling Guidance

"Based on this use case, what should the domain model include — classes, relationships, and attributes?"

"Can you review my domain model and help me refine or justify relationships?"

### 4. 🧩 Design Pattern Identification

"Which GRASP and GoF design patterns should I apply to this use case and why?"  → (Mention system flexibility, reuse, or future extensibility if needed.)

### 5. 🧱 Apply Patterns to UML

"Now generate the UML diagram(s) that apply the Composite / Strategy / Factory pattern for this use case."

### 6. 🧭 Clarify Responsibility & Method Design

"What methods should be in the interface, the composite, and the leaf classes for this use case?"

"Which methods should the controller call directly, and which should it access through polymorphism?"

### 7. ⚙️ Controller–Factory–Strategy Wiring

"Should the controller create or call the factory/strategy, and how should those responsibilities be divided?"

### 8. 📑 Scenario Description Table

"Help me build the scenario description table for each non-trivial step in my expanded use case."

"Apply GRASP and GoF patterns in the scenario table where appropriate."

### 9. 🕹 Sequence Diagram Construction

"Now design a formal sequence diagram for this step, including function calls with parameters and return types."

### 10. 🧱 Derive Design Class Diagram

"Now derive the Design Class Diagram from all applied patterns and sequence diagrams. Show attributes, methods, and relationships."

### 11. 🧑‍🎨 UI/UX Design Brief for Figma

"Generate a UI/UX design brief for this use case that I can hand over to a designer working in Figma."  → (Keep it friendly, focused on flow, screen elements, actions, and success/failure paths.)

### 12. 📦 Export to PDF or Deliverable

"Can you generate this as a PDF I can attach or submit?"

### 13. 🧹 Project Documentation Support

"Can you give me a compiled list of the prompts I used to complete this use case?"

"Now give me a general set of prompts I can reuse for other use cases in the future."

cs.zany-rotary-phone-q55pv74wx6wh99qp.main
