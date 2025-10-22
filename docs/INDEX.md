# Documentation Index - Jumper 2D Platformer

**Last Updated:** 2025-10-21
**Documentation Health:** 9/10 ✅

This index provides a comprehensive map of all project documentation. Start here to find what you need.

---

## 📖 Quick Start

**New to the project?** Read these in order:
1. [DEVELOPMENT_PHILOSOPHY.md](DEVELOPMENT_PHILOSOPHY.md) - Core principles and strategic approach
2. [ARCHITECTURE_GUIDELINES.md](ARCHITECTURE_GUIDELINES.md) - Component architecture decisions
3. [How_To_Run_Tests.md](How_To_Run_Tests.md) - Running the test suite

---

## 🎯 Core Documentation (Active & Current)

### Development Philosophy & Strategy
- **[DEVELOPMENT_PHILOSOPHY.md](DEVELOPMENT_PHILOSOPHY.md)** (669 lines)
  - Ship first, perfect later philosophy
  - 30-70 testing strategy (30% unit, 70% PlayMode)
  - Refactoring policy and decision framework
  - Time allocation guidelines
  - **Status:** ACTIVE - Primary strategic reference

### Architecture & Design
- **[ARCHITECTURE_GUIDELINES.md](ARCHITECTURE_GUIDELINES.md)** (950 lines)
  - Definitive 3-4 component player system design
  - Migration from 11-component monolithic architecture
  - Real-world examples (Celeste, Hollow Knight)
  - Decision trees for component creation
  - **Status:** ACTIVE - Definitive architectural decision record

### Testing Documentation
- **[Testing_Guide.md](Testing_Guide.md)** (~370 lines)
  - Comprehensive test infrastructure overview
  - ~98 tests across 6 test files
  - EditMode and PlayMode test strategies
  - Test writing templates and best practices
  - **Status:** ACTIVE - Recently updated (Oct 21, 2025)

- **[How_To_Run_Tests.md](How_To_Run_Tests.md)** (~200 lines)
  - Step-by-step Unity Test Runner guide
  - Troubleshooting common test issues
  - Quick reference for test execution
  - **Status:** ACTIVE - Most comprehensive test running guide

### Feature Implementation Guides
- **[MOVING_PLATFORMS_GUIDE.md](MOVING_PLATFORMS_GUIDE.md)** (~250 lines)
  - Complete moving platform implementation
  - Setup instructions and prefab creation
  - Physics configuration and best practices
  - **Status:** ACTIVE - Recently implemented feature (Oct 21, 2025)

---

## 🗄️ Root-Level Documentation

### Configuration & Workflow
- **[CLAUDE.md](../CLAUDE.md)** (416 lines)
  - Claude Code / SPARC workflow configuration
  - Agent coordination protocols
  - Development commands and tools
  - MCP integration settings
  - **Status:** ACTIVE - System configuration

### Development Log
- **[DevLog.md](../DevLog.md)** (340 lines)
  - Development journal with architectural evolution
  - Major refactoring documentation
  - Bug fix history and solutions
  - Current blockers and debugging notes
  - **Status:** ACTIVE - Ongoing development record

---

## 📁 Archive Documentation

Historical documentation has been organized in `outdated_docs/` with the following structure:

### [outdated_docs/migrations/](outdated_docs/migrations/)
Completed migration guides (5 files):
- Parameter Migration docs (4 files) - Phase 2 completed
- EnemySystemMigrationGuide.md - Migration from complex to simple enemy system

### [outdated_docs/experimental/](outdated_docs/experimental/)
Exploratory/abandoned features (4 files):
- Breakable terrain implementation experiments
- Composite collider guides
- **Status:** Exploratory work, not in production

### [outdated_docs/completed/](outdated_docs/completed/)
Completed feature setup docs (3 files):
- Moving platform setup and fix guides
- Implementation process documentation
- **Status:** Setup completed, archived for reference

### [outdated_docs/testing_history/](outdated_docs/testing_history/)
Historical test documentation (4 files):
- Test suite expansion planning and analysis
- Test failure fix summaries
- Project analysis for unit testing
- **Status:** Historical record of testing evolution

### [outdated_docs/](outdated_docs/)
General outdated docs (1 file):
- feature_log.md - Legacy feature tracking (superseded by DevLog.md)

**See [outdated_docs/README.md](outdated_docs/README.md) for detailed archive information.**

---

## 📊 Documentation Statistics

### Active Documentation
- **Core Guides:** 5 files (~2,300 lines)
- **Root Config:** 2 files (~750 lines)
- **Total Active:** 7 files (~3,050 lines)

### Archived Documentation
- **Historical Guides:** 17 files
- **Organized by:** Category (migrations, experimental, completed, testing_history)

### Removed Documentation
- **Deleted:** 2 outdated duplicate test guides
- **Reason:** Superseded by comprehensive How_To_Run_Tests.md

---

## 🔍 Finding Documentation by Topic

### Player Systems
- **Architecture:** ARCHITECTURE_GUIDELINES.md
- **Philosophy:** DEVELOPMENT_PHILOSOPHY.md
- **Testing:** Testing_Guide.md

### Features & Mechanics
- **Moving Platforms:** MOVING_PLATFORMS_GUIDE.md
- **Enemy System (historical):** outdated_docs/migrations/EnemySystemMigrationGuide.md
- **Breakable Terrain (experimental):** outdated_docs/experimental/

### Testing & Quality
- **Running Tests:** How_To_Run_Tests.md
- **Writing Tests:** Testing_Guide.md
- **Test Results:** ../Assets/Tests/TEST_SUMMARY.md

### Development Workflow
- **SPARC/Claude Config:** ../CLAUDE.md
- **Development Journal:** ../DevLog.md
- **Philosophy:** DEVELOPMENT_PHILOSOPHY.md

### Historical Reference
- **Migrations:** outdated_docs/migrations/
- **Test Evolution:** outdated_docs/testing_history/
- **Experimental Features:** outdated_docs/experimental/

---

## 📝 Documentation Maintenance

### Update Frequency
- **Testing_Guide.md:** Update after major test suite changes
- **DevLog.md:** Update after significant changes or bug fixes
- **ARCHITECTURE_GUIDELINES.md:** Update when architectural decisions change
- **DEVELOPMENT_PHILOSOPHY.md:** Review quarterly

### Quality Checklist
- ✅ No duplicate documentation
- ✅ Clear status indicators (ACTIVE/ARCHIVED)
- ✅ Outdated docs properly archived
- ✅ Current test counts accurate
- ✅ Cross-references valid

### Next Review
- **Date:** After next major feature implementation
- **Focus:** Ensure new features are documented
- **Archive:** Move completed feature docs to outdated_docs/

---

## 🚀 Recommended Reading Order

### For New Developers
1. DEVELOPMENT_PHILOSOPHY.md - Understand the "why"
2. ARCHITECTURE_GUIDELINES.md - Learn the architecture
3. How_To_Run_Tests.md - Set up your workflow
4. ../DevLog.md - Current project state

### For Feature Development
1. ARCHITECTURE_GUIDELINES.md - Component design patterns
2. MOVING_PLATFORMS_GUIDE.md - Example implementation
3. Testing_Guide.md - Test-driven approach
4. DEVELOPMENT_PHILOSOPHY.md - Decision framework

### For Debugging
1. ../DevLog.md - Known issues and solutions
2. Testing_Guide.md - Test infrastructure
3. outdated_docs/testing_history/ - Historical bug patterns

### For Refactoring
1. DEVELOPMENT_PHILOSOPHY.md - Refactoring policy
2. ARCHITECTURE_GUIDELINES.md - Target architecture
3. outdated_docs/migrations/ - Previous refactoring examples

---

**Questions or suggestions?** Update this index when adding new documentation.
