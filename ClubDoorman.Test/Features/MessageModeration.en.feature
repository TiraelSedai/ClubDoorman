Feature: Message Moderation
  As a chat moderator
  I want to automatically check messages for compliance with rules
  To maintain communication quality in the group

  Background:
    Given the moderation system is initialized
    And user with ID 123456789 is not approved in the chat

  @blacklist
  Scenario: User in blacklist is automatically banned
    Given user with ID 987654321 is in blacklist lols.bot
    When user sends message "Hello everyone!"
    Then user should be banned
    And reason should be "Пользователь в блэклисте спамеров"
    And notification should be sent to admin chat

  @buttons
  Scenario: Message with buttons automatically bans user
    Given the moderation system is initialized
    And user with ID 123456789 is not approved in the chat
    Given user with ID 123456789 sends message with inline buttons
    When system checks message for moderation
    Then user should be banned
    And reason should be "Сообщение с кнопками"

  @story
  Scenario: Story messages are deleted
    Given user with ID 123456789 sends story
    When system checks message for moderation
    Then message should be deleted
    And reason should be "Story"

  @media_no_caption
  Scenario: Media without caption is sent for report
    Given user with ID 123456789 sends photo without caption
    When system checks message for moderation
    Then message should be sent for report
    And reason should be "Media without caption"

  @known_bad_message
  Scenario: Known spam message automatically bans user
    Given message "BUY OUR PRODUCT RIGHT NOW!" is in known bad messages list
    And user with ID 123456789 sends this message
    When system checks message for moderation
    Then user should be banned
    And reason should be "Known spam message"

  @emoji_filter
  Scenario: Too many emojis in message
    Given user with ID 123456789 sends message "Hello everyone! 😀😃😄😁😆😅😂🤣😊😇🙂🙃😉😌😍🥰😘😗😙😚😋😛😝😜🤪🤨🧐🤓😎🤩🥳😏😒😞😔😟😕🙁☹️😣😖😫😩🥺😢😭😤😠😡🤬🤯😳🥵🥶😱😨😰😥😓🤗🤔🤭🤫🤥😶😐😑😯😦😧😮😲🥱😴🤤😪😵🤐🥴🤢🤮🤧😷🤒🤕🤑🤠"
    When system checks message for moderation
    Then message should be deleted
    And reason should be "Too many emojis in this message"

  @lookalike_symbols
  Scenario: Words with lookalike symbols are blocked
    Given user with ID 123456789 sends message "Купіте наш товар прямо сейчас!"
    When system checks message for moderation
    Then message should be deleted
    And reason should contain "Words masking as Russian were found"

  @lookalike_autoban
  Scenario: Lookalike symbols with autoban
    Given LookAlikeAutoBan setting is enabled
    And user with ID 123456789 sends message "Купіте наш товар прямо сейчас!"
    When system checks message for moderation
    Then user should be banned
    And reason should contain "Words masking as Russian were found"

  @stop_words
  Scenario: Stop words detection
    Given user with ID 123456789 sends message "Buy our product right now!"
    When system checks message for moderation
    Then message should be deleted
    And reason should be "This message contains stop words"

  @ml_classification
  Scenario: ML classifier detects spam
    Given ML classifier is configured with threshold 0.5
    And user with ID 123456789 sends message "EARN MONEY FAST! SIMPLE WAY!"
    When system checks message for moderation
    And ML classifier returns result "spam" with confidence 0.8
    Then message should be deleted
    And reason should contain "ML decided this is spam, score 0.8"

  @low_confidence
  Scenario: Low confidence in ham requires manual review
    Given user with ID 123456789 sends message "Interesting article about technology"
    When system checks message for moderation
    And ML classifier returns result "ham" with confidence -0.4
    And LowConfidenceHamForward setting is enabled
    Then message should be sent for manual review
    And reason should contain "Classifier thinks this is NOT spam, but confidence is low"

  @normal_message
  Scenario: Normal message passes all checks
    Given user with ID 123456789 sends message "Hello! How is everyone doing?"
    When system checks message for moderation
    And ML classifier returns result "ham" with confidence -1.2
    Then message should be allowed
    And reason should be "Message passed all checks"
    And user's good message counter should increase

  @media_filtering_disabled
  Scenario: Media filtering disabled for chat
    Given media filtering is disabled for chat 123456789
    And user with ID 987654321 sends photo with caption "Beautiful photo"
    When system checks message for moderation
    Then message should be allowed
    And reason should be "Message passed all checks"

  @media_filtering_enabled
  Scenario: Media filtering enabled - photo is blocked
    Given media filtering is enabled for chat 123456789
    And user with ID 987654321 sends photo with caption "Beautiful photo"
    When system checks message for moderation
    Then message should be deleted
    And reason should be "Cannot send images or videos in first three messages"

  @sticker_document_blocked
  Scenario: Stickers and documents are always blocked
    Given user with ID 987654321 sends sticker
    When system checks message for moderation
    Then message should be deleted
    And reason should be "Cannot send stickers or documents in first three messages"

  @announcement_chat_media_allowed
  Scenario: Media is allowed in announcement chats
    Given chat 123456789 is an announcement chat
    And user with ID 987654321 sends photo with caption "Important announcement"
    When system checks message for moderation
    Then message should be allowed
    And reason should be "Message passed all checks"

  @empty_message_media
  Scenario: Empty message with media
    Given user with ID 123456789 sends photo without caption
    When system checks message for moderation
    Then message should be sent for report
    And reason should be "Media without caption"

  @cached_message
  Scenario: Message text is cached
    Given user with ID 123456789 sends message "Test message"
    When system checks message for moderation
    Then message text should be saved in cache
    And cache should contain key "123456789_123456789"

  @null_message
  Scenario: Null message throws exception
    When system checks null message for moderation
    Then ArgumentNullException should be thrown
    And exception message should contain "Message cannot be null"

  @null_user
  Scenario: Message without user information
    Given message without user information
    When system checks message for moderation
    Then ModerationException should be thrown
    And exception message should contain "Message must contain user information" 