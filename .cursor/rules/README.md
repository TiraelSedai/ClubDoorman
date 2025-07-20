# Cursor IDE Rules for ClubDoorman Project

## Core Rules

### Professional Communication
- Follow professional tone guidelines in `professional_tone_assessment.mdc`
- Use technical terminology, avoid emotional language
- Provide objective assessments with specific metrics
- Identify limitations honestly

### Testing Standards
- Follow TDD testing guidelines in `tdd_testing_guidelines.md`
- Test behavior, not implementation
- Use TestFactory patterns for test isolation
- Mock external dependencies consistently

### Code Quality
- Write readable, maintainable code
- Follow established patterns in the codebase
- Add appropriate error handling
- Document complex logic with comments

### Git Workflow
- Work in feature branches, never directly in next-dev
- Use descriptive commit messages without emojis
- Create pull requests for all changes
- Follow conventional commit format

### Project Structure
- Keep BDD scenarios in `docs/scenarios/` as documentation
- Focus on TDD unit tests in `ClubDoorman.Test/`
- Maintain clear separation between test types
- Use mocks for external dependencies

## File Organization

### Rules Structure
- `README.md` - Main rules file (this file)
- `professional_tone_assessment.mdc` - Professional communication standards
- `tdd_testing_guidelines.md` - TDD testing best practices

### Project Structure
- `ClubDoorman/` - Main application code
- `ClubDoorman.Test/` - Unit tests and test infrastructure
- `docs/scenarios/` - BDD scenarios as documentation
- `plans/` - Project planning and progress tracking
- `scripts/` - Utility scripts and automation

## Development Workflow

### Before Making Changes
1. Check current branch (should be feature branch)
2. Review relevant rules and guidelines
3. Understand existing code patterns
4. Plan test approach

### During Development
1. Follow TDD approach: write tests first
2. Use TestFactory patterns for test setup
3. Mock external dependencies
4. Maintain professional communication

### After Changes
1. Run all tests to ensure they pass
2. Update documentation if needed
3. Create pull request with descriptive title
4. Include status report with specific metrics

## Quality Standards

### Code Quality
- Readable and maintainable
- Proper error handling
- Appropriate logging
- Performance considerations

### Test Quality
- Behavior-focused tests
- Proper isolation
- Clear test names
- Comprehensive coverage

### Documentation Quality
- Clear and accurate
- Professional tone
- Specific examples
- Regular updates

---

**Note: These rules ensure consistent, professional development practices across the project.** 