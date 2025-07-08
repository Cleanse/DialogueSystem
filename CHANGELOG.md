# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.1] - 2025-07-08

### Added
- DialogueInputSettings component for centralized input configuration
- Enhanced property drawers for improved editor experience
- PortraitAttribute for better character portrait handling in editor
- Comprehensive validation systems for graph integrity
- Improved editor integration with warning indicators

### Improved
- Character assignment workflow with auto-detection
- Graph editor context menus for better workflow
- UI component organization and modularity
- Editor tools with enhanced property drawers
- Documentation updated to reflect current system architecture

### Fixed
- Input handling consistency across UI components
- Character portrait display logic
- Variable persistence edge cases
- Graph validation warnings and error reporting

### Technical
- Refactored input system architecture for better maintainability
- Enhanced editor tooling with custom property drawers
- Improved UI component separation of concerns
- Better error handling and validation throughout system

## [1.0.0] - 2025-07-05

### Added
- Initial release of the Dialogue System package
- Complete graph-based dialogue system with visual editor
- Character portrait system with expression support
- Variable system for persistent dialogue state
- Multiple node types:
  - Speech nodes for character dialogue
  - Choice nodes for branching conversations
  - Conditional nodes for logic-based flow control
  - Function nodes for custom game integration
  - Variable set nodes for state management
- Comprehensive UI system with:
  - Dialogue display with typewriter effects
  - Character portraits and expressions
  - Choice selection interface
  - Keyboard navigation support
- Unity Editor integration:
  - Visual graph editor window
  - Real-time property editing
  - Node validation and error reporting
  - Context menu workflows
- Speaker database for centralized character management
- Dialogue trigger system for world integration
- JSON-based variable persistence
- Complete documentation and setup guide

### Technical Features
- Unity 2022.3+ compatibility
- TextMeshPro integration for enhanced text rendering
- UI Elements framework for editor tools
- ScriptableObject-based asset management
- Event-driven architecture for extensibility
- Comprehensive error handling and validation

### Documentation
- Complete README with setup instructions
- Character portrait and expression guide
- API documentation for extensibility
- Best practices for team workflows
- Troubleshooting guide

### Notes
- This is the initial stable release
- Full backwards compatibility maintained for future updates
- Expression system architecture in place (visual editing coming in future release)