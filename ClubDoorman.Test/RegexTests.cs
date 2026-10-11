namespace ClubDoorman.Test;

public class RegexTests
{
    [TestCase("50 крипто-приваток в одном месте: t.me/+vxZqUUDQc045NzAy", true, TestName = "Example")]
    [TestCase("50 крипто-випок одном канале: t.me/+vxZqUUDQc045NzAy", true, TestName = "VipokChannel")]
    [TestCase("50 крипто-приваток одном канале: t.me/+vxZqUUDQc045NzAy", true, TestName = "PrivatkiChannel")]
    [TestCase("50 крипто-випок в одном месте: t.me/+vxZqUUDQc045NzAy", true, TestName = "VipokPlace")]
    [TestCase("Бесплатные крипто-приватки: t.me/+nEhsby0I9WxiNmUy", true, TestName = "FreePrivatki")]
    [TestCase("Крипто приватки тут t.me/+nEhsby0I9WxiNmUy", true, TestName = "PrivatkiNoHyphen")]
    [TestCase("Бесплатные крипто-приватки: t.me/somechannel", false, TestName = "PublicLink")]
    [TestCase("Криптография и приватность, пишите t.me/+nEhsby0I9WxiNmUy", false, TestName = "Cryptography")]
    public void CryptoPrivatkiBio_Tests(string bio, bool expectedMatch)
    {
        var result = MyRegexes.CryptoPrivatkiBio().IsMatch(bio);
        Assert.That(result, Is.EqualTo(expectedMatch));
    }

    [TestCase("Пиши сюда чтобы купить => @MXBW28", true, TestName = "Mention")]
    [TestCase("купить тут: t.me/mxbw28", true, TestName = "Link")]
    [TestCase("пиши @MXBW281", false, TestName = "LongerUsername")]
    [TestCase("пиши @xMXBW28", false, TestName = "PrefixedUsername")]
    [TestCase("Доход за активность, без вложений: t.me/MillionsGame_Bot?start=r10704", true, TestName = "MillionsGameReferral")]
    [TestCase("играю в @MillionsGame_Bot", true, TestName = "MillionsGameMention")]
    [TestCase("t.me/MillionsGame_Bot2?start=r1", false, TestName = "MillionsGameLongerUsername")]
    public void BlacklistedBioMention_Tests(string bio, bool expectedMatch)
    {
        var result = MyRegexes.BlacklistedBioMention().IsMatch(bio);
        Assert.That(result, Is.EqualTo(expectedMatch));
    }
}
