In NUnit tests, use `using (Assert.EnterMultipleScope())` instead of `Assert.Multiple(() => ...)`.

## Commit messages

Single short lowercase line, imperative/descriptive (e.g. `nag for delete rights instead of ignoring chats`). No conventional-commits prefixes (`feat:`, `fix:`, `chore:`). No body unless needed. Never add a `Co-authored-by` trailer or any Claude/AI attribution.

## Code map

`ClubDoorman/MessageProcessor.cs` is ~1700 lines: `grep -n` a symbol below and read only that method.

- Moderation pipeline: `MessageProcessor.HandleUpdate` routes reactions, callbacks, joins and admin-chat messages; `CheckMessageContent` runs filters, the ML classifier and the LLM on each message.
- Paid vs free chat (платный / бесплатный чат): `Config.NonFreeChat` is true for chats in `DOORMAN_ADMIN_CHAT_MAP`, or for every chat when the map is empty; free chats get LLM warnings from `MessageProcessor.FreeChatLlmChecks` instead of admin reports.
- Report actions (users say "dontdelete", "deletedontreport", "autoban"): `MessageProcessor.DontDeleteButReportMessage` ("Сообщение НЕ удалено") and `DontDeleteButReportMessageWithApprove`, `DeleteAndReportMessage` ("Сообщение УДАЛЕНО", 10-minute read-only, restore button), `AutoBan` ("Авто-бан").
  In a free chat `DontDeleteButReportMessage` does nothing and `DeleteAndReportMessage` deletes without a report.
- Admin buttons and commands (ban, ok, approve, restore, `/spam`, `/ham`, `/check`, `/undo`, `/latest`, `/unban`): `AdminCommandHandler.HandleAdminCallback` and `AdminCommandHandler.AdminChatMessage`; `/undo` is the dataset rollback (откат датасета).
- Dataset storage (spam-ham, датасет): the SQLite table `AppDbContext.SpamHamRecords`; `data/spam-ham.txt` only seeds an empty table in `Program.SeedData`.
- Background dataset review (consensus, Jev + Luna, "автоматически добавлено как пример"): the verdict and its thresholds live in `AiChecks.GetSpamConsensus`, as literals rather than in `Consts`.
  It is launched by `MessageProcessor.StartDatasetReview` (gated on the ML score band) or `RunDatasetReview`, and `AddConsensusExample` saves through `AdminCommandHandler.AddAutomaticExample`.
  Luna is the paid OpenRouter model `AiChecks.PaidModel`; Jev is `JevChecks`.
- Manual dataset suggestion ("Хорошая идея - добавить сообщение в датасет", "Возможно стоит добавить в ham"): every such branch passes its forwarder as `onNoConsensus`, and calls it directly when the review did not start, so admins are asked only when the consensus abstains.
  - `ForwardApprovedUserHam`: trusted (approved) user whom ML calls spam, in `HandleUpdate`.
  - `ForwardManualDatasetReview`: low-confidence ham in `CheckMessageContent`; picks `ForwardLowConfidenceHam` or `ForwardHighConfidenceLlmSpam` from Luna's verdict.
  - `ForwardHighConfidenceLlmSpam`: confident Luna spam below the review band, passed to `RunDatasetReview` in `CheckMessageContent`.
- Greeting deletion ("привет и больше ничего"): `SimpleFilters.HasOnlyHelloWord`, beside the stop-word, emoji and lookalike-letter filters.
- Profile checks (bio, эротика, полиция нравов): `MessageProcessor.CheckUserProfile` and `MessageProcessor.CheckUserBio`.
- End-to-end moderation tests: the private `Fixture.Create(...)` in `ClubDoorman.Test/MessageProcessorMlOverrideTests.cs` fakes Telegram, Luna and Jev HTTP over in-memory SQLite and drives `HandleUpdate` or `CheckMessageContent`; add tests to that file and extend `Create` parameters.
- Formatter: `dotnet-tools.json` at the repo root pins csharpier, and `.git/hooks/pre-commit` runs `dotnet tool restore` and csharpier on staged `.cs` files.
