using ClubDoorman.Services;
using NUnit.Framework;

namespace ClubDoorman.Test;

[TestFixture]
public class SimpleFiltersNullTests
{
    [Test]
    public void HasStopWords_WithNullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            SimpleFilters.HasStopWords(null!));
        
        Assert.That(exception.ParamName, Is.EqualTo("message"));
    }

    [Test]
    public void HasStopWords_WithEmptyString_ReturnsFalse()
    {
        // Act
        var result = SimpleFilters.HasStopWords("");
        
        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void HasStopWords_WithNormalText_ReturnsFalse()
    {
        // Act
        var result = SimpleFilters.HasStopWords("Привет, как дела?");
        
        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void FindAllRussianWordsWithLookalikeSymbols_WithNullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            SimpleFilters.FindAllRussianWordsWithLookalikeSymbols(null!));
        
        Assert.That(exception.ParamName, Is.EqualTo("message"));
    }

    [Test]
    public void FindAllRussianWordsWithLookalikeSymbols_WithEmptyString_ReturnsEmptyList()
    {
        // Act
        var result = SimpleFilters.FindAllRussianWordsWithLookalikeSymbols("");
        
        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void FindAllRussianWordsWithLookalikeSymbolsInNormalizedText_WithNullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            SimpleFilters.FindAllRussianWordsWithLookalikeSymbolsInNormalizedText(null!));
        
        Assert.That(exception.ParamName, Is.EqualTo("message"));
    }

    [Test]
    public void FindAllRussianWordsWithLookalikeSymbolsInNormalizedText_WithEmptyString_ReturnsEmptyList()
    {
        // Act
        var result = SimpleFilters.FindAllRussianWordsWithLookalikeSymbolsInNormalizedText("");
        
        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void TooManyEmojis_WithNullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            SimpleFilters.TooManyEmojis(null!));
        
        Assert.That(exception.ParamName, Is.EqualTo("message"));
    }

    [Test]
    public void TooManyEmojis_WithEmptyString_ReturnsFalse()
    {
        // Act
        var result = SimpleFilters.TooManyEmojis("");
        
        // Assert
        Assert.That(result, Is.False);
    }
} 