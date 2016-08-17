using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace MicroRuleEngine
{
    public static class RuleCompiler
    {
        public static Func<T, bool> Compile<T>(Rule rule)
        {
            var expressionParameter = Expression.Parameter(typeof(T));
            var expression = ExpressionBuilder.Build<T>(rule, expressionParameter);

            return Expression.Lambda<Func<T, bool>>(expression, expressionParameter).Compile();
        }

        public static Func<T, bool> Compile<T>(IEnumerable<Rule> rules)
        {
            var expressionParameter = Expression.Parameter(typeof(T));
            var expression = ExpressionBuilder.Build<T>(rules, expressionParameter, ExpressionType.And);
            return Expression.Lambda<Func<T, bool>>(expression, expressionParameter).Compile();
        }
    }
}