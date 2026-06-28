# PROJECT_REQUIREMENTS.md

# Observation Duty Clone

## Goal

Develop a complete horror observation game inspired by **I'm on Observation Duty** using Unity 6 (URP).

The game must be scalable, modular, production-ready and support adding new rooms and anomalies without modifying existing gameplay code.

---

# Gameplay

The player is a security guard monitoring CCTV cameras.

The player observes multiple rooms through a camera system.

Every room starts in a normal state.

Random anomalies begin appearing over time.

The player must:

* Detect anomaly
* Remember the original room layout
* Open report menu
* Select room
* Select anomaly type
* Submit report

Correct report removes anomaly.

Wrong report does nothing.

---

# Game Objective

Survive from

00:00

until

06:00

without allowing too many anomalies.

---

# Lose Condition

Maximum active anomalies:

4

Configurable in Inspector.

---

# Win Condition

Reach 06:00.

---

# Required Features

## Camera System

Unlimited rooms.

Camera switching.

Current room indicator.

Smooth transition support.

---

## Room System

Every room has

Room ID

Display Name

Camera Point

Environment

Anomaly list

---

## Anomaly System

Must support unlimited anomaly types.

Each anomaly belongs to exactly one room.

Only one instance of each anomaly may exist.

An anomaly has

ID

Room

Type

Cooldown

Spawn Weight

Normal Objects

Anomaly Objects

Activation Method

---

## Spawn Rules

Spawn every configurable interval.

Random weighted selection.

Cannot spawn:

already active

cooldown active

current observed room

Maximum simultaneous anomalies configurable.

---

## Report System

Popup contains

Room Dropdown

Anomaly Type Dropdown

Submit

Cancel

Correct report:

Deactivate anomaly

Increase score

Incorrect report:

No penalty initially

Penalty system may be added later.

---

## Time System

Separate Game Clock from real time.

Configurable time multiplier.

Pause support.

---

## UI

Main HUD

Current Camera

Clock

Active anomaly counter

Report button

Popup

Dropdowns

Status message

---

## Audio

Ambient loops

Room ambience

Footsteps

Ghost sounds

Camera static

TV noise

Future:

Spatial audio

---

## Graphics

URP

Real-time lighting

Post Processing

Bloom

Color Adjustment

Film Grain

Vignette

Future:

Volumetric Fog

---

## Save Data

Future implementation.

Store

Settings

Statistics

Achievements

---

## Performance Goals

Support

100+ anomalies

20+ rooms

without noticeable frame drops.

No Update polling unless necessary.

Avoid Find() every frame.

Use object pooling when appropriate.

---

## Target Platform

Windows

1920x1080

60 FPS minimum.

Keyboard + Mouse.

Future:

Steam Deck

Linux

MacOS
