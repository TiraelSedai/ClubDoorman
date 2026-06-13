namespace ClubDoorman.Test;

public class RegexTests
{
    [TestCase("50 крипто-приваток в одном месте: t.me/+vxZqUUDQc045NzAy", true, TestName = "Example")]
    public void CryptoPrivatkiBio_Tests(string bio, bool expectedMatch)
    {
        var result = MyRegexes.CryptoPrivatkiBio().IsMatch(bio);
        Assert.That(result, Is.EqualTo(expectedMatch));
    }
}
