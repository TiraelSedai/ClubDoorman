namespace ClubDoorman.Test;

public class RegexTests
{
    [TestCase("50 крипто-приваток в одном месте: t.me/+vxZqUUDQc045NzAy", true, TestName = "Example")]
    [TestCase("50 крипто-випок одном канале: t.me/+vxZqUUDQc045NzAy", true, TestName = "VipokChannel")]
    [TestCase("50 крипто-приваток одном канале: t.me/+vxZqUUDQc045NzAy", true, TestName = "PrivatkiChannel")]
    [TestCase("50 крипто-випок в одном месте: t.me/+vxZqUUDQc045NzAy", true, TestName = "VipokPlace")]
    public void CryptoPrivatkiBio_Tests(string bio, bool expectedMatch)
    {
        var result = MyRegexes.CryptoPrivatkiBio().IsMatch(bio);
        Assert.That(result, Is.EqualTo(expectedMatch));
    }
}
