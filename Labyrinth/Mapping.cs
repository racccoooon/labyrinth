using System.Diagnostics;
using System.Linq.Expressions;

namespace Labyrinth;

public class Mapping<TTarget, TSource> : Mapping
{
    public Func<TSource, TTarget> CompiledMapper { get; private set; }

    private List<Expression> autoMaps = new();

    private List<PropertyMap> propertyMaps = new();

    public void AutoMap<T>(Expression<Func<TSource, T>> source)
    {
        autoMaps.Add(source);
    }

    public void MapProperty<T>(Expression<Func<TSource, T?>> source, Expression<Func<TTarget, T?>> dest)
    {
        Debug.Assert(dest.Parameters.Count == 1);
        Debug.Assert(ExpressionType.MemberAccess == dest.Body.NodeType);
        Debug.Assert(dest.Parameters[0] == (dest.Body as MemberExpression)!.Expression);
        var name = (dest.Body as MemberExpression)!.Member.Name;
        propertyMaps.Add(new PropertyMap(name, source.Parameters[0], source.Body));
    }

    public void MapProperty<T>(Expression<Func<TSource, T?>> source)
    {
        Debug.Assert(source.Parameters.Count == 1);
        Debug.Assert(ExpressionType.MemberAccess == source.Body.NodeType);
        Debug.Assert(source.Parameters[0] == (source.Body as MemberExpression)!.Expression);
        var name = (source.Body as MemberExpression)!.Member.Name;
        propertyMaps.Add(new PropertyMap(name));
    }


    public void Compile()
    {
        var sourceVar = Expression.Variable(typeof(TSource), "source");
        var destVar = Expression.Variable(typeof(TTarget), "dest");
        var exprs = new List<Expression>();
        exprs.Add(Expression.Assign(destVar, Expression.New(typeof(TTarget))));

        exprs.AddRange(autoMaps.Select(autoMap => GenerateAutoMap(autoMap, sourceVar, destVar)));

        exprs.AddRange(propertyMaps.Select(propertyMap => GeneratePropertyMap(propertyMap, sourceVar, destVar)));

        exprs.Add(destVar);

        this.CompiledMapper = Expression.Lambda<Func<TSource, TTarget>>(
            Expression.Block(new[] { destVar }, exprs),
            sourceVar).Compile();
    }

    private Expression GenerateAutoMap(Expression autoMap, Expression sourceVar,
        Expression destVar)
    {
        Debug.Assert(ExpressionType.Lambda == autoMap.NodeType);
        Debug.Assert(autoMap.GetType().IsGenericType);
        Debug.Assert(autoMap.GetType().GenericTypeArguments.Length == 1);
        Debug.Assert(autoMap.GetType().GenericTypeArguments[0].IsGenericType);
        Debug.Assert(typeof(Func<,>) == autoMap.GetType().GenericTypeArguments[0].GetGenericTypeDefinition());
        Debug.Assert(2 == autoMap.GetType().GenericTypeArguments[0].GenericTypeArguments.Length);
        Debug.Assert(typeof(TSource) == autoMap.GetType().GenericTypeArguments[0].GenericTypeArguments[0]);
        
        var autoMapLambda = (LambdaExpression)autoMap;

        var ctxVar = Expression.Variable(autoMapLambda.ReturnType, "ctx");

        var exprs = new List<Expression>();

        var ctxInit = autoMapLambda.Body.ReplaceParameter(autoMapLambda.Parameters[0], sourceVar);

        exprs.Add(Expression.Assign(ctxVar, ctxInit));

        foreach (var propertyInfo in autoMapLambda.ReturnType.GetProperties())
        {
            var targetProp = typeof(TTarget).GetProperty(propertyInfo.Name);
            if (targetProp == null) continue;

            if (targetProp.PropertyType != propertyInfo.PropertyType) continue;
            
            exprs.Add(GeneratePropertyMap(new PropertyMap(propertyInfo.Name, null, null), ctxVar, destVar));
        }

        return Expression.Block(new[] { ctxVar }, exprs);
    }


    private Expression MakeSetter(string name, Expression destVar, Expression valueParam)
    {
        var property = typeof(TTarget).GetProperty(name)!;
        var methodInfo = property.GetSetMethod(true)!;
        return Expression.Call(destVar, methodInfo, valueParam);
    }

    private Expression GeneratePropertyMap(PropertyMap map, Expression sourceVar, Expression destVar)
    {
        Expression sourceExpr;
        bool simple;
        if (map.SourceParam != null && map.SourceGetter != null)
        {
            simple = false;
            sourceExpr = map.SourceGetter.ReplaceParameter(map.SourceParam, sourceVar);
        }
        else
        {
            simple = true;
            sourceExpr = Expression.Property(sourceVar, map.Name);
        }

        var optional = !sourceExpr.Type.IsValueType || Nullable.GetUnderlyingType(sourceExpr.Type) != null;

        if (optional)
        {
            if (simple)
            {
                if (Nullable.GetUnderlyingType(sourceExpr.Type) != null)
                {
                    return Expression.IfThen(Expression.Property(sourceExpr, nameof(Nullable<int>.HasValue)),
                        MakeSetter(map.Name, destVar, Expression.Property(sourceExpr, nameof(Nullable<int>.Value)))
                    );
                }

                return Expression.IfThen(Expression.NotEqual(sourceExpr, Expression.Constant(null)),
                    MakeSetter(map.Name, destVar, sourceExpr));
            }
            else
            {
                var valueVar = Expression.Variable(sourceExpr.Type, "val");

                Expression ifThen;

                if (Nullable.GetUnderlyingType(sourceExpr.Type) != null)
                {
                    ifThen = Expression.IfThen(Expression.Property(sourceExpr, nameof(Nullable<int>.HasValue)),
                        MakeSetter(map.Name, destVar, Expression.Property(valueVar, nameof(Nullable<int>.Value)))
                    );
                }
                else
                {
                    ifThen = Expression.IfThen(Expression.NotEqual(sourceExpr, Expression.Constant(null)),
                        MakeSetter(map.Name, destVar, valueVar));
                }

                return Expression.Block(new[] { valueVar }, Expression.Assign(valueVar, sourceExpr), ifThen);
            }
        }
        else
        {
            return MakeSetter(map.Name, destVar, sourceExpr);
        }
    }

    private record PropertyMap(string Name, ParameterExpression? SourceParam = null, Expression? SourceGetter = null);
}

public abstract class Mapping
{
}