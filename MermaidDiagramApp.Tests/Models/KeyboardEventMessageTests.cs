using System.Text.Json;
using MermaidDiagramApp.Models;
using Xunit;

namespace MermaidDiagramApp.Tests.Models;

/// <summary>
/// Unit tests for KeyboardEventMessage serialization and deserialization.
/// </summary>
public class KeyboardEventMessageTests
{
    /// <summary>
    /// Test that KeyboardEventMessage can be deserialized from valid JSON.
    /// </summary>
    [Fact]
    public void Deserialize_WithValidJson_ReturnsCorrectObject()
    {
        // Arrange
        var json = @"{
            ""type"": ""keypress"",
            ""key"": ""F11"",
            ""ctrlKey"": true,
            ""shiftKey"": false,
            ""altKey"": false
        }";

        // Act
        var message = JsonSerializer.Deserialize<KeyboardEventMessage>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        Assert.NotNull(message);
        Assert.Equal("keypress", message.Type);
        Assert.Equal("F11", message.Key);
        Assert.True(message.CtrlKey);
        Assert.False(message.ShiftKey);
        Assert.False(message.AltKey);
    }

    /// <summary>
    /// Test that KeyboardEventMessage handles missing Type property.
    /// </summary>
    [Fact]
    public void Deserialize_WithMissingType_UsesDefaultValue()
    {
        // Arrange
        var json = @"{
            ""key"": ""Escape"",
            ""ctrlKey"": false,
            ""shiftKey"": false,
            ""altKey"": false
        }";

        // Act
        var message = JsonSerializer.Deserialize<KeyboardEventMessage>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        Assert.NotNull(message);
        Assert.Equal(string.Empty, message.Type);
        Assert.Equal("Escape", message.Key);
    }

    /// <summary>
    /// Test that KeyboardEventMessage handles missing Key property.
    /// </summary>
    [Fact]
    public void Deserialize_WithMissingKey_UsesDefaultValue()
    {
        // Arrange
        var json = @"{
            ""type"": ""keypress"",
            ""ctrlKey"": true,
            ""shiftKey"": false,
            ""altKey"": false
        }";

        // Act
        var message = JsonSerializer.Deserialize<KeyboardEventMessage>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        Assert.NotNull(message);
        Assert.Equal("keypress", message.Type);
        Assert.Equal(string.Empty, message.Key);
        Assert.True(message.CtrlKey);
    }

    /// <summary>
    /// Test that KeyboardEventMessage handles missing boolean properties.
    /// </summary>
    [Fact]
    public void Deserialize_WithMissingBooleanProperties_UsesDefaultFalse()
    {
        // Arrange
        var json = @"{
            ""type"": ""keypress"",
            ""key"": ""F7""
        }";

        // Act
        var message = JsonSerializer.Deserialize<KeyboardEventMessage>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        Assert.NotNull(message);
        Assert.Equal("keypress", message.Type);
        Assert.Equal("F7", message.Key);
        Assert.False(message.CtrlKey);
        Assert.False(message.ShiftKey);
        Assert.False(message.AltKey);
    }

    /// <summary>
    /// Test that KeyboardEventMessage can be serialized to JSON.
    /// </summary>
    [Fact]
    public void Serialize_WithValidObject_ReturnsCorrectJson()
    {
        // Arrange
        var message = new KeyboardEventMessage
        {
            Type = "keypress",
            Key = "F11",
            CtrlKey = true,
            ShiftKey = false,
            AltKey = false
        };

        // Act
        var json = JsonSerializer.Serialize(message, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // Assert
        Assert.Contains("\"type\":\"keypress\"", json);
        Assert.Contains("\"key\":\"F11\"", json);
        Assert.Contains("\"ctrlKey\":true", json);
        Assert.Contains("\"shiftKey\":false", json);
        Assert.Contains("\"altKey\":false", json);
    }

    /// <summary>
    /// Test round-trip serialization and deserialization.
    /// </summary>
    [Fact]
    public void RoundTrip_SerializeAndDeserialize_PreservesData()
    {
        // Arrange
        var original = new KeyboardEventMessage
        {
            Type = "keypress",
            Key = "Escape",
            CtrlKey = false,
            ShiftKey = true,
            AltKey = false
        };

        // Act
        var json = JsonSerializer.Serialize(original, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var deserialized = JsonSerializer.Deserialize<KeyboardEventMessage>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.Type, deserialized.Type);
        Assert.Equal(original.Key, deserialized.Key);
        Assert.Equal(original.CtrlKey, deserialized.CtrlKey);
        Assert.Equal(original.ShiftKey, deserialized.ShiftKey);
        Assert.Equal(original.AltKey, deserialized.AltKey);
    }

    /// <summary>
    /// Test that empty JSON object deserializes with default values.
    /// </summary>
    [Fact]
    public void Deserialize_WithEmptyJson_UsesDefaultValues()
    {
        // Arrange
        var json = "{}";

        // Act
        var message = JsonSerializer.Deserialize<KeyboardEventMessage>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        Assert.NotNull(message);
        Assert.Equal(string.Empty, message.Type);
        Assert.Equal(string.Empty, message.Key);
        Assert.False(message.CtrlKey);
        Assert.False(message.ShiftKey);
        Assert.False(message.AltKey);
    }
}
