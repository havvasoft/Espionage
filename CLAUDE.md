# ESPIONAGE — PROJECT INSTRUCTIONS

## Project

Espionage is a Unity 3D adventure game developed for Kocaeli University
Software Development Laboratory-I.

The game combines two independent AI systems:

1. An autonomous companion using real NavMesh-based navigation.
2. A local LLM-powered NPC using Ollama.

The game scenario is:

Player finds an employee card
→ opens a secured door
→ disables security cameras through a computer interaction
→ commands the companion to disable a laser barrier
→ passes security patrol robots
→ reaches the mainframe
→ speaks freely with the local LLM NPC
→ NPC produces structured JSON
→ valid decision changes the game state
→ player gains access
→ level completed.

## Technical Requirements

- Unity + C#
- Object-oriented programming
- SOLID principles
- Interfaces and abstraction where appropriate
- Real NavMesh-based autonomous navigation
- Raycast/sensor-based companion perception
- Local Ollama LLM only
- No cloud LLM API
- Structured JSON output from the LLM
- Safe JSON validation/parsing
- Asynchronous LLM communication
- Game must not freeze while waiting for LLM
- Game must not crash when Ollama/model/network fails
- Invalid or unexpected LLM responses must be handled safely
- Systems must remain testable and modular

## Team Responsibilities

Developer A:
- Player
- Gameplay
- Companion AI
- NavMesh
- Physical interactions
- Guard robots

Developer B:
- Ollama integration
- Local LLM
- NPC
- Dialogue
- Structured JSON parsing
- Dialogue UI
- LLM error handling

Shared:
- Core interfaces/contracts
- Main scene integration
- Final testing
- Documentation
- Build and presentation preparation

## Ownership Rule

Do not modify another developer's primary system unless:

1. the user explicitly requests it, or
2. a minimal integration change is genuinely required.

Before modifying another developer's area, explain why the change is necessary.

## Architecture Rules

Keep systems modular and focused.

Prefer:
- Single Responsibility Principle
- interfaces
- abstraction
- composition
- small focused classes

Avoid:
- giant manager classes
- unnecessary dependencies
- duplicate systems
- unnecessary rewrites of working code

Companion AI and LLM NPC must remain separate systems with separate responsibilities.

## Companion AI Rules

The companion must use real NavMesh-based navigation.

Do not simulate autonomous behavior using hardcoded movement sequences.

The companion should have clear states, such as:

- Follow
- ReceiveCommand
- NavigateToTarget
- Interact
- Complete

The companion should use sensor-based perception such as Raycasts where appropriate.

## LLM Rules

The NPC must communicate with a locally running Ollama server.

Do not introduce:
- OpenAI API
- Gemini API
- Claude API
- any other cloud LLM service

Treat every LLM response as untrusted input.

Validate structured output before allowing it to modify game state.

Handle:
- Ollama unavailable
- model unavailable
- network errors
- timeout
- empty response
- malformed JSON
- unexpected values

The game must continue running safely after LLM failures.

## Unity Rules

Prefer reusable Prefabs and components.

Avoid unnecessary edits to shared scenes.

Treat .unity scene files as integration-sensitive.

Inspect existing implementation before changing it.

Do not replace working systems without a clear reason.

## Git Rules

Never use destructive Git commands.

Do not:
- force push
- reset shared branches
- delete branches
- revert another developer's work

without explicit instruction.

Use meaningful commit messages.

Keep commits logically grouped.

Do not commit generated Unity folders such as:
- Library
- Temp
- Obj
- Logs
- UserSettings
- Build

Do not commit personal Claude instructions such as CLAUDE.local.md.

## Claude Workflow

Before implementing a significant task:

1. Inspect the current repository state.
2. Read relevant existing files.
3. Identify the files that should change.
4. Briefly explain the proposed implementation.
5. Implement only the requested scope.
6. Verify the result.
7. Summarize changed files and remaining issues.

Do not silently expand the scope of a task.

Do not assume that work by another developer is complete.
Inspect the repository instead.

## Testing

Companion tests should include different starting positions and conditions.

LLM tests should include:
- persuasive input
- angry input
- irrelevant input
- malformed response
- unavailable Ollama
- timeout

## Development Priority

Priority order:

1. Working gameplay
2. Real AI functionality
3. Robust error handling
4. Integration
5. Testing
6. Visual polish
7. Documentation polish

Do not sacrifice required functionality for visual polish.