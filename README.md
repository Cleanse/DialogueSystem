# Dialogue System

A comprehensive graph-based dialogue system for Unity featuring character portraits, variables, conditional branching, and a powerful visual editor. Perfect for RPGs, visual novels, and story-driven games.

## Features

- **Visual Graph Editor**: Intuitive node-based dialogue creation with real-time editing
- **Character System**: Rich character portraits with expression support
- **Variable System**: Persistent dialogue state with type-safe variables
- **Conditional Logic**: Complex branching with AND/OR operations
- **Function Integration**: Custom game function execution
- **UI Framework**: Complete UI system with typewriter effects and keyboard navigation
- **Audio Support**: Character voice integration (architecture in place)
- **Editor Tools**: Custom property drawers and enhanced inspectors

## Installation

### Option 1: Install from Git URL (Recommended)
1. Open Unity Package Manager (Window → Package Manager)
2. Click the "+" button in the top-left corner
3. Select "Add package from git URL"
4. Enter the git URL for this repository
5. Click "Add"

### Option 2: Manual Installation
1. Copy the `DialogueSystem` folder to your Unity project's `Assets` directory
2. Ensure you have the required dependencies:
   - TextMeshPro (3.0.6+)
   - Unity UI (1.0.0+)

### Import Sample Assets
After installing the package:
1. Open Package Manager (Window → Package Manager)
2. Select "In Project" and find "Dialogue System"
3. Click "Import" next to "Complete Dialogue System" sample
4. Sample assets will be imported to `Assets/Samples/Dialogue System/[version]/Complete Dialogue System/`

## Quick Start

1. **Create a Speaker Database**:
   - Right-click in Project → Create → Dialogue System → Speaker Database
   - This will centrally manage all your characters

2. **Create Characters**:
   - Right-click in Project → Create → Dialogue System → Character
   - Configure character name and default portrait for each character

3. **Add Characters to Speaker Database**:
   - Select your Speaker Database asset
   - Add all your Character assets to the Characters list

4. **Create Dialogue Graphs**:
   - Right-click in Project → Create → Dialogue System → Dialogue Graph Asset
   - Open the graph via Window → App UI Dialogue → Graph Window
   - Add Speech nodes for dialogue lines
   - Add Choice nodes for branching conversations
   - Connect nodes to create dialogue flow

5. **Setup Components with Required Assets**:
   - Add DialogueTrigger to NPCs for world interaction
   - Add DialogueRunner to your scene for dialogue execution
   - Assign your Speaker Database to both components
   - Assign specific dialogue graphs to DialogueTrigger
   - Create/assign DialogueInputSettings for input configuration

6. **Setup UI**:
   - Use the provided UI prefabs from imported samples, or
   - Create custom UI and assign to DialogueRunner
   - Ensure UI components reference the DialogueRunner

## Using Sample Assets

After importing the sample, you'll find:

### Sample Structure
```
Assets/Samples/Dialogue System/[version]/Complete Dialogue System/
├── Art/
│   ├── Characters/          # Character portraits (Ange, Marshal)
│   └── UI/                  # UI sprites and frames
├── Prefabs/
│   ├── DialogueContinueButton.prefab
│   ├── DialogueChoiceButton.prefab
│   └── UI.prefab           # Complete dialogue UI setup
├── Resources/
│   ├── Dialogue/           # Sample characters and dialogue graphs
│   ├── DialogueInputSettings.asset
│   └── Fonts/              # Sample font assets
└── Scenes/
    └── Dialogue.unity      # Demo scene with working dialogue
```

### Quick Setup with Samples
1. **Copy Sample Assets**: Move the imported sample assets to your project's main Assets folder
2. **Use Sample UI**: Drag the `UI.prefab` to your scene
3. **Test Demo Scene**: Open `Dialogue.unity` to see the system in action
4. **Reference Sample Setup**: Use the sample `SpeakerDatabase.asset` and characters as templates

## System Architecture

### Core Components

#### Runtime System (19 files)
- **DialogueRunner**: Core execution engine for dialogue processing
- **DialogueUI**: Main UI controller with typewriter effects and navigation
- **DialogueGraphAsset**: ScriptableObject container for dialogue graphs
- **DialogueNode**: Abstract base class for all dialogue nodes
- **DialogueConnectionNode**: Base class for connected nodes

#### Node Types (5 types)
- **SpeechNode**: Character dialogue delivery
- **ChoiceNode**: Multiple choice branching with dynamic buttons
- **ConditionalNode**: Variable-based logic with AND/OR support
- **FunctionNode**: Custom game function execution
- **VariableSetNode**: Game state manipulation

#### Variable System (2 components)
- **DialogueVariableManager**: Singleton for persistent state management
- **DialogueVariable**: Type-safe variable container with JSON serialization

#### Character System (2 components)
- **Character**: Character configuration with portraits and audio
- **SpeakerDatabase**: Centralized character registry

#### UI & Integration (5 components)
- **DialogueTrigger**: World interaction component
- **DialogueSelectionUI**: Multiple dialogue option selection
- **DialogueInputSettings**: Centralized input configuration
- **DialogueOption**: Data structure for dialogue choices
- **PortraitAttribute**: Enhanced editor property attribute

### Editor Tools (6 components)
- **DialogueGraphWindow**: Main visual editor with split-pane interface
- **DialogueGraph**: GraphView implementation for node editing
- **DialogueGraphAssetEditor**: Custom inspector for dialogue assets
- **CharacterEditor**: Enhanced character asset inspector
- **CharacterPropertyDrawer**: Custom property drawer for characters
- **DialogueTriggerEditor**: Custom editor for dialogue triggers

## Usage

### Creating Dialogue

```csharp
// Create a dialogue graph asset
var dialogueGraph = CreateInstance<DialogueGraphAsset>();

// Access via DialogueRunner
var runner = FindObjectOfType<DialogueRunner>();
runner.StartDialogue(dialogueGraph);
```

### Managing Variables

```csharp
// Set variables
DialogueVariableManager.Instance.SetVariable("playerGold", 100);
DialogueVariableManager.Instance.SetVariable("questComplete", true);

// Get variables
int gold = DialogueVariableManager.Instance.GetVariable<int>("playerGold");
bool isComplete = DialogueVariableManager.Instance.GetVariable<bool>("questComplete");
```

### Character Setup

```csharp
// Create character in editor or via code
var character = CreateInstance<Character>();
character.characterName = "Hero";
character.defaultPortrait = heroSprite;

// Add to speaker database
speakerDatabase.characters.Add(character);
```

## File Structure

```
DialogueSystem/
├── Runtime/
│   ├── NodeTypes/           # All dialogue node implementations
│   ├── VariableSystem/      # Variable management system
│   ├── DialogueRunner.cs    # Core dialogue execution
│   ├── DialogueUI.cs        # Main UI controller
│   ├── Character.cs         # Character system
│   └── ...                  # Other runtime components
├── Editor/
│   ├── DialogueGraphWindow.cs   # Visual graph editor
│   ├── DialogueGraph.cs         # Graph view implementation
│   └── ...                      # Editor tools and property drawers
├── Samples/
│   ├── Prefabs/            # UI prefabs and examples
│   ├── Resources/          # Sample dialogue assets
│   └── Art/               # Character portraits
└── package.json           # Package configuration
```

## Advanced Features

### Custom Node Types

Create custom dialogue nodes by inheriting from `DialogueNode`:

```csharp
[CreateAssetMenu(fileName = "CustomNode", menuName = "Dialogue System/Custom Node")]
public class CustomNode : DialogueNode
{
    public override void Execute(DialogueRunner runner)
    {
        // Custom node logic
        ExecuteConnectedNodes(runner);
    }
    
    public override string GetDisplayText()
    {
        return "Custom Node";
    }
}
```

### Variable Conditions

Use complex conditional logic in ConditionalNode:

```csharp
// AND condition: playerLevel >= 5 AND questComplete == true
// OR condition: hasKey == true OR isAdmin == true
```

### Function Integration

Execute custom game functions via FunctionNode:

```csharp
// Functions can modify game state, give items, etc.
public void GivePlayerGold(int amount)
{
    PlayerInventory.AddGold(amount);
    DialogueVariableManager.Instance.SetVariable("playerGold", 
        PlayerInventory.GetGold());
}
```

## Input System

The system supports dual keybinding through DialogueInputSettings:

- **Continue Dialogue**: Space or Enter
- **Skip Typewriter**: Space or Enter
- **Navigate Choices**: Arrow keys
- **Select Choice**: Enter or Return

## Validation & Debugging

The system provides comprehensive validation:
- Visual warning indicators for unconnected nodes
- Character assignment validation
- Variable reference checking
- Graph flow validation

## Performance

- Efficient node execution with connection caching
- Minimal garbage collection during dialogue playback
- Optimized UI updates with pooled components
- JSON serialization for fast variable persistence

## Compatibility

- **Unity Version**: 2022.3+
- **Render Pipeline**: Universal Render Pipeline (URP) compatible
- **Input System**: Both old and new input systems supported
- **Platform**: Cross-platform compatible

## Contributing

When extending the system:
1. Follow existing code patterns and naming conventions
2. Add comprehensive validation for new node types
3. Include editor tools for new components
4. Update documentation for new features

## License

This package is provided as-is for use in Unity projects. See the project license for full details.

## Support

For questions and support, refer to the project documentation or contact the development team.