Feature: Check Command
  As an administrator
  I want to check message content for spam
  So that I can analyze suspicious messages

  Background:
    Given I am an administrator
    And I have a message handler

  Scenario: Check spam message content
    Given I reply to a spam message with check command "/check"
    When I send the check command
    Then I should receive spam analysis results
    And the analysis should include emoji check
    And the analysis should include stop words check
    And the analysis should include ML classifier results

  Scenario: Check normal message content
    Given I reply to a normal message with check command "/check"
    When I send the check command
    Then I should receive spam analysis results
    And the analysis should show "спам False"

  Scenario: Check message with emojis
    Given I reply to a message with emojis with check command "/check"
    When I send the check command
    Then I should receive spam analysis results
    And the analysis should include emoji check

  Scenario: Check message with stop words
    Given I reply to a message with stop words with check command "/check"
    When I send the check command
    Then I should receive spam analysis results
    And the analysis should include stop words check

  Scenario: Check command without reply should fail
    Given I send "/check" without replying to a message
    When I send the check command
    Then I should receive a check error message
    And the error should indicate I need to reply to a message

  Scenario: Non-admin user cannot use check command
    Given I am not an administrator
    And I reply to a user's message with check command "/check"
    When I send the check command
    Then I should receive a check access denied message
    And no analysis results should be displayed 