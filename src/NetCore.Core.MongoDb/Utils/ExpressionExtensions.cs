using System;
using System.Linq.Expressions;

namespace NetCore.Core.MongoDb.Utils
{
    public static class ExpressionExtensions
    {
        public static string GetPropertyName<T, TProperty>(this Expression<Func<T, TProperty>> expression)
        {
            MemberExpression member = null;

            if (expression.Body is UnaryExpression)
                member = (MemberExpression)((UnaryExpression)expression.Body).Operand;

            if (expression.Body is MemberExpression)
                member = expression.Body as MemberExpression;

            if (member == null)
                throw new InvalidOperationException("Expression must be a member expression");

            return member.Member.Name;
        }
    }
}