# Dialogue System - Character Portrait & Expression Guide

This guide will walk you through creating characters with portrait expressions for the dialogue system.

## Table of Contents
- [Overview](#overview)
- [Creating a Character](#creating-a-character)
- [Adding Expressions](#adding-expressions)
- [Setting Up a Speaker Database](#setting-up-a-speaker-database)
- [Using Characters in Dialogue Nodes](#using-characters-in-dialogue-nodes)
- [UI Setup](#ui-setup)
- [Testing Your Setup](#testing-your-setup)

## Overview

The character portrait system allows you to:
- Create character assets with multiple facial expressions
- Centrally manage all characters in a Speaker Database
- Display character portraits in dialogue UI
- Switch expressions based on dialogue context
- Maintain backward compatibility with existing dialogue

## Creating a Character

### Step 1: Create the Character Asset

1. **Right-click** in your Project window
2. Navigate to **Create > Dialogue System > Character**
3. Name your character (e.g., "Hero", "Merchant", "Villain")

### Step 2: Configure Basic Character Info

Select your new Character asset and configure these fields in the Inspector:

```
Character Info:
├── Character Name: "Hero"
├── Character Description: "The main protagonist of our story"
└── Default Portrait: [Drag your default portrait sprite here]
```

**Important:** The Character Name field will be used as the display name in dialogue UI.

### Step 3: Set Up Audio (Optional - Not Yet Implemented)

```
Audio Settings:
├── Voice Pitch: 1.0 (range: 0.5 to 2.0)
└── Voice Clip: [Optional audio clip for this character]
```

## Adding Expressions (Not Yet Implemented)

### Adding Happy Expression

1. In the **Expressions** section, click the **+** button to add a new expression
2. Configure the new expression:
   ```
   Expression 0:
   ├── Expression Name: "Happy"
   ├── Portrait: [Drag your happy portrait sprite here]
   └── Description: "Character's joyful expression"
   ```

### Expression Best Practices

- **Consistent Naming**: Use standard names like "Happy", "Sad", "Angry", "Surprised", "Neutral"
- **Portrait Dimensions**: Keep all portraits for a character the same size for consistent UI
- **File Organization**: Store character portraits in `Assets/_Project/Art/Characters/[CharacterName]/`

### Example Expression Setup

```
Hero Character Asset:
├── Default Portrait: hero_neutral.png
├── Expressions:
│   ├── Happy: hero_happy.png
│   ├── Angry: hero_angry.png
│   ├── Sad: hero_sad.png
│   └── Surprised: hero_surprised.png
```

## Setting Up a Speaker Database

### Step 1: Create the Database

1. **Right-click** in your Project window
2. Navigate to **Create > Dialogue System > Speaker Database**
3. Name it "Speaker Database" (or "Main Speaker Database")

### Step 2: Add Characters to Database

1. Select your Speaker Database asset
2. In the **Characters** list, set the size to match your character count
3. Drag your Character assets into the list slots:
   ```
   Characters:
   ├── Element 0: Hero (Character)
   ├── Element 1: Merchant (Character)
   └── Element 2: Villain (Character)
   ```

### Step 3: Set Fallback Character (Optional)

1. In the **Settings** section, assign a **Fallback Character**
2. This character will be used when a requested character isn't found
   ```
   Settings:
   └── Fallback Character: Hero (Character)
   ```

## Using Characters in Dialogue Nodes

### Future-Proof Workflow (Recommended)

**For new dialogues, follow this workflow to avoid needing migration:**

1. **Create Characters First**: Set up your Character assets and Speaker Database before building dialogue
2. **Use Character-Specific Creation**: Right-click in Graph Editor and choose:
   - `Create Speech Node with Character/[CharacterName]`
   - `Create Choice Node with Character/[CharacterName]`
3. **Auto-Assignment**: When using standard creation, the system will automatically assign characters if names match your Speaker Database

### Enhanced Graph Editor Features

**Character-Aware Context Menus**:
- **Character Submenus**: Create nodes pre-assigned to specific characters
- **Quick Assignment**: Right-click selected nodes for character assignment options
- **Auto-Assignment**: Standard node creation now checks Speaker Database for matching names
- **Visual Validation**: Warning icons (⚠) appear on nodes without character assignments

### Working with Existing Nodes

**Method 1: Context Menu Assignment**
1. Right-click any Speech or Choice node
2. Choose `Assign Character/[CharacterName]`
3. Character and expression are automatically set

**Method 2: Inspector Assignment**
1. Select a Speech or Choice Node
2. In the Inspector **Speaker Settings**:
   ```
   Speaker Settings:
   ├── Speaker Name: "Hero" (fallback text)
   ├── Speaker Character: [Drag Hero character here]
   └── Expression: "Happy" (dropdown appears after character is assigned)
   ```

### Expression Dropdown (Not Yet Implemented)

Once you assign a Character to the Speaker Character field:
- The **Expression** field becomes a dropdown
- It shows all available expressions for that character
- Includes "Default" option to use the character's default portrait

### Validation and Warnings

The system provides visual feedback:
- **Warning Icon (⚠)**: Appears on nodes without character assignments
- **Tooltips**: Hover over warning icons for helpful suggestions
- **Auto-Detection**: System scans for Speaker Databases automatically

## UI Setup
*I created the UIs already.*

### Adding Portrait Display to DialogueUI

1. Open your **DialogueCanvas** prefab
2. Add an **Image** component for the character portrait
3. Optionally, create a **Portrait Container** GameObject to group portrait elements
4. In your **DialogueUI** script component, assign these new UI references:
   ```
   Character Portrait:
   ├── Portrait Container: [Your portrait container GameObject]
   ├── Speaker Portrait: [Your portrait Image component]
   └── Default Portrait Sprite: [Optional fallback sprite]
   ```

### Adding Portrait Display to DialogueSelectionUI

1. Open your **DialogueSelectionCanvas** prefab
2. Add an **Image** component for the NPC portrait
3. Optionally, create a **Portrait Container** GameObject to group portrait elements
4. In your **DialogueSelectionUI** script component, assign these new UI references:
   ```
   Character Portrait:
   ├── Portrait Container: [Your NPC portrait container GameObject]
   ├── NPC Portrait: [Your NPC portrait Image component]
   └── Default Portrait Sprite: [Optional fallback sprite]
   ```

### Setting Up NPC Characters for DialogueTrigger

1. Create Character assets for your NPCs
2. Add them to your Speaker Database
3. In your **DialogueTrigger** component, set:
   ```
   NPC Settings:
   ├── NPC Name: "Merchant" (fallback text)
   ├── NPC Character: [Drag Merchant character here]
   └── NPC Expression: "Default" (or specific expression)
   ```

**Auto-Assignment**: If you don't assign an NPC Character, the system will automatically try to find a matching character in your Speaker Database based on the NPC Name.

### Portrait Container Setup Examples

**Main Dialogue UI:**
```
DialogueCanvas
├── DialoguePanel
│   ├── PortraitContainer
│   │   └── SpeakerPortrait (Image)
│   ├── SpeakerName (TextMeshPro)
│   ├── DialogueText (TextMeshPro)
│   └── ContinueButton
└── ChoicePanel
    └── ...
```

**Selection UI:**
```
DialogueSelectionCanvas
├── SelectionPanel
│   ├── NPCPortraitContainer
│   │   └── NPCPortrait (Image)
│   ├── NPCName (TextMeshPro)
│   ├── InstructionText (TextMeshPro)
│   ├── OptionsContainer
│   │   └── [Option Buttons]
│   └── CancelButton
```

### Automatic Character Assignment

The system now automatically assigns characters when:

1. **Opening existing dialogues**: Nodes with matching speaker names get characters auto-assigned
2. **Creating new nodes**: Standard creation methods check the Speaker Database for matches
3. **Using DialogueTrigger**: NPCs automatically get character assignments based on name matching

### Manual Character Assignment

For nodes that need manual attention (shown with ⚠ warning icons):

**Method 1: Context Menu (Recommended)**
1. Right-click any Speech or Choice node in the Graph Editor
2. Choose `Assign Character/[CharacterName]` from the context menu
3. Character and expression are automatically set

**Method 2: Inspector Assignment**
1. Select a Speech or Choice node
2. In the Inspector, assign a Character to the **Speaker Character** field
3. Choose an expression from the **Expression** dropdown

### Bulk Character Assignment

For multiple nodes:
1. Ensure your characters are in the Speaker Database
2. Let auto-assignment handle matching names automatically
3. Use context menu "Assign Character" for remaining nodes
4. Warning icons (⚠) will disappear as characters are assigned

## Testing Your Setup

### Manual Testing

1. Create a simple dialogue in the Graph Window
2. Assign characters and expressions to speech nodes
3. Test the dialogue in play mode
4. Verify portraits display correctly for each expression

### Debugging Tips

- **No Portrait Showing**: Check if Portrait Container and Speaker Portrait are assigned in DialogueUI
- **Wrong Expression**: Verify the expression name matches exactly (case-sensitive)
- **Character Not Found**: Ensure the character is added to the Speaker Database
- **Default Portrait**: Will show if no character is assigned or expression is missing
- **"0 have character assignments"**: This is normal for existing dialogue - auto-assignment and context menus will help

### Common Test Results and Solutions

**"Found 2 speech nodes, 0 have character assignments"**
- This means you have existing dialogue nodes that need character assignments
- Solution: Auto-assignment will handle nodes with matching names, use context menus for others

**"No characters in database"**
- Your Speaker Database is empty
- Solution: Create Character assets and add them to your Speaker Database

**"Character 'Hero' default portrait: Not set"**
- The character exists but has no default portrait assigned
- Solution: Assign a sprite to the Default Portrait field in your Character asset

## Advanced Usage

### Multiple Speaker Databases

You can create different Speaker Databases for different scenes or chapters:
- `MainCharacters_Database` - Core story characters
- `TownNPCs_Database` - Town-specific characters
- `Chapter1_Database` - Chapter-specific characters

### Character Inheritance

For character variants, create base characters and variant characters:
- `Hero_Base` - Basic hero with common expressions
- `Hero_Armored` - Hero in armor with same expressions
- `Hero_Injured` - Hero with injury-specific expressions

## File Structure Recommendation

```
Assets/_Project/
├── Resources/
│   └── Dialogue/
│       ├── Characters/
│       │   ├── Hero.asset
│       │   ├── Merchant.asset
│       │   └── Villain.asset
│       ├── Databases/
│       │   └── SpeakerDatabase.asset
│       └── Graphs/
│           └── Chapter1_Dialogue.asset
└── Art/
    └── Characters/
        ├── Hero/
        │   ├── hero_neutral.png
        │   ├── hero_happy.png
        │   ├── hero_angry.png
        │   └── hero_sad.png
        └── Merchant/
            ├── merchant_neutral.png
            ├── merchant_happy.png
            └── merchant_greedy.png
```

This structure keeps your dialogue assets organized and your art assets properly sorted by character.

---

## Workflow Benefits

### Best Practices for Teams

1. **Set Up Characters First**: Before any dialogue creation
2. **Use Character Submenus**: Preferred method for new nodes
3. **Maintain Speaker Database**: Keep it updated with all characters
4. **Follow Validation**: Address warning icons promptly
5. **Consistent Naming**: Use standard expression names across characters

## Quick Reference

### Future-Proof Workflow Checklist
- [ ] Create Character assets (with expressions-NYI)
- [ ] Set up Speaker Database with all characters
- [ ] Use "Create [Node] with Character" context menus
- [ ] Address any warning icons immediately
- [ ] Test portrait display in UI

### Character Creation Checklist
- [ ] Create Character asset
- [ ] Set Character Name and Default Portrait
- [ ] Add character to Speaker Database
- [ ] Test in dialogue node using character-specific creation
- [ ] Verify UI displays correctly

### Graph Editor Context Menu Options
- `Create Speech Node with Character/[Name]` - Pre-assigned character
- `Create Choice Node with Character/[Name]` - Pre-assigned character
- `Assign Character/[Name]` - Add character to selected node
- `Remove Character Assignment` - Clear character from selected node

### Common Expression Names (NYI)
- Default, Neutral, Happy, Sad, Angry, Surprised, Confused, Excited, Worried, Smug, Tired, Determined

### Troubleshooting
1. **Portrait not showing**: Check DialogueUI component assignments
2. **Expression dropdown empty**: Verify Character asset is assigned first
3. **Wrong character speaking**: Check Speaker Database character assignments
4. **UI layout broken**: Ensure Portrait Container layout is configured properly
5. **Warning icons everywhere**: Auto-assignment will handle matching names, use context menus for remaining nodes
6. **No character submenus**: Verify Speaker Database is set up and has characters