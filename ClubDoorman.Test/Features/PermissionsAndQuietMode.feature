# language: en
Feature: Permissions and Quiet Mode
  As a chat administrator
  I want to manage bot permissions and quiet mode
  So that the bot can work in different environments

  Scenario: Working with admin rights
    Given the bot has admin rights in group
    When a user joins the group
    Then captcha is sent with admin rights
    And the bot can delete messages
    And the bot can restrict users

  Scenario: Quiet mode
    Given the bot works in quiet mode without admin rights
    When a user joins the group
    Then captcha is sent without admin rights
    And the bot CANNOT delete messages directly
    And the bot uses alternative moderation methods

  Scenario: Disable captcha by chat ID
    Given captcha is enabled in settings
    When captcha is disabled for specific chat by ID
    Then captcha is NOT sent in this chat
    And users pass without verification
    And there is a log record about disabling 