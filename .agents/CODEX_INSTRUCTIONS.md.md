# CODEX_INSTRUCTIONS.md

# AI Development Instructions

## Project

Observation Duty Clone

Engine:

Unity 6

Language:

C#

Rendering Pipeline:

URP

Platform:

Windows

---

# Objective

Develop a production-quality horror observation game inspired by **I'm on Observation Duty**.

The project must prioritize:

* Clean Architecture
* Scalability
* Modularity
* Maintainability
* Inspector-friendly workflow

The codebase should be suitable for future commercial expansion.

---

# General Rules

Always prefer:

* Modular code
* Small reusable components
* Composition over inheritance
* Event-driven communication
* Inspector configuration

Never hardcode values unless explicitly requested.

Every configurable gameplay value must be editable through the Unity Inspector.

---

# Coding Standards

Language:

C#

Naming:

Classes → PascalCase

Methods → PascalCase

Properties → PascalCase

Variables → camelCase

Private serialized fields

```csharp
[SerializeField]
private int spawnInterval;
```

Avoid public fields unless the value is intentionally editable by other scripts.

---

# Architecture Rules

Every class must have a single responsibility.

Examples

GameManager

Controls only game flow.

CameraManager

Controls only camera switching.

AnomalyManager

Controls only anomaly spawning and reporting.

UIManager

Controls only UI.

TimeManager

Controls only game time.

AudioManager

Controls only audio.

Never mix responsibilities.

---

# Forbidden

Do NOT:

Use Find() every frame

Use GameObject.Find() inside Update()

Use FindObjectOfType() repeatedly

Create unnecessary Singletons

Duplicate logic

Create circular dependencies

Hardcode room count

Hardcode anomaly count

Store gameplay logic inside UI scripts

Move gameplay logic into buttons

Modify unrelated systems when implementing a feature

---

# Preferred Patterns

Use

Events

Delegates

Interfaces

ScriptableObjects

Composition

Dependency Injection where appropriate

Use Coroutines instead of Update polling whenever possible.

---

# Unity Inspector

Every MonoBehaviour exposed to designers should contain:

Headers

Tooltips

SerializeField

Example

```csharp
[Header("Spawn Settings")]
[SerializeField]
private float spawnCooldown = 20f;
```

Avoid cluttered Inspectors.

---

# Folder Rules

Scripts must be organized.

Assets/

Scripts/

Core/

Gameplay/

Managers/

Camera/

UI/

Audio/

Utilities/

ScriptableObjects/

Avoid placing every script inside one folder.

---

# Script Size

Target

100–300 lines

Maximum

500 lines

If a script grows larger than 500 lines:

Split responsibilities.

---

# Comments

Write meaningful comments.

Explain WHY.

Do not explain obvious syntax.

Good

```csharp
// Prevent spawning anomalies in the currently observed room.
```

Bad

```csharp
// Increase i.
i++;
```

---

# Logging

Use Debug.Log only for

Initialization

Warnings

Errors

Testing

Do not leave excessive debug logs inside production code.

---

# Error Handling

Always validate

null

empty collections

invalid indexes

missing references

Fail gracefully.

Avoid NullReferenceException.

---

# Performance Rules

Avoid unnecessary Update()

Prefer

Coroutine

Events

Invoke

Animation Events

Cache references during Awake() or Start().

Never repeatedly search the hierarchy.

Future target

100+ anomalies

20+ rooms

60 FPS

---

# Camera System

Use

One Main Camera

Multiple CameraPoints

Never create one rendering camera per room.

Camera switching should support

Instant

Smooth interpolation

Future cinematic transition.

---

# Room System

Every room owns

Room ID

Display Name

Camera Point

Environment

Anomaly List

Adding a new room should require:

Creating a room

Creating CameraPoint

Assigning Room ID

No gameplay code modification.

---

# Anomaly System

Every anomaly must contain

Unique ID

Room ID

Anomaly Type

Spawn Weight

Cooldown

Normal Objects

Anomaly Objects

Activation logic

Deactivation logic

Never create hardcoded anomaly behaviour inside AnomalyManager.

AnomalyManager coordinates.

Individual anomalies execute behaviour.

---

# UI Rules

UI only displays data.

UI never decides gameplay.

UI communicates through public APIs.

Example

Good

UI

↓

AnomalyManager.Report()

Bad

UI

↓

Disable anomaly directly

---

# Data Driven Design

Whenever possible

Store gameplay data inside

ScriptableObjects

instead of hardcoding.

Examples

Difficulty

Anomaly Database

Room Database

Settings

Localization

---

# Feature Development Workflow

When implementing a feature:

1.

Understand current architecture.

2.

Reuse existing systems.

3.

Avoid rewriting working code.

4.

Implement only necessary changes.

5.

Keep backward compatibility.

6.

Compile.

7.

Check Console.

8.

Test Play Mode.

9.

Verify Inspector.

10.

Document changes.

---

# Before Creating New Script

Ask:

Can an existing class be extended?

Can this become a reusable component?

Should this be a ScriptableObject?

Should this be an interface?

Avoid unnecessary managers.

---

# Pull Request Checklist

Every completed feature must satisfy:

✔ No compile errors

✔ No Console errors

✔ No missing references

✔ Inspector configurable

✔ Modular

✔ No duplicated code

✔ Uses existing architecture

✔ Works with unlimited rooms

✔ Works with unlimited anomalies

✔ Clean naming

✔ Proper comments

✔ No magic numbers

✔ Easy to extend

---

# Long-Term Vision

The final architecture should support:

Unlimited Rooms

Unlimited Camera Points

Unlimited Anomalies

Difficulty Presets

Steam Achievements

Localization

Save System

Mod Support

Workshop Support

without major architectural changes.

---

# AI Behaviour

When generating code:

Always think about future scalability.

Never implement the quickest solution if it hurts architecture.

Prefer production-quality code over prototype code.

If multiple implementations are possible:

Choose the one that minimizes coupling, maximizes reusability, and follows SOLID principles.

When modifying an existing script:

* Preserve public APIs whenever possible.
* Avoid breaking existing scenes or serialized references.
* Explain any required Unity Inspector changes.
* If a refactor is necessary, keep it incremental rather than rewriting the entire system.

If requirements are ambiguous:

* Make the safest architectural decision.
* Leave clear TODO comments for future enhancements.
* Do not invent gameplay mechanics beyond the documented design.

The objective is to build a maintainable Unity project that can grow from a prototype into a complete commercial-quality game.
