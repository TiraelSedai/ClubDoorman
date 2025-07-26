# language: en
Feature: Moderation Flow
  As a chat administrator
  I want messages to be moderated automatically
  So that spam and inappropriate content is filtered

  Scenario: Check order in logs
    Given a user sends a message
    Then the logs check strict order:
      | 1. lols.bot blacklist |
      | 2. long names |
      | 3. captcha |
      | 4. stop words |
      | 5. ML analysis |
      | 6. AI analysis (only for first message) |

  Scenario: Spam message
    Given a user sends a spam message
    When ML/stop words/known spam triggers
    Then the message is deleted
    And AI check is NOT performed in admin chat
    And there is a log record about spam

  Scenario: ML model training
    Given there is a message in chat
    When the /spam command is executed
    Then the message is added to dataset as spam
    And there is a log record about training

  Scenario: Forward messages
    Given a user forwards a message
    When the message passes checks
    Then the forward is also deleted for spam
    And there is a log record about forward 