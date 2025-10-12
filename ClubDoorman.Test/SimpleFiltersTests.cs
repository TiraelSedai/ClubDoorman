namespace ClubDoorman.Test;

public class Tests
{
    [TestCase(">хром", false, TestName = "4ch quote")]
    [TestCase("привет", false, TestName = "CyrillicWord_NoLookAlikes")]
    [TestCase("приве7", false, TestName = "CyrillicWord_WithDigit")]
    [TestCase("вас3к", false, TestName = "CyrillicWord_WithMixedLookAlikes")]
    [TestCase("мирu", true, TestName = "MostlyCyrillicWord_WithLookAlikes")]
    [TestCase("Прuвет!", true, TestName = "MostlyCyrillicWord_WithLookAlikes2")]
    [TestCase("privет", false, TestName = "PartiallyCyrillicWord_WithLookAlikes")]
    [TestCase("hello", false, TestName = "PurelyNonCyrillicWord")]
    [TestCase("", false, TestName = "EmptyString")]
    [TestCase("д", false, TestName = "SingleCyrillicCharacter")]
    [TestCase("a", false, TestName = "SingleNonCyrillicCharacter")]
    [TestCase("ПриВет!", false, TestName = "CyrillicWord_WithPunctuation")]
    [TestCase("Ищy людeй в комaнду, прuятный заработоk, мuнuмум затpат по верменu, подpобностu в лuчку", true, TestName = "BigSentence")]
    [TestCase("Ищyлюдeйαπό", true, TestName = "Greek")]
    [TestCase("это русское слово и в нём есть", false, TestName = "Cyrillic Small Letter Io")]
    [TestCase("Именно так в этой сфере всë и устроено.", false, TestName = "Weird Letter Io")]
    [TestCase("хей", false, TestName = "IKratkoe")]
    [TestCase("адказ на пытанне хто вы з гэтай кнігі", false, TestName = "Belarus")]
    [TestCase("натомість використовують код альтернативних клієнтів", false, TestName = "Ukrainian")]
    [TestCase("Велико Търново, Зелено дърво", false, TestName = "Bulgarian")]
    [TestCase("Дорћол, Љекар, Њега", false, TestName = "Serbian")]
    [TestCase("најмање, пријављено, територији, србије, критеријум", false, TestName = "Serbian2")]
    public void HasLookAlikeSymbols_Tests(string word, bool expectedResult)
    {
        var result = SimpleFilters.FindAllRussianWordsWithLookalikeSymbols(word);
        Assert.That(result.Count > 0, Is.EqualTo(expectedResult), string.Join(", ", result));
    }

    [TestCase]
    public void FormatStripped()
    {
        const string spam =
            "\u2068g\u2068o\u2068 \u2068f\u2068a\u2068\u2068\u2068\u2068\u2068\u2068\u2068s\u2068t\u2068\ud83e\udd71  F\u2068\u2068R\u2068\u2068E\u2068EC\u2068L\u2068\u2068A\u2068\u2068\u2068I\u2068\u2068M\u2068\u2068 N\u2068\u2068F\u2068\u2068\u2068T\u2068\u2068  I\u2068\u2068F \u2068\u2068\u2068AL\u2068\u2068RE\u2068\u2068A\u2068\u2068D\u2068Y 5\u2068\u20680\u2068\u20680\u2068\u20680\u2068\u2068\u2068\u2068/\u2068\u206850\u2068\u20680\u2068\u20680\u2068\u2068\u2068 \u2068yo\u2068u\u2068 ar\u2068e t\u2068\u2068o\u2068\u2068o \u2068l\u2068ate,\u2068 so\u2068r\u2068\u2068r\u2068y";
        var norm = TextProcessor.NormalizeText(spam);
        Assert.That(norm, Has.Length.LessThan(70));
    }

    [TestCase("привет", false, TestName = "NoEmoji")]
    [TestCase("♥️", true, TestName = "heart")]
    [TestCase("🩷", true, TestName = "heart2")]
    [TestCase("💧", true, TestName = "droplet")]
    [TestCase("💧", true, TestName = "droplet")]
    [TestCase("Поздравляю! 🔥🔥🔥", false, TestName = "three emoji")]
    [TestCase(
        "✅Получению водительских прав (новые или дубликат);\r\n✅Открытие категории;\r\n✅Миграционка;\r\n✅Регистратура;\r\n✅Патент;\r\n✅Чек;\r\n✅Мед книжка;\r\n✅Мед карта;",
        "true",
        TestName = "lots of emoji"
    )]
    public void Emoji_Tests(string word, bool expectedResult)
    {
        var result = SimpleFilters.TooManyEmojis(word);
        Assert.That(result, Is.EqualTo(expectedResult), string.Join(", ", result));
    }
}
