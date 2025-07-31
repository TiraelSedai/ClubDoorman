# language: en
@BDD
Feature: Captcha System
  As a chat administrator
  I want to protect the chat from bots
  So that I can ensure safe communication

  Scenario: Captcha timeout
    Given a user joins the group
    When a captcha is sent
    And the user does not respond within timeout
    Then the user gets banned
    And there is a log record about captcha timeout
    And all user messages are deleted

  Scenario: Wrong captcha button
    Given a user joins the group
    When a captcha is sent
    And the user clicks the wrong button
    Then the user gets banned
    And there is a log record about wrong answer
    And all user messages are deleted

  Scenario: Correct captcha completion
    Given a user joins the group
    When a captcha is sent
    And the user clicks the correct button
    Then the captcha is removed
    And there is a log record about successful completion

  Scenario: Captcha in silent mode
    Given the bot works in silent mode
    When a user joins the group
    Then the captcha is sent without admin rights
    And the user can pass the captcha 