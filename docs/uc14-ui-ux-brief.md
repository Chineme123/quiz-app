# UC14 — UI/UX Design Brief

> Converted from the original Word design doc (AUM corpus). Faithful extraction; original preserved in `quiz-trash/`.

## 🎨 UI/UX Design Brief — UC14: Create User Profile (Post-Registration Completion)

### 🔰 Actor

Role: Authenticated User (Student or Teacher)  Goal: Enrich or update personal academic profile information after registration.  Outcome: Profile data is validated, saved, and immediately reflected in the dashboard.

### 📌 Key Screens & Flows

#### 1️⃣ Dashboard (Entry Point)

Context: User is logged in and viewing their dashboard.

Required UI Elements:

Profile icon (top-right corner)

Dropdown menu

“Manage Profile”

Primary Action:  User clicks Profile icon → selects Manage Profile

#### 2️⃣ Manage Profile Page (Editable Form)

This is the core screen.

The page should be a clean, structured, role-aware form.

#### Layout Sections

##### 🧍 Personal Information Section

Display Name (text input)

Avatar Upload (image upload component with preview)

Bio (multiline text area)

##### 🎓 Academic Information Section

Common fields:

School (text input)

Department (text input)

Conditional fields:

If Student:

Academic Level (dropdown)

Freshman

Sophomore

Junior

Senior

Graduate

If Teacher:

Instructor Type (dropdown)

Professor

Assistant Professor

Teaching Assistant

High School Teacher

Only relevant fields should appear based on role.

#### 🔘 Actions

Primary CTA:

Save

Optional:

Cancel (return to dashboard)

### 🧠 Flows and Variations

#### ✅ Main Success Flow

User opens dashboard.

User selects “Manage Profile.”

System displays pre-filled editable form.

User updates profile fields.

User clicks Save.

System validates:

Required fields (display name)

Role-specific fields (academicLevel or instructorType)

Avatar file type/size

Validation passes.

Profile is saved.

User sees updated profile information.

Optional success banner:

“Profile Updated Successfully”

#### 🔁 Alternate Flow — First-Time Profile Creation

If no profile exists:

Form fields appear empty.

Save action creates new profile.

After save, page refreshes with populated data.

#### ⚠️ Exception Flows

##### E1 — Validation Error

Trigger:

Empty displayName

Missing academicLevel (Student)

Missing instructorType (Teacher)

Behavior:

Inline error messages beneath invalid fields.

Do NOT clear other form fields.

Scroll to first error automatically.

Save button remains enabled after correction.

Example error styles:

Red border

Small red text beneath field

Clear, human-readable message

##### E2 — Unauthorized Attempt

If user somehow attempts to edit another profile:

Display full-page error or redirect to dashboard.

Message: “You are not authorized to modify this profile.”

##### E3 — Persistence Failure (System Error)

If save fails due to server issue:

Show neutral error banner:

“We couldn’t save your changes. Please try again.”

Do not erase form inputs.

### 🧩 Design Notes

#### Role Awareness

The UI must dynamically render fields based on role:

No academicLevel visible for teachers.

No instructorType visible for students.

This reduces cognitive load and prevents invalid input.

#### Validation Behavior

Prefer inline validation.

Real-time validation for:

Required fields

Dropdown selections

Final validation on Save.

#### Avatar Component

Show preview immediately after upload.

Accept image formats only (jpg, png).

Display size restriction hint.

Provide “Remove” or “Change” option.

#### State Feedback

On successful save:

Show subtle confirmation message.

Do not redirect unless necessary.

Maintain form context.

#### Interaction Principles

Pre-fill all existing data.

No password or email fields on this screen.

Maintain visual separation between Personal and Academic sections.

Use clear section headers.

### 🧭 UX Tone & Experience Goals

The experience should feel:

Lightweight (not bureaucratic)

Personal (this is identity)

Trustworthy (clean validation feedback)

Fast (no unnecessary reloads)

This is not an administrative screen — it’s a self-expression space within an academic environment.

### 🏗 Suggested Layout Structure

Consider a card-based layout:

[ Profile Card ]

Avatar

Display Name

Bio

[ Academic Info Card ]

School

Department

(Conditional Role Fields)

[ Save Button ]

Keep vertical spacing generous.

### 🧪 Future-Ready Considerations (Optional)

Although not required immediately:

Progress indicator (“Profile 70% complete”)

Read-only public preview

Badge or completion indicator on dashboard

### 🔐 Context Reminder

This use case:

Occurs post-authentication.

Does not modify login credentials.

Updates identity metadata only.

Must reflect changes immediately after persistence.
