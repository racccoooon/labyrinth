using FluentAssertions;
using Xunit;

namespace Labyrinth.Test;

public class MapperTest
{
    [Fact]
    public void MapProperty()
    {
        var mapper = new Mapper();
        mapper.Configure<PatchCardCommand, PatchCardCommandParameters>(mapping =>
        {
            mapping.MapProperty(p => p.Id, c => c.CardId);
            mapping.MapProperty(p => p.UserId);
            mapping.MapProperty(p => p.Comment);
        });

        var result = mapper.Map<PatchCardCommand, PatchCardCommandParameters>(new()
        {
            UserId = 1,
            Id = 10,
            Data = new()
            {
                Content = "foobar"
            },
            Comment = ""
        });

        result.Should().BeOfType<PatchCardCommand>();
        result.Should().Be(new PatchCardCommand
        {
            CardId = 10,
            UserId = 1,
            Comment = ""
        });
    }

    [Fact]
    public void MapPropertyComputed()
    {
        var mapper = new Mapper();
        mapper.Configure<PatchCardCommand, PatchCardCommandParameters>(mapping =>
        {
            mapping.MapProperty(p => p.Id * 2, c => c.CardId);
            mapping.MapProperty(p => p.UserId * 3, c => c.UserId);
        });

        var result = mapper.Map<PatchCardCommand, PatchCardCommandParameters>(new()
        {
            UserId = 1,
            Id = 10,
            Data = new()
            {
                Content = "foobar"
            },
            Comment = ""
        });

        result.Should().BeOfType<PatchCardCommand>();
        result.Should().Be(new PatchCardCommand
        {
            CardId = 20,
            UserId = 3,
        });
    }
    
    [Fact]
    public void AutoMap()
    {
        var mapper = new Mapper();
        mapper.Configure<PatchCardCommand, PatchCardCommandParameters>(mapping =>
        {
            mapping.AutoMap(p => p.Data);
            mapping.AutoMap(p => p);
        });

        var result = mapper.Map<PatchCardCommand, PatchCardCommandParameters>(new()
        {
            UserId = 1,
            Id = 10,
            Data = new()
            {
                Content = "foobar"
            },
            Comment = ""
        });

        result.Should().BeOfType<PatchCardCommand>();
        result.Should().Be(new PatchCardCommand
        {
            UserId = 1,
            Content = "foobar",
            Comment = ""
        });
    }
}

record PatchCardCommandParameters
{
    public int UserId { get; init; }
    public int Id { get; init; }

    public string? Comment { get; init; }
    public PatchCardDto Data { get; init; }
}

record PatchCardDto
{
    public string? Title { get; init; }
    public string? Content { get; init; }
}

record PatchCardCommand
{
    public int UserId { get; init; }
    public int CardId { get; init; }
    public string? Comment { get; init; }
    public string? Title { get; init; }
    public string? Content { get; init; }
}