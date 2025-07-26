# language: en
Feature: AI Profile Analysis
  As a chat administrator
  I want AI to analyze suspicious user profiles
  So that I can protect the chat from bots and spam

  Scenario: AI analysis on first message
    Given a user with bait profile joins the group
    When the user sends the first message
    Then AI profile analysis is performed
    And a notification is sent to admin chat with profile photo
    And the user gets restricted for 10 minutes
    And there is a log record about AI analysis

  Scenario: Admin button "🥰 own"
    Given there is a notification with buttons in admin chat
    When the button "🥰 own" is clicked
    Then the user is added to global approved list
    And the restriction is removed
    And there is a log record about approval

  Scenario: Admin button "🤖 ban"
    Given there is a notification with buttons in admin chat
    When the button "🤖 ban" is clicked
    Then the user gets banned
    And all user messages are deleted
    And there is a log record about ban

  Scenario: AI analysis in channels
    Given a user with bait profile joins the channel
    When the user leaves a comment
    Then AI profile analysis is performed
    And a notification is sent to admin chat
    And the captcha is NOT shown (channels don't support captcha) 