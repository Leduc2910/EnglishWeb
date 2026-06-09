# Sprint Change Proposal: Homework And Live Exam Modes

**Date:** 2026-06-08  
**Status:** accepted for artifact correction  
**Scope:** DD-001 MVP Test Workflows  
**Requested by:** Duc  

---

## Change Summary

EnglishTestWeb must support two student work modes:

1. **Homework**: assigned to students/classes with a due date.
2. **Live Exam**: an in-class exam session opened/controlled by the teacher.

The existing "Thư viện đề" concept is clarified as the source test/template library. It represents reusable test material and answer keys, not an assigned homework item and not a live exam session.

---

## Business Reason

Teachers need one reusable source of truth for a test, then decide how to use it in class operations:

- reuse the same source test across multiple classes;
- assign it as homework with a deadline;
- run it as a controlled live exam during class;
- compare results without mixing template identity with delivery mode.

---

## Artifact Impact

| Artifact | Impact | Action |
|----------|--------|--------|
| Product brief | Moderate | Updated to separate source template, HomeworkAssignment, and LiveExamSession. |
| Trigger map | Low/Moderate | Existing teacher workload triggers still apply; semantics should be read through new mode distinction. |
| UX scenarios/specs | Moderate | Updated key teacher, student, and results specs to show Homework vs Live Exam. |
| Design delivery DD-001 | Major | Updated data model, acceptance criteria, user flows, and edge cases. |
| Test scenario TS-001 | Major | Updated happy paths, error states, and must-fix criteria. |
| HTML prototype | Moderate | Updated primary visible labels and mode signals for stakeholder review. |

---

## Revised Domain Model

- **TestTemplate**: reusable source test in Thư viện đề.
- **TestMaterial**: PDF/audio files attached to the template.
- **AnswerKey**: scoring definition attached to the template.
- **HomeworkAssignment**: class/student assignment created from a ready template; includes deadline and time limit.
- **LiveExamSession**: in-class session created from a ready template; includes scheduled/open/closed state.
- **Submission**: student attempt referencing either a HomeworkAssignment or LiveExamSession.

---

## Acceptance Changes

- Teacher can create a reusable template without choosing class or deadline.
- Teacher can mark a template ready after material and answer key are valid.
- Teacher can create Homework from a ready template and set class plus due date.
- Teacher can create/open/close Live Exam from a ready template for in-class use.
- Student list clearly distinguishes Homework from Live Exam.
- Homework blocks new attempts after due date.
- Live Exam blocks access before the teacher opens the session.
- Results and grading preserve class, template, and mode context.

---

## Recommended Next Steps

1. Have BMad Architect convert the revised DD-001 model into technical architecture.
2. Create implementation stories around template creation, homework assignment, live exam session control, student attempt rules, and results filtering.
3. Resolve two open product decisions before implementation:
   - whether homework can be extended/reopened after deadline;
   - whether live exams open manually only, by schedule only, or both.
