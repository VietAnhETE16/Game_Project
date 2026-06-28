# ARCHITECTURE.md

# High-Level Architecture

```
GameManager
│
├── CameraManager
│      │
│      └── RoomCameraPoint
│
├── AnomalyManager
│      │
│      ├── Anomaly
│      ├── SpawnSystem
│      └── ReportSystem
│
├── TimeManager
│
├── UIManager
│      │
│      ├── HUD
│      ├── ReportPanel
│      ├── PauseMenu
│      └── GameOverPanel
│
├── AudioManager
│
└── SaveManager
```

---

# Managers

## GameManager

Controls

Game State

Pause

Win

Lose

Restart

Scene Flow

---

## CameraManager

Responsibilities

Discover camera points

Switch cameras

Maintain current room

Notify UI

Future

Smooth interpolation

Camera effects

Static interference

---

## RoomCameraPoint

Stores

Room ID

Display Name

Order

Camera Transform

---

## TimeManager

Owns

Current game hour

Current game minute

Time multiplier

Pause state

Events

OnMinuteChanged

OnHourChanged

OnNightFinished

---

## AnomalyManager

Owns

Spawn Scheduler

Active anomalies

Cooldown

Spawn Rules

Validation

Game Over checking

Future

Difficulty scaling

Spawn curves

Weighted probability

---

## Anomaly

Owns

Room

Type

Objects

Activation

Deactivation

Animation

Future

Sound

Particle

AI

---

## UIManager

Owns

HUD

Menus

Popups

Notifications

Receives events only.

Must never contain gameplay logic.

---

## AudioManager

Owns

Music

Ambient

One-shot SFX

Room ambience

Future

Dynamic horror music

---

## SaveManager

Future implementation.

Stores

Settings

Statistics

Achievements

Unlocked nights

---

# Dependency Rules

Allowed

GameManager

↓

Managers

↓

Gameplay Components

↓

UI

Not allowed

UI → Gameplay logic

Anomaly → CameraManager

Camera → UI

Use events whenever possible.

---

# Coding Rules

Single Responsibility Principle

No circular dependency

No Singleton abuse

Inspector configurable

No hardcoded room count

No hardcoded anomaly count

Use enums for anomaly types

Use ScriptableObjects for databases

---

# Future Architecture

Database

ScriptableObjects

↓

Runtime Manager

↓

Runtime Objects

↓

UI

Allows adding new anomalies without changing code.
