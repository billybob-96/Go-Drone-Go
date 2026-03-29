# Go Drone Go

Go Drone Go is a parcel delivery game where the player remotely operates a delivery drone to deliver parcels and packages of varying shapes, sizes, weights and requirements in the shortest time possible. The player can upgrade and improve the drone and parcel options as they progress.

## Project Overview

This is a Unity 6 project using the Universal Render Pipeline, the new Input System, and Cinemachine-related camera tooling. The current playable setup appears focused on drone movement, camera follow behaviour, and a sample scene for testing core flight controls.

## Requirements

- Unity Editor `6000.4.0f1`
- Unity Hub
- Git

## Opening The Project

1. Open Unity Hub.
2. Add the project folder if needed:
   `E:\Program Files (x86)\Unity Projects\Projects\Go Drone Go\GDG V1`
3. Open the project with Unity Editor `6000.4.0f1`.
4. Wait for package import and script compilation to finish.

## Packages In Use

The project currently includes these notable Unity packages:

- Universal Render Pipeline
- Input System
- Cinemachine
- AI Navigation
- Timeline
- Unity Test Framework
- Visual Scripting

Package definitions live in [Packages/manifest.json](/E:/Program Files (x86)/Unity Projects/Projects/Go Drone Go/GDG V1/Packages/manifest.json).

## Running The Project

- Open the main build scene: [Assets/Scenes/SampleScene.unity](/E:/Program Files (x86)/Unity Projects/Projects/Go Drone Go/GDG V1/Assets/Scenes/SampleScene.unity)
- Press Play in the Unity Editor

At the moment, this scene is the only one enabled in build settings.

## Current Controls

Based on the current drone input actions:

- `W` / `A` / `S` / `D`: move the drone
- `Space`: ascend
- `C`: descend
- Mouse left/right movement: yaw
- `F`: interact

There is also an older or broader input asset in [Assets/InputSystem_Actions.inputactions](/E:/Program Files (x86)/Unity Projects/Projects/Go Drone Go/GDG V1/Assets/InputSystem_Actions.inputactions), but the main drone setup appears to rely on [Assets/PlayerControls.inputactions](/E:/Program Files (x86)/Unity Projects/Projects/Go Drone Go/GDG V1/Assets/PlayerControls.inputactions).

## Project Structure

- [Assets/Scenes](/E:/Program Files (x86)/Unity Projects/Projects/Go Drone Go/GDG V1/Assets/Scenes): playable and test scenes
- [Assets/Scripts/Player](/E:/Program Files (x86)/Unity Projects/Projects/Go Drone Go/GDG V1/Assets/Scripts/Player): drone movement and camera scripts
- [Assets/Prefabs](/E:/Program Files (x86)/Unity Projects/Projects/Go Drone Go/GDG V1/Assets/Prefabs): drone prefabs and related art assets
- [Packages](/E:/Program Files (x86)/Unity Projects/Projects/Go Drone Go/GDG V1/Packages): Unity package definitions
- [ProjectSettings](/E:/Program Files (x86)/Unity Projects/Projects/Go Drone Go/GDG V1/ProjectSettings): project-wide Unity settings

## Source Control Notes

- The repository uses git and tracks the `main` branch.
- Unity-generated folders like `Library`, `Temp`, `Logs`, and `UserSettings` are ignored in [`.gitignore`](/E:/Program Files (x86)/Unity Projects/Projects/Go Drone Go/GDG V1/.gitignore).
- GitHub warned that [Assets/Prefabs/PBR Racing Drone/Models/Racing Drone v2.FBX](/E:/Program Files (x86)/Unity Projects/Projects/Go Drone Go/GDG V1/Assets/Prefabs/PBR Racing Drone/Models/Racing Drone v2.FBX) is large enough that Git LFS may be worth considering later.

## Known Notes

- The project opens correctly in Unity `6000.4.0f1`.
- The sample scene is currently the main entry point for testing.
- This `README` is a first-pass setup guide and can be expanded as gameplay systems, delivery logic, upgrade systems, and UI become more defined.
