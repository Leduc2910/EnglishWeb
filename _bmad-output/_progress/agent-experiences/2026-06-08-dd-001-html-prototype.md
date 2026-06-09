# Agent Experience: DD-001 HTML Prototype

**Date:** 2026-06-08  
**Agent:** Freya  
**Context:** WDS Phase 4 `[W] Visual Design` for DD-001 MVP Test Workflows

## What Worked

An HTML prototype was the best fit because the user wanted something people could see quickly. The project already had detailed page specs and no formal design system mode, so a single static prototype gave immediate review value without introducing Figma/tooling friction.

## Design Decisions

- Use a calm operational SaaS visual style: neutral surfaces, teal primary actions, amber pending states, green success states.
- Make the prototype flow-based rather than route-based: sidebar navigation lets reviewers jump across all 15 screens.
- Preserve key UX decisions from the specs:
  - Teacher Dashboard stays a summary surface.
  - Test creation starts from "Bài test".
  - Student class confirmation is explicit before login.
  - Exam workspace keeps PDF/audio and answer form side by side.
  - Speaking upload distinguishes uploaded draft from submitted.
  - Results grading uses a master-detail layout with player, score, and feedback together.

## Artifact

`_bmad-output/D-Design-System/01-Visual-Design/design-concepts/dd-001-mvp-test-workflows-prototype.html`

## Caveat

The prototype is for visual review and UX feedback. It is not production code and should not override the page specifications for exact validation, access control, or backend behavior.
