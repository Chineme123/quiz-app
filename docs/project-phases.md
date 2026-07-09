# Project Phases (Roadmap)

> Converted from the original Word design doc (AUM corpus). Faithful extraction; original preserved in `quiz-trash/`.

#### Phase 1 — Get the core loop working (highest ROI)

UC6 Create Classroom Quiz (Teacher)  You define the Quiz + Question model and the “publishable” concept.

UC8 Take Classroom Quiz (Student)  Forces Attempt/Submission modeling and validates your quiz format.

UC9 View My Results & Progress (Student)  You now have the full loop: create → take → see outcome.

UC10 View Classroom Results (Teacher)  Same data, different read pattern; exposes aggregation and access control needs.

This phase gives you a demoable system fast.

#### Phase 2 — Make it real for teachers (assignment + analytics + export)

UC7 Assign Quiz (Teacher)  Introduces “who should take what” and deadlines (a huge real-world requirement).

UC12 Configure Grading Style (Teacher) (FR13/FR14)  Do after assignment exists because the “deadline passed” constraint becomes meaningful.

UC11 View Quiz Metrics (Teacher)  Metrics depend on attempts existing; you’ll compute average/max/min cleanly now.

UC13 Export Quiz Results (Teacher)  Export is easy once your result queries are stable; do it after metrics/results.

#### Phase 3 — Add classrooms + enrollment (structure + access rules)

UC2 Create Classroom (Teacher)  Establishes ownership and rosters.

UC3 Join Classroom (Student)  Enables enrollment; now FR7 (“only enrolled students can take”) becomes enforceable.

(You can swap UC2/UC3 earlier if you want classrooms from day 1, but for time optimization, you can fake a “default class” while building the core loop.)

#### Phase 4 — Add practice mode (nice-to-have, not core)

UC4 Create Practice Quiz (Student)

UC5 Take Practice Quiz (Student)

## 🚀 Phase 2 — UserService Core (Proposed UCs)

Let’s design this properly.

### UC14 – Create User Profile (Post-Registration Completion)

Business Value:  Users complete profile beyond authentication claims.

Behavior:

Save display name

Avatar

Bio

Academic level (optional)

Instructor metadata (department)

Why this matters:  You now have a real User aggregate.

### UC15 – View & Update My Profile

User retrieves profile

User updates allowed fields

Validate uniqueness rules (e.g., username)

This creates:

Update workflows

Concurrency

Validation domain rules

### UC16 – Manage Account Status (Admin)

Activate / deactivate user

Suspend account

Role upgrade (Student → Teacher)

Now UserService has:

Account lifecycle

Role transitions

Administrative control

That’s real domain power.

### UC17 – Manage Roles & Permissions

Assign roles

Remove roles

View user permissions

Now you have:

Role aggregate

Possibly Policy-based access control

### UC18 – Configure Notification Preferences

Now you introduce:

UserPreferences entity

Notification toggles (email, in-app)

Preference-based filtering for NotificationService

This connects cleanly to NotificationService later.

### UC19 – View My Activity Summary

User-level dashboard:

Total quizzes taken

Average score

Classrooms joined

This forces:

Cross-service read model queries

Or projection from events

Now UserService becomes meaningful.
