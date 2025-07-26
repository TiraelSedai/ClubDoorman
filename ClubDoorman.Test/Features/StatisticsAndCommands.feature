# language: en
Feature: Statistics and Commands
  As a chat administrator
  I want to view statistics and execute commands
  So that I can monitor chat activity and manage moderation

  Scenario: /stat command
    Given there is activity in chat (messages, bans, approvals)
    When the /stat command is executed
    Then correct statistics are displayed:
      | message count |
      | ban count |
      | approval count |
      | AI analyses |
      | ML analyses |
    And statistics are formatted correctly

  Scenario: Automatic statistics
    Given the system works throughout the day
    When automatic statistics time comes
    Then daily report is sent
    And statistics include all metrics
    And report is sent to correct chat

  Scenario: Command access rights
    Given a regular user tries to execute /spam command
    When the command is executed
    Then the command is ignored
    And there is a log record about unauthorized access attempt 