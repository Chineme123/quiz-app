# UC14 — Create User Profile

> Converted from the original Word design doc (AUM corpus). Faithful extraction; original preserved in `quiz-trash/`.

## UC14 – Create User Profile (Post-Registration Completion)

### Business Description:

Following initial account registration, users are provided the ability to complete and enrich their profile information within the system. While registration captures only essential authentication credentials (e.g., email, password, role), this use case enables users to supply additional contextual and academic attributes that enhance personalization, identification, and system functionality.

The system shall allow users to update and maintain the following profile information:

Display Name

Avatar/Profile Picture

Biography (Bio)

Academic Level (for students)

School and Department (for students and teachers)

Instructor Type (for teachers, e.g., Professor, Assistant Professor, Teaching Assistant, High School Teacher)

This enriched profile data supports several business objectives:

Improved Identity Representation – Users can present themselves clearly within classrooms and quizzes, promoting recognition and professionalism.

Role-Based Differentiation – Instructor types and academic levels allow the system to contextualize users appropriately within academic workflows.

Enhanced Personalization – Profile metadata enables future system capabilities such as filtering, targeted notifications, analytics, and academic segmentation.

Credibility and Trust Building – Completed profiles increase transparency and legitimacy within academic environments.

This use case strengthens the platform’s academic integrity and supports scalability for future features such as institutional analytics, department-based reporting, and advanced role-based permissions.

### Domain Model:

### UC14 High-Level Use Case:

TUCBW (The Use Case Begins With) User selects the Profile icon from the dashboard.

TUCEW (The Use Case Ends With) User views the updated and saved profile enrichment information.

### UC14 Expanded Use Case:

| Actor (User) | System (Quiz App) |
| --- | --- |
|   | 0. Quiz App displays the user dashboard. |
| 1. TUCBW (The Use Case Begins With) User clicks on profile Icon on the top right corner of dashboard. | 2. Quiz App displays a dropdown menu containing the “Manage Profile” option. |
| 3. User selects the “Manage Profile” option. | 4. Quiz App displays the Manage Profile page with a pre-filled editable form. |
| 5. User enters or updates profile information and selects the “Save” option. | 6. Quiz App validates the submitted information. 6a. If validation fails, Quiz App displays error messages and prompts the user to correct invalid fields.* 6b. If validation succeeds, Quiz App stores the updated profile information.* |
| 7. TUCEW (The Use Case Ends With) User views the updated profile information displayed on the screen. |   |

### Scenario Description:

| Step | Subject | Subject Action | Other Data/Objects | Object Acted Upon |
| --- | --- | --- | --- | --- |
| 6.1 | User | Clicks “Save” | ProfileForm | UserProfileController |
| 6.2 | UserProfileController | Receives | ProfileUpdateRequest | ProfileUpdateRequest |
| 6.3 | UserProfileController | Retrieves | userId | User |
| 6.4 | UserProfileController | Selects strategy based on role | User.role | ProfileUpdateStrategy |
| 6.5 | UserProfileController | Invokes updateProfile | ProfileUpdateRequest | ProfileUpdateStrategy |
| 6.6 | ProfileUpdateStrategy | Applies updates | ProfileUpdateRequest | Profile |
| 6.7 | ProfileUpdateStrategy | Requests validation | Updated Profile | Profile |
| 6.8 | Profile | Validates own fields | displayName, bio, school, department | Profile |
| 6.9 | ProfileUpdateStrategy | Performs role-specific validation | academicLevel / instructorType | Profile |
| 6.10 | ProfileUpdateStrategy | Returns validation result | ValidationStatus | UserProfileController |
| 6.11 | UserProfileController | Persists profile | Validated Profile | ProfileRepository |
| 6.12 | ProfileRepository | Saves | Profile | Database |
| 6.13 | UserProfileController | Returns updated profile | Profile | QuizApp UI |
| 6.14 | User | Views updated profile information | Updated Profile | Dashboard/ProfilePage |

### Applying Patterns:

#### ➡️ Strategy Pattern:

### Sequence Diagram:

### Design Class Diagram:

### Extra Notes (AI generated):

#### Preconditions

The user is authenticated and has access to the system dashboard.

The user account exists in the system with a valid userId.

The user has the appropriate role (Student or Teacher).

The user has permission to modify their own profile.

If a profile already exists, it is retrievable by userId.

#### Postconditions (Success)

The user’s profile enrichment data is validated according to business rules.

The updated Profile object is persisted and associated with the User.

The Profile’s updatedAt timestamp is refreshed.

Role-specific attributes (academicLevel or instructorType) are stored consistently with the user’s role.

The system returns the updated profile data for display.

The dashboard reflects the enriched profile information.

If avatar updates are included, the new avatar reference is stored and accessible.

No unauthorized data outside the user’s profile is modified.

#### Postconditions (Failure)

No changes are persisted if validation fails.

The existing profile state remains unchanged.

Validation errors are returned to the user for correction.

Role-specific validation errors are clearly identified (e.g., missing academicLevel for Student).

System-level errors (if any) are logged without exposing internal details to the user.

#### Main Success Flow

User opens the dashboard.

User clicks the profile icon and selects “Manage Profile.”

System displays a pre-filled editable profile form.

User updates one or more fields (e.g., displayName, bio, academicLevel).

User clicks “Save.

System retrieves the user’s Profile entity using userId.

System selects the appropriate ProfileUpdateStrategy based on the user’s role.

Strategy applies updates to the Profile.

Profile validates its own core attributes (e.g., displayName non-empty).

Strategy performs role-specific validation (e.g., academicLevel required for Student).

System confirms validation success.

System persists the updated Profile via ProfileRepository.

System returns updated profile data.

User views updated profile information on the screen.

#### Alternate Flow A1 — Profile Does Not Yet Exist

A1.1 User attempts to update profile but no Profile entity exists. A1.2 System initializes a new Profile object associated with the user. A1.3 System continues with validation and persistence steps. A1.4 User views newly created profile information.

#### Exception Flow E1 — Validation Fails

E1.1 User submits profile with missing or invalid fields. E1.2 Profile validation detects errors (e.g., empty displayName). E1.3 Strategy validation detects role-specific inconsistencies (e.g., missing instructorType for Teacher). E1.4 System returns validation messages without persisting changes. E1.5 User corrects errors and resubmits. E1.6 Use case resumes at validation step in Main Success Flow.

#### Exception Flow E2 — Unauthorized Profile Modification

E2.1 System detects that user is attempting to modify a profile not associated with their userId. E2.2 System denies access and logs the security event. E2.3 User is redirected to dashboard or shown an authorization error message. E2.4 No profile changes occur.

#### Exception Flow E3 — Persistence Failure

E3.1 Validation succeeds, but repository save operation fails (e.g., database unavailable). E3.2 System logs the error. E3.3 User receives a generic error message indicating update could not be completed. E3.4 No partial update is committed.

#### Business Rules (Contextual Notes)

Each user may have at most one Profile associated with their account. Profile lifecycle is dependent on User lifecycle (composition relationship). Display name must be non-empty once profile is created. AcademicLevel is required if and only if User role = Student. InstructorType is required if and only if User role = Teacher. School and Department fields are free-text values (no institutional validation enforced at this stage). Role-specific validation logic is encapsulated within ProfileUpdateStrategy implementations. Profile entity is responsible for validating its own core attributes (Information Expert principle). Profile persistence operations are handled exclusively through ProfileRepository (Pure Fabrication). Strategy selection eliminates conditional branching in the controller (Open/Closed Principle). Profile updates must not modify authentication credentials (email, passwordHash).

#### Usability Notes

The profile form is pre-filled with existing data to reduce friction. Only relevant role-based fields are displayed (e.g., academicLevel shown only for students). Clear inline validation messages are displayed adjacent to invalid fields. Avatar uploads validate file type and size before persistence. Changes are reflected immediately after successful save. User should be able to revisit and edit profile multiple times.
