using System;
using System.Collections.Generic;

namespace Labyrinth;

public class Mapper
{
    private Dictionary<(Type, Type), Mapping> mappings = new();

    public void Configure<TTarget, TSource>(Action<Mapping<TTarget, TSource>> setup)
    {
        var mapping = new Mapping<TTarget, TSource>();
        setup(mapping);
        mapping.Compile();
        mappings.Add((typeof(TTarget), typeof(TSource)), mapping);
    }

    public TTarget Map<TTarget, TSource>(TSource source)
    {
        if (mappings.TryGetValue((typeof(TTarget), typeof(TSource)), out var mapping))
        {
            return ((Mapping<TTarget, TSource>)mapping).CompiledMapper(source);
        }

        throw new Exception("you fucked up");
    }
}