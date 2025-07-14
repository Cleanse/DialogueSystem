# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.3] - 2025-07-14

### Major Improvements
- **Universal Input System Compatibility**: Complete rewrite of input handling to support both legacy Input Manager and new Input System packages
- **Zero Configuration Setup**: Package now works out-of-the-box without requiring any user configuration or asset creation
- **Production-Ready Logging**: Cleaned up debug output for professional deployment while maintaining developer debugging tools

### Added
- Automatic DialogueInputSettings creation at runtime with sensible defaults
- Safe reflection-based Input System detection that doesn't break compilation
- Conditional debug logging system controlled by `enableDebugLogging` flag
- Context menu tools for developers (`Test Input Detection`, `Reset to Defaults`)
- Comprehensive input fallback system (New Input System → Legacy Input Manager)
- Static initialization system for proper ScriptableObject lifecycle management

### Enhanced
- **Input System Architecture**: Completely rebuilt for maximum compatibility and reliability
- **UI Panel Management**: Improved initialization order and state validation
- **Key Consumption Logic**: Fixed timing issues that caused "Key not available" errors
- **Error Handling**: Graceful fallbacks with silent error recovery
- **Package Integration**: Professional Unity Package Manager compliance

### Fixed
- **Critical**: Resolved Input System compilation errors when package is not installed
- **Critical**: Fixed DialogueSelectionUI showing as active on startup
- **Critical**: Eliminated "Key not available" errors in DialogueTrigger
- Input key availability logic now uses OR instead of AND for multiple keys
- UI panel state detection now properly validates both panel and runner state
- ScriptableObject initialization issues resolved with static constructors
- Frame-based input consumption timing corrected

### Performance
- Eliminated unnecessary debug string allocations in hot paths
- Reduced reflection calls with smart caching
- Simplified conditional checks and variable assignments
- Optimized input detection with early returns

### Technical
- Moved from conditional compilation to safe reflection for Input System detection
- Implemented hybrid input approach with multiple fallback layers
- Added automatic runtime asset creation for zero-configuration workflow
- Enhanced UI component validation and error recovery
- Cleaned up compiler warnings and code quality issues

### Developer Experience
- Debug logging is now off by default for clean console output
- Optional verbose logging available via `enableDebugLogging` toggle
- Context menu debugging tools for troubleshooting
- Simplified error messages with actionable information
- Better separation between development and production logging

### Backwards Compatibility
- All existing DialogueInputSettings assets continue to work unchanged
- No breaking changes to public APIs
- Existing project configurations remain functional
- Smooth upgrade path from previous versions

## [1.0.2] - 2025-07-12

### Fixed
- Samples structure updated.

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