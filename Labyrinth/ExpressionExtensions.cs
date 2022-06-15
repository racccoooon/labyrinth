using System.Linq.Expressions;

namespace Labyrinth;

public static class ExpressionExtensions
{
    public static Expression ReplaceParameter(this Expression expression, ParameterExpression param, Expression replacement)
    {
        return new ParameterReplacer(param, replacement).Visit(expression);
    }

    private class ParameterReplacer : ExpressionVisitor
    {
        private readonly ParameterExpression _param;
        private readonly Expression _replacement;
        public ParameterReplacer(ParameterExpression param, Expression replacement)
        {
            _param = param;
            _replacement = replacement;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (node == _param)
            {
                return _replacement;
            }

            return base.VisitParameter(node);
        }
    }
}