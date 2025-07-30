Feature: Spam and Ham Commands
  As an administrator
  I want to mark messages as spam or ham
  So that I can train the ML model and moderate content

  Background:
    Given the bot is running in a group chat

  @critical
  Scenario: Admin can mark message as spam
    Given I am an admin and reply to a message with "/spam"
    When I execute the command
    Then the command should be processed successfully

  @critical
  Scenario: Admin can mark message as ham
    Given I am an admin and reply to a message with "/ham"
    When I execute the command
    Then the command should be processed successfully

  @critical
  Scenario: Regular user cannot use spam/ham commands
    Given I am a regular user and reply to a message with "/spam"
    When I execute the command
    Then I should receive an access denied message for spam ham

  @critical
  Scenario: Command without reply should fail
    Given I send "/spam" without replying to a message
    When I execute the command
    Then I should receive an error message 