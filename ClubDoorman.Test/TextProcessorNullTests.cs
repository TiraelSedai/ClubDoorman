using ClubDoorman.Services;
using NUnit.Framework;

namespace ClubDoorman.Test;

[TestFixture]
public class TextProcessorNullTests
{
    [Test]
    public void NormalizeText_WithNullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            TextProcessor.NormalizeText(null!));
        
        Assert.That(exception.ParamName, Is.EqualTo("input"));
    }

    [Test]
    public void NormalizeText_WithEmptyString_ReturnsEmptyString()
    {
        // Act
        var result = TextProcessor.NormalizeText("");
        
        // Assert
        Assert.That(result, Is.EqualTo(""));
    }

    [Test]
    public void NormalizeText_WithNormalText_ReturnsNormalizedText()
    {
        // Arrange
        var input = "Hello, World! 🌍";
        
        // Act
        var result = TextProcessor.NormalizeText(input);
        
        // Assert
        Assert.That(result, Is.EqualTo("hello world "));
    }

    [Test]
    public void NormalizeText_WithRussianText_ReturnsNormalizedText()
    {
        // Arrange
        var input = "Привет, мир! 🌍";
        
        // Act
        var result = TextProcessor.NormalizeText(input);
        
        // Assert
        Assert.That(result, Is.EqualTo("привет мир "));
    }

    [Test]
    public void NormalizeText_WithMultipleLines_ReturnsSingleLine()
    {
        // Arrange
        var input = "Line 1\nLine 2\r\nLine 3";
        
        // Act
        var result = TextProcessor.NormalizeText(input);
        
        // Assert
        Assert.That(result, Is.EqualTo("line 1 line 2 line 3"));
    }

    [Test]
    public void NormalizeText_WithMultipleSpaces_ReturnsSingleSpaces()
    {
        // Arrange
        var input = "  Multiple    spaces  ";
        
        // Act
        var result = TextProcessor.NormalizeText(input);
        
        // Assert
        Assert.That(result, Is.EqualTo(" multiple spaces "));
    }
} 