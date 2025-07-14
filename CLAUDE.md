# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity 6000.1.8f1 project featuring a comprehensive graph-based dialogue system for "Lunar Tides: A Dance of Gravity". The system uses Unity's App UI package and UI Elements framework for both runtime UI and custom editor tools.

## Development Commands

### Unity Editor
- Open Unity 6000.1.8f1 to work with the project
- Access the dialogue graph editor via `Window/App UI Dialogue/Graph Window`
- Test dialogue scenes in `Assets/_Project/Scenes/Dialogue.unity`

### No Build Scripts
This project doesn't have custom build scripts or automated testing. Use Unity's standard build process through the editor.

## Core Architecture

### Dialogue System Structure
The dialogue system is built around a graph-based node architecture:

- **DialogueGraphAsset**: ScriptableObject container for dialogue graphs
- **DialogueRunner**: Core execution engine that processes dialogue flow
- **DialogueUI**: Handles speech display, choices, and user interaction
- **DialogueVariableManager**: Singleton for persistent dialogue state

### Node Types System
All nodes inherit from `DialogueNode` base class:
- **SpeechNode**: Basic dialogue delivery with speaker/text
- **ChoiceNode**: Multiple choice branching with dynamic button generation
- **FunctionNode**: Custom game function execution (gold, items, etc.)
- **ConditionalNode**: Variable-based logic branching with AND/OR support
- **VariableSetNode**: Game state manipulation

### Variable System
Persistent dialogue state management:
- Type-safe variables (bool, int, float, string)
- JSON-based persistence in `DialogueVariableManager`
- Event-driven variable change notifications
- Debug visualization in inspector

### Custom Editor Tools
Split-pane editor window (`DialogueGraphWindow`) with:
- Graph view using UI Elements framework
- Real-time property editing with inspector integration
- Node creation via right-click context menus
- Graph validation and error reporting

## Key File Locations

- **Main dialogue system**: `Assets/DialogueSystem/` (Package-based system)
  - Core runtime: `Assets/DialogueSystem/Runtime/`
  - Node types: `Assets/DialogueSystem/Runtime/NodeTypes/`
  - Variable system: `Assets/DialogueSystem/Runtime/VariableSystem/`
  - Editor tools: `Assets/DialogueSystem/Editor/`
  - Samples: `Assets/DialogueSystem/Samples/`
- **Dialogue assets**: `Assets/_Project/Resources/Dialogue/`
- **UI prefabs**: `Assets/_Project/Prefabs/UI/`

## Unity Package Dependencies

- App UI (1.1.1) - Required for editor tools and UI framework
- Input System (1.14.0) - OPTIONAL: Automatically detected and used if available
- URP (17.1.0) - Universal Render Pipeline
- TextMeshPro - Text rendering for dialogue UI

## Input System Architecture (v1.0.3+)

The dialogue system features **universal input compatibility** that automatically works with both Unity input systems:

### Automatic Detection
- **DialogueInputSettings**: Uses safe reflection to detect available input systems
- **Graceful Fallback**: New Input System → Legacy Input Manager
- **Zero Configuration**: Creates runtime settings automatically with sensible defaults
- **No Compilation Errors**: Safe reflection ensures compatibility regardless of installed packages

### Key Components
- **DialogueInputSettings.cs**: Central input management with hybrid detection
- **Runtime Asset Creation**: `GetOrCreateDefaultSettings()` eliminates user setup requirements
- **Input Consumption System**: Frame-based key consumption prevents conflicts
- **Debug Tools**: Context menu testing and conditional logging

### Default Key Bindings
- **Interaction**: T (primary), E (secondary)
- **Continuation**: Space (primary), Enter (secondary)  
- **Cancel**: Escape

### Developer Tools
- Right-click DialogueInputSettings → "Test Input Detection" for troubleshooting
- Toggle `enableDebugLogging` for verbose input logging
- Context menu "Reset to Defaults" for quick configuration

## Development Workflow

### Creating New Node Types
1. Inherit from `DialogueNode` or `DialogueConnectionNode` in `Assets/DialogueSystem/Runtime/`
2. Implement required abstract methods (`Execute`, `GetDisplayText`)
3. Add corresponding editor class in `Assets/DialogueSystem/Editor/` folder
4. Register in `DialogueGraph.cs` context menu

### Adding New Variables
Use `DialogueVariableManager.Instance.SetVariable()` and `GetVariable()` methods. Variables are automatically persisted and can be referenced by name in conditional nodes.

### Character Management
- Characters are managed through `SpeakerDatabase.cs` with centralized character registry
- Each character supports multiple portraits and audio settings
- Use `Character.cs` for individual character configuration

### UI Customization
UI components use Unity's UI Elements. The main UI controller is `DialogueUI.cs` which handles:
- Typewriter text effects with skip functionality
- Dynamic choice button generation
- Audio integration for dialogue events
- Keyboard navigation (Space/Enter to continue)

## Testing and Debugging

- Use `Assets/_Project/Scenes/Dialogue.unity` for testing dialogue flows
- DialogueVariableManager provides inspector debugging for variable states
- Graph validation runs automatically in the editor
- Console logs provide execution flow information

## Before creating new code:
- [ ] Searched for similar functionality
- [ ] Looked for similar UI components
- [ ] Verified no existing hook handles this state
- [ ] Confirmed can't extend existing methods
- [ ] Considered if feature flag in existing component works

### R - Recursive Decomposition (Least-to-Most)

Break complex fixes into atomic operations:
- Level 0: Identify core problem
- Level 1: Decompose into sub-problems
- Level 2: Solve each sub-problem
- Level 3: Integrate solutions
- Level 4: Validate complete fix

### I - Intelligent Mutation (SAMMO-Inspired for Unity C#)

Apply mutation operators to generate optimal Unity C# implementations:
- PARAPHRASE: Rewrite logic using alternative, idiomatic C# structures (e.g., LINQ, extension methods, delegates)
- INDUCE: Analyze working code to identify reusable design patterns (e.g., Singleton, Strategy, Observer)
- COMBINE: Merge effective implementation techniques from multiple scripts (e.g., blend coroutine timing with state machine behavior)
- ABSTRACT: Extract and generalize code into reusable MonoBehaviours, ScriptableObjects, or interfaces with proper inheritance and decoupling
