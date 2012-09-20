using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace MicroRuleEngine
{
    public class MRE
    {
        private ExpressionType[] nestedOperators = new ExpressionType[] { ExpressionType.And, ExpressionType.AndAlso, ExpressionType.Or, ExpressionType.OrElse };

        public bool PassesRules<T>(IList<Rule> rules, T toInspect)
        {
            return this.CompileRules<T>(rules).Invoke(toInspect);
        }

        public Func<T, bool> CompileRule<T>(Rule r)
        {
            var paramUser = Expression.Parameter(typeof(T));
            Expression expr = GetExpressionForRule<T>(r, paramUser);

            return Expression.Lambda<Func<T, bool>>(expr, paramUser).Compile();
        }

        Expression GetExpressionForRule<T>(Rule r, ParameterExpression param)
        {
            ExpressionType nestedOperator;
            if (ExpressionType.TryParse(r.Operator, out nestedOperator) && nestedOperators.Contains(nestedOperator) && r.Rules != null && r.Rules.Any())
                return BuildNestedExpression<T>(r.Rules, param, nestedOperator);
            else
                return BuildExpr<T>(r, param);
        }

        public Func<T, bool> CompileRules<T>(IList<Rule> rules)
        {
            var paramUser = Expression.Parameter(typeof(T));
            var expr = BuildNestedExpression<T>(rules, paramUser, ExpressionType.And);
            return Expression.Lambda<Func<T, bool>>(expr, paramUser).Compile();
        }

        Expression BuildNestedExpression<T>(IList<Rule> rules, ParameterExpression param, ExpressionType operation)
        {
            List<Expression> expressions = new List<Expression>();
            foreach (var r in rules)
            {
                expressions.Add(GetExpressionForRule<T>(r,param));
            }

            Expression expr = BinaryExpression(expressions, operation);
            return expr;
        }

        Expression BinaryExpression(IList<Expression> expressions, ExpressionType operationType)
        {
            Func<Expression, Expression, Expression> methodExp = new Func<Expression, Expression, Expression>((x1, x2) => Expression.And(x1, x2));
            switch (operationType)
            {
                case ExpressionType.Or:
                    methodExp = new Func<Expression, Expression, Expression>((x1, x2) => Expression.Or(x1, x2));
                    break;
                case ExpressionType.OrElse:
                    methodExp = new Func<Expression, Expression, Expression>((x1, x2) => Expression.OrElse(x1, x2));
                    break;
                case ExpressionType.AndAlso:
                    methodExp = new Func<Expression, Expression, Expression>((x1, x2) => Expression.AndAlso(x1, x2));
                    break;
            }

            if (expressions.Count == 1)
                return expressions[0];
            Expression exp = methodExp(expressions[0], expressions[1]);
            for (int i = 2; expressions.Count > i; i++)
            {
                exp = methodExp(exp, expressions[i]);
            }
            return exp;
        }

        Expression AndExpressions(IList<Expression> expressions)
        {
            if (expressions.Count == 1)
                return expressions[0];
            Expression exp = Expression.And(expressions[0], expressions[1]);
            for (int i = 2; expressions.Count > i; i++)
            {
                exp = Expression.And(exp, expressions[i]);
            }
            return exp;
        }

        Expression OrExpressions(IList<Expression> expressions)
        {
            if (expressions.Count == 1)
                return expressions[0];
            Expression exp = Expression.Or(expressions[0], expressions[1]);
            for (int i = 2; expressions.Count > i; i++)
            {
                exp = Expression.Or(exp, expressions[i]);
            }
            return exp;
        }

        Expression BuildExpr<T>(Rule r, ParameterExpression param)
        {
            Expression propExpression = null;
            Type propType = null;

            ExpressionType tBinary;
            if (string.IsNullOrEmpty(r.MemberName))//check is against the object itself
            {
                propExpression = param;
                propType = propExpression.Type;
            }
            else if (r.MemberName.Contains('.'))//Child property
            {
                String[] childProperties = r.MemberName.Split('.');
                var property = typeof(T).GetProperty(childProperties[0]);
                var paramExp = Expression.Parameter(typeof(T), "SomeObject");

                propExpression = Expression.PropertyOrField(param, childProperties[0]);
                for (int i = 1; i < childProperties.Length; i++)
                {
                    var orig = property;
                    property = property.PropertyType.GetProperty(childProperties[i]);
                    if (property != null)
                        propExpression = Expression.PropertyOrField(propExpression, childProperties[i]);
                }
                propType = propExpression.Type;
            }
            else//Property
            {
                propExpression = Expression.PropertyOrField(param, r.MemberName);
                propType = propExpression.Type;
            }

            // is the operator a known .NET operator?
            if (ExpressionType.TryParse(r.Operator, out tBinary))
            {
                var right = this.StringToExpression(r.TargetValue, propType);
                return Expression.MakeBinary(tBinary, propExpression, right);
            }
            else if (r.Operator == "IsMatch")
            {
                return Expression.Call(
                    typeof(Regex).GetMethod("IsMatch",
                        new[] { typeof(string), typeof(string), typeof(RegexOptions) }),
                    propExpression,
                    Expression.Constant(r.TargetValue, typeof(string)),
                    Expression.Constant(RegexOptions.IgnoreCase, typeof(RegexOptions))
                );
            }
            else //Invoke a method on the Property
            {
                var inputs = r.Inputs.Select(x=> x.GetType()).ToArray();
                var methodInfo = propType.GetMethod(r.Operator, inputs);
                if (!methodInfo.IsGenericMethod)
                    inputs = null;//Only pass in type information to a Generic Method
                var expressions = r.Inputs.Select(x => Expression.Constant(x)).ToArray();
                return Expression.Call(propExpression, r.Operator,inputs,expressions);
            }
        }

        private Expression StringToExpression(string value, Type propType)
        {
            ConstantExpression right = null;
            if (value.ToLower() == "null")
            {
                right = Expression.Constant(null);
            }
            else
            {
                right = Expression.Constant(Convert.ChangeType(value, propType));
            }
            return right;
        }
    }

    public class Rule
    {
        public Rule()
        {
            Inputs = new List<object>();
        }
        public string MemberName { get; set; }
        public string Operator { get; set; }
        public string TargetValue { get; set; }
        public List<Rule> Rules { get; set; }
        public List<object> Inputs { get; set; }
    }

    public class RuleValue<T>
    {
        public T Value { get; set; }
        public List<Rule> Rules { get; set; }
    }

    public class RuleValueString : RuleValue<string> { }
}
