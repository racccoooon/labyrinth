
# Labyrinth

This is a cute package for raccoons to map objects.

## Usage/Examples

```csharp
var mapper = new Mapper();
mapper.Configure<TargetType, SourceType>(mapping =>
{
    mapping.AutoMap(s => s.Data);
    mapping.MapProperty(s => s.Id);
    mapping.MapProperty(s => s.Num + 1, t => t.Number);
});

var result = mapper.Map<TargetType, SourceType>(new()
{
    Id = 123,
    Number = 68,
    Data = new()
    {
        Content = "foobar"
    }
});

// result == TargetType {Id = 123, Number = 69, Content = "foobar"}

```


## Authors

[a bunch of raccoons](https://github.com/racccoooon/)


## License

[MIT](https://choosealicense.com/licenses/mit/)

