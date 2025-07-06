# Dual Interaction Keys Implementation

## Overview

The dialogue system now supports **dual keybinds per interaction** with the interaction key(s) also functioning as dialogue continuation keys. This provides a more intuitive user experience where the same key that starts dialogue also advances it.

## Key Features

### ✅ Dual Keybind Support
- Each DialogueTrigger supports **two interaction keys** (Primary + Secondary)
- Default keys: **T** (primary) and **E** (secondary)
- Both keys work for triggering and continuing dialogue

### ✅ Dynamic Key Display
- Interaction prompts automatically show the correct key combinations
- Examples: "Press T to talk", "Press T or E to talk"
- Continue button shows all available advancement keys

### ✅ Centralized Configuration
- **DialogueInputSettings** ScriptableObject for global key configuration
- Consistent key handling across all dialogue components
- Easy customization per project

## Setup Instructions

### 1. DialogueInputSettings Asset (Optional)

Create a global input settings asset for consistent configuration:

1. **Right-click in Project** → **Create** → **Dialogue System** → **Input Settings**
2. **Name it**: `DialogueInputSettings`
3. **Place in**: `Assets/DialogueSystem/Resources/` (for auto-discovery)
4. **Configure keys** as desired

### 2. DialogueTrigger Configuration

Each DialogueTrigger now has **Interaction Keys** section:

- **Primary Interaction Key**: Main trigger key (default: T)
- **Secondary Interaction Key**: Alternative trigger key (default: E)
- **Custom Interaction Prompt Text**: Override auto-generated text (optional)

### 3. DialogueUI Configuration

DialogueUI has new **Input Settings** section:

- **Input Settings**: Reference to DialogueInputSettings asset (optional)
- Auto-discovers settings from Resources if not assigned

## How It Works

### Interaction Flow

1. **Player approaches NPC** → Interaction prompt appears ("Press T or E to talk")
2. **Player presses T or E** → Dialogue starts, keys are passed to DialogueUI
3. **During dialogue** → Player can use T, E, Space, or Enter to continue
4. **Dialogue ends** → Interaction keys are cleared from DialogueUI

### Key Priority System

**For Triggering Dialogue:**
- Primary Interaction Key (T)
- Secondary Interaction Key (E)

**For Continuing Dialogue:**
- Primary Continue Key (Space)
- Secondary Continue Key (Enter)
- Primary Interaction Key (T) - *if enabled*
- Secondary Interaction Key (E) - *if enabled*

## Customization Options

### DialogueInputSettings Properties

```csharp
[Header("Default Interaction Keys")]
public KeyCode primaryInteractionKey = KeyCode.T;
public KeyCode secondaryInteractionKey = KeyCode.E;

[Header("Dialogue Continuation Keys")]
public KeyCode primaryContinueKey = KeyCode.Space;
public KeyCode secondaryContinueKey = KeyCode.Return;

[Header("Input Settings")]
public bool useInteractionKeysForContinuation = true;
public bool showAllKeysInPrompts = true;
```

### Per-Trigger Customization

Each DialogueTrigger can override:
- Primary/Secondary interaction keys
- Custom prompt text
- Input settings reference

## Backwards Compatibility

### ✅ Existing Functionality Preserved
- Space and Enter keys still work for dialogue continuation
- Legacy single-key interaction still works
- All existing DialogueTrigger settings remain functional

### 🔄 Migration Notes
- Old `interactionKey` field replaced with `primaryInteractionKey` and `secondaryInteractionKey`
- Prompt text is now auto-generated but can be customized
- No breaking changes to existing dialogue graphs

## Code Examples

### Basic Usage

```csharp
// Get current input settings
var inputSettings = FindObjectOfType<DialogueInputSettings>();

// Check if any interaction key was pressed
if (inputSettings.IsInteractionKeyPressed(KeyCode.T, KeyCode.E))
{
    TriggerDialogue();
}

// Check if any dialogue advancement key was pressed
if (inputSettings.IsDialogueAdvancementKeyPressed(KeyCode.T, KeyCode.E))
{
    ContinueDialogue();
}
```

### Custom Key Text Generation

```csharp
// Get formatted key text for UI display
string keyText = inputSettings.GetInteractionKeyText(KeyCode.T, KeyCode.E);
// Result: "T or E"

string advancementText = inputSettings.GetAdvancementKeyText(KeyCode.T, KeyCode.E);
// Result: "Space, Enter, T, or E"
```

## File Changes Summary

### New Files
- `DialogueInputSettings.cs` - Centralized input configuration
- `INTERACTION_KEYS_README.md` - This documentation

### Modified Files
- `DialogueUI.cs` - Added interaction key support for continuation
- `DialogueTrigger.cs` - Added dual keybind support and dynamic prompts
- `DialogueSelectionUI.cs` - Added interaction key passing support

### Key Methods Added
- `DialogueUI.SetInteractionKeys()` - Set keys for current dialogue session
- `DialogueInputSettings.IsInteractionKeyPressed()` - Check interaction input
- `DialogueInputSettings.IsDialogueAdvancementKeyPressed()` - Check continuation input
- `DialogueInputSettings.GetInteractionKeyText()` - Format keys for display

## Troubleshooting

### No Input Settings Warning
If you see: *"DialogueUI: No DialogueInputSettings assigned. Using default settings."*

**Solution**: Create a DialogueInputSettings asset in `Assets/DialogueSystem/Resources/`

### Keys Not Working
1. Check DialogueTrigger has correct primary/secondary keys set
2. Verify DialogueUI has InputSettings assigned or auto-discovered
3. Ensure player is within interaction distance
4. Check console for input-related errors

### Custom Prompts Not Showing
1. Verify `customInteractionPromptText` is not empty
2. Check interaction prompt GameObject has TextMeshProUGUI component
3. Ensure SetupInteractionPrompt() is being called in Start()

## Performance Notes

- Input polling only occurs when dialogue is active or player is in range
- Key text generation is cached and only updates when keys change
- No impact on gameplay when dialogue system is inactive

---

**Implementation Status**: ✅ Complete
**Tested With**: Unity 6000.1.8f1, App UI 1.1.1
**Backward Compatible**: Yes
**Breaking Changes**: None