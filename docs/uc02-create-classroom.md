# UC2 — Create Classroom (Teacher)

> ⚠️ **Placeholder — needs a real design pass.** The original `UC2 – Create Classroom.docx` was a **mis-titled copy of UC6** (its body is the Create-Quiz use case under a Create-Classroom title), so there is no genuine UC2 design. The broken original is preserved in `quiz-trash/Quiz Application (original docx corpus)/`.

UC2 is **in scope for v1** (see `context/foundation.md` §8): creating a classroom is what makes enrolment (UC3) and FR7 (only enrolled students take a classroom's quizzes) real.

When designed, follow the AUM pattern used by the other UCs (`reusable-uc-design-prompts.md`): business description → domain model → high-level (TUCBW/TUCEW) + expanded use case → scenario table → GRASP/GoF patterns → sequence & class diagrams → UI/UX brief.

**What's already known from the code/foundation:**
- A `Classroom` is owned by a teacher and has a privacy setting (open vs. invite-only).
- `Classroom` and `Enrollment` entities already exist in QuizService; `Enrollment` has a unique `(StudentId, ClassroomId)` index.
- Identity is the teacher's `Guid UserId` from the JWT (`foundation.md` §7 #14).
