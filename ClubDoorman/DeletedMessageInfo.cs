namespace ClubDoorman;

internal enum DeletionReason
{
    MessageContent,
    UserProfile,
}

internal sealed record DeletedMessageInfo(
    string Key,
    long ChatId,
    long UserId,
    string UserFirstName,
    string? UserLastName,
    string? Username,
    string? Text,
    string? Caption,
    string? PhotoFileId,
    string? VideoFileId,
    int? ReplyToMessageId,
    DeletionReason Reason = DeletionReason.MessageContent
);
