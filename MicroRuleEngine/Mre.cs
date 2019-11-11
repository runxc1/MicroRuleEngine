using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MicroRuleEngine
{
    public class Mre
    {
        public enum OperatorType
        {
            InternalString = 1,
            ObjectMethod = 2,
            Comparison = 3,
            Logic = 4
        }

        private static readonly ExpressionType[] NestedOperators =
            {ExpressionType.And, ExpressionType.AndAlso, ExpressionType.Or, ExpressionType.OrElse};

        private static readonly Lazy<MethodInfo> MiRegexIsMatch = new Lazy<MethodInfo>(() =>
            typeof(Regex).GetMethod("IsMatch", new[] { typeof(string), typeof(string), typeof(RegexOptions) }));

        private static readonly Lazy<MethodInfo> MiGetItem = new Lazy<MethodInfo>(() =>
            typeof(DataRow).GetMethod("get_Item", new[] { typeof(string) }));

        private static readonly Tuple<string, Lazy<MethodInfo>>[] EnumrMethodsByName =
        {
            Tuple.Create("Any", new Lazy<MethodInfo>(() => GetLinqMethod("Any", 2))),
            Tuple.Create("All", new Lazy<MethodInfo>(() => GetLinqMethod("All", 2)))
        };

        private static readonly Lazy<MethodInfo> MiIntTryParse = new Lazy<MethodInfo>(() =>
            typeof(int).GetMethod("TryParse", new[] { typeof(string), Type.GetType("System.Int32&") }));

        private static readonly Lazy<MethodInfo> MiFloatTryParse = new Lazy<MethodInfo>(() =>
            typeof(float).GetMethod("TryParse", new[] { typeof(string), Type.GetType("System.Single&") }));

        private static readonly Lazy<MethodInfo> MiDoubleTryParse = new Lazy<MethodInfo>(() =>
            typeof(double).GetMethod("TryParse", new[] { typeof(string), Type.GetType("System.Double&") }));

        private static readonly Lazy<MethodInfo> MiDecimalTryParse = new Lazy<MethodInfo>(() =>
            typeof(decimal).GetMethod("TryParse", new[] { typeof(string), Type.GetType("System.Decimal&") }));

        private static readonly Regex RegexIndexed = new Regex(@"(\w+)\[(\d+)\]", RegexOptions.Compiled);

        public Func<T, bool> CompileRule<T>(Rule r)
        {
            var paramUser = Expression.Parameter(typeof(T));
            var expr = GetExpressionForRule(typeof(T), r, paramUser);

            return Expression.Lambda<Func<T, bool>>(expr, paramUser).Compile();
        }

        public static Expression<Func<T, bool>> ToExpression<T>(Rule r, bool useTryCatchForNulls = true)
        {
            var paramUser = Expression.Parameter(typeof(T));
            var expr = GetExpressionForRule(typeof(T), r, paramUser, useTryCatchForNulls);

            return Expression.Lambda<Func<T, bool>>(expr, paramUser);
        }

        public static Func<T, bool> ToFunc<T>(Rule r, bool useTryCatchForNulls = true)
        {
            return ToExpression<T>(r, useTryCatchForNulls).Compile();
        }

        public static Expression<Func<object, bool>> ToExpression(Type type, Rule r)
        {
            var paramUser = Expression.Parameter(typeof(object));
            var expr = GetExpressionForRule(type, r, paramUser);

            return Expression.Lambda<Func<object, bool>>(expr, paramUser);
        }

        public static Func<object, bool> ToFunc(Type type, Rule r)
        {
            return ToExpression(type, r).Compile();
        }

        public Func<object, bool> CompileRule(Type type, Rule r)
        {
            var paramUser = Expression.Parameter(typeof(object));
            var expr = GetExpressionForRule(type, r, paramUser);

            return Expression.Lambda<Func<object, bool>>(expr, paramUser).Compile();
        }

        public Func<T, bool> CompileRules<T>(IEnumerable<Rule> rules)
        {
            var paramUser = Expression.Parameter(typeof(T));
            var expr = BuildNestedExpression(typeof(T), rules, paramUser, ExpressionType.And);
            return Expression.Lambda<Func<T, bool>>(expr, paramUser).Compile();
        }

        public Func<object, bool> CompileRules(Type type, IEnumerable<Rule> rules)
        {
            var paramUser = Expression.Parameter(type);
            var expr = BuildNestedExpression(type, rules, paramUser, ExpressionType.And);
            return Expression.Lambda<Func<object, bool>>(expr, paramUser).Compile();
        }

        // Build() in some forks
        protected static Expression GetExpressionForRule(Type type, Rule rule, ParameterExpression parameterExpression,
            bool useTryCatchForNulls = true)
        {
            ExpressionType nestedOperator;
            if (Enum.TryParse(rule.Operator, out nestedOperator) &&
                NestedOperators.Contains(nestedOperator) && rule.Rules != null && rule.Rules.Any())
                return BuildNestedExpression(type, rule.Rules, parameterExpression, nestedOperator,
                    useTryCatchForNulls);
            return BuildExpr(type, rule, parameterExpression, useTryCatchForNulls);
        }

        protected static Expression BuildNestedExpression(Type type, IEnumerable<Rule> rules, ParameterExpression param,
            ExpressionType operation, bool useTryCatchForNulls = true)
        {
            var expressions = rules.Select(r => GetExpressionForRule(type, r, param, useTryCatchForNulls));
            return BinaryExpression(expressions, operation);
        }

        protected static Expression BinaryExpression(IEnumerable<Expression> expressions, ExpressionType operationType)
        {
            Func<Expression, Expression, Expression> methodExp;
            switch (operationType)
            {
                case ExpressionType.Or:
                    methodExp = Expression.Or;
                    break;
                case ExpressionType.OrElse:
                    methodExp = Expression.OrElse;
                    break;
                case ExpressionType.AndAlso:
                    methodExp = Expression.AndAlso;
                    break;
                default:
                case ExpressionType.And:
                    methodExp = Expression.And;
                    break;
            }

            return expressions.Aggregate(methodExp);
        }

        private static Expression GetProperty(Expression param, string propname)
        {
            var propExpression = param;
            var childProperties = propname.Split('.');
            var propertyType = param.Type;

            foreach (var childProprop in childProperties)
            {
                var isIndexed = RegexIndexed.Match(childProprop);
                if (isIndexed.Success)
                {
                    var collectionName = isIndexed.Groups[1].Value;
                    var index = int.Parse(isIndexed.Groups[2].Value);
                    var collectionProp = propertyType.GetProperty(collectionName);
                    if (collectionProp == null)
                        throw new RulesException(
                            $"Cannot find collection property {collectionName} in class {propertyType.Name} (\"{propname}\")");
                    var collExpr = Expression.PropertyOrField(propExpression, collectionName);

                    var collectionType = collExpr.Type;
                    if (collectionType.IsArray)
                    {
                        propExpression = Expression.ArrayAccess(collExpr, Expression.Constant(index));
                        propertyType = propExpression.Type;
                    }
                    else
                    {
                        var getter = collectionType.GetMethod("get_Item", new[] { typeof(int) });
                        if (getter == null)
                            throw new RulesException($"'{collectionName} ({collectionType.Name}) cannot be indexed");
                        propExpression = Expression.Call(collExpr, getter, Expression.Constant(index));
                        propertyType = getter.ReturnType;
                    }
                }
                else
                {
                    var property = propertyType.GetProperty(childProprop);
                    if (property == null)
                        throw new RulesException(
                            $"Cannot find property {childProprop} in class {propertyType.Name} (\"{propname}\")");
                    propExpression = Expression.PropertyOrField(propExpression, childProprop);
                    propertyType = property.PropertyType;
                }
            }

            return propExpression;
        }


        private static MethodInfo IsEnumerableOperator(string oprator)
        {
            return (from tup in EnumrMethodsByName
                    where string.Equals(oprator, tup.Item1, StringComparison.CurrentCultureIgnoreCase)
                    select tup.Item2.Value).FirstOrDefault();
        }

        private static Expression BuildExpr(Type type, Rule rule, Expression param, bool useTryCatch = true)
        {
            Expression propExpression;
            Type propType;

            if (param.Type == typeof(object)) param = Expression.TypeAs(param, type);
            var drule = rule as DataRule;

            if (string.IsNullOrEmpty(rule.MemberName)) //check is against the object itself
            {
                propExpression = param;
                propType = propExpression.Type;
            }
            else if (drule != null)
            {
                if (type != typeof(DataRow))
                    throw new RulesException(" Bad rule");
                propExpression = GetDataRowField(param, drule.MemberName, drule.Type);
                propType = propExpression.Type;
            }
            else
            {
                propExpression = GetProperty(param, rule.MemberName);
                propType = propExpression.Type;
            }

            if (useTryCatch)
                propExpression = Expression.TryCatch(
                    Expression.Block(propExpression.Type, propExpression),
                    Expression.Catch(typeof(NullReferenceException), Expression.Default(propExpression.Type))
                );

            // is the operator a known .NET operator?
            ExpressionType tBinary;

            if (Enum.TryParse(rule.Operator, out tBinary))
            {
                Expression right;
                var txt = rule.TargetValue as string;
                if (txt != null && txt.StartsWith("*."))
                {
                    txt = txt.Substring(2);
                    right = GetProperty(param, txt);
                }
                else
                {
                    right = StringToExpression(rule.TargetValue, propType);
                }

                return Expression.MakeBinary(tBinary, propExpression, right);
            }

            switch (rule.Operator)
            {
                case "IsMatch":
                    return Expression.Call(
                        MiRegexIsMatch.Value,
                        propExpression,
                        Expression.Constant(rule.TargetValue, typeof(string)),
                        Expression.Constant(RegexOptions.IgnoreCase, typeof(RegexOptions))
                    );
                case "IsInteger":
                    return Expression.Call(
                        MiIntTryParse.Value,
                        propExpression,
                        Expression.MakeMemberAccess(null, typeof(Placeholder).GetField("Int"))
                    );
                case "IsSingle":
                    return Expression.Call(
                        MiFloatTryParse.Value,
                        propExpression,
                        Expression.MakeMemberAccess(null, typeof(Placeholder).GetField("Float"))
                    );
                case "IsDouble":
                    return Expression.Call(
                        MiDoubleTryParse.Value,
                        propExpression,
                        Expression.MakeMemberAccess(null, typeof(Placeholder).GetField("Double"))
                    );
                case "IsDecimal":
                    return Expression.Call(
                        MiDecimalTryParse.Value,
                        propExpression,
                        Expression.MakeMemberAccess(null, typeof(Placeholder).GetField("Decimal"))
                    );
            }

            var enumrOperation = IsEnumerableOperator(rule.Operator);
            if (enumrOperation != null)
            {
                var elementType = ElementType(propType);
                var lambdaParam = Expression.Parameter(elementType, "lambdaParam");
                return rule.Rules?.Any() == true
                    ? Expression.Call(enumrOperation.MakeGenericMethod(elementType),
                        propExpression,
                        Expression.Lambda(
                            BuildNestedExpression(elementType, rule.Rules, lambdaParam, ExpressionType.AndAlso),
                            lambdaParam)
                    )
                    : Expression.Call(enumrOperation.MakeGenericMethod(elementType), propExpression);
            }

            var inputs = rule.Inputs.Select(x => x.GetType()).ToArray();
            var methodInfo = propType.GetMethod(rule.Operator, inputs);
            var expressions = new List<Expression>();

            if (methodInfo == null)
            {
                methodInfo = propType.GetMethod(rule.Operator);
                if (methodInfo != null)
                {
                    var parameters = methodInfo.GetParameters();
                    var i = 0;
                    foreach (var item in rule.Inputs)
                    {
                        expressions.Add(StringToExpression(item, parameters[i].ParameterType));
                        i++;
                    }
                }
            }
            else
            {
                expressions.AddRange(rule.Inputs.Select(Expression.Constant));
            }

            if (methodInfo == null)
                throw new RulesException($"'{rule.Operator}' is not a method of '{propType.Name}");


            if (!methodInfo.IsGenericMethod)
                inputs = null; //Only pass in type information to a Generic Method
            var callExpression = Expression.Call(propExpression, rule.Operator, inputs, expressions.ToArray());
            if (useTryCatch)
                return Expression.TryCatch(
                    Expression.Block(typeof(bool), callExpression),
                    Expression.Catch(typeof(NullReferenceException), Expression.Constant(false))
                );
            return callExpression;
        }

        private static MethodInfo GetLinqMethod(string name, int numParameter)
        {
            return typeof(Enumerable).GetMethods(BindingFlags.Static | BindingFlags.Public)
                .FirstOrDefault(m => m.Name == name && m.GetParameters().Length == numParameter);
        }


        private static Expression GetDataRowField(Expression prm, string member, string typeName)
        {
            var expMember = Expression.Call(prm, MiGetItem.Value, Expression.Constant(member, typeof(string)));
            var type = Type.GetType(typeName);
            Debug.Assert(type != null);

            if (type.IsClass || typeName.StartsWith("System.Nullable"))
                //  equals "return  testValue == DBNull.Value  ? (typeName) null : (typeName) testValue"
                return Expression.Condition(Expression.Equal(expMember, Expression.Constant(DBNull.Value)),
                    Expression.Constant(null, type),
                    Expression.Convert(expMember, type));
            return Expression.Convert(expMember, type);
        }

        private static Expression StringToExpression(object value, Type propType)
        {
            Debug.Assert(propType != null);

            object safeValue;
            var valueType = propType;
            var txt = value as string;
            if (value == null)
            {
                safeValue = null;
            }
            else if (txt != null)
            {
                if (txt.ToLower() == "null")
                {
                    safeValue = null;
                }
                else if (propType.IsEnum)
                {
                    safeValue = Enum.Parse(propType, txt);
                }
                else if (propType.Name == "Nullable`1")
                {
                    valueType = Nullable.GetUnderlyingType(propType);
                    safeValue = Convert.ChangeType(value, valueType);
                }
                else
                {
                    safeValue = Convert.ChangeType(value, valueType);
                }
            }
            else if (propType.Name == "Nullable`1")
            {
                valueType = Nullable.GetUnderlyingType(propType);
                safeValue = Convert.ChangeType(value, valueType);
            }
            else
            {
                safeValue = Convert.ChangeType(value, valueType);
            }

            return Expression.Constant(safeValue, propType);
        }

        private static Type ElementType(Type seqType)
        {
            var iEnum = FindIEnumerable(seqType);
            return iEnum == null ? seqType : iEnum.GetGenericArguments()[0];
        }

        private static Type FindIEnumerable(Type seqType)
        {
            if (seqType == null || seqType == typeof(string))
                return null;
            if (seqType.IsArray)
                return typeof(IEnumerable<>).MakeGenericType(seqType.GetElementType());
            if (seqType.IsGenericType)
                foreach (var arg in seqType.GetGenericArguments())
                {
                    var ienum = typeof(IEnumerable<>).MakeGenericType(arg);
                    if (ienum.IsAssignableFrom(seqType)) return ienum;
                }

            var iFaces = seqType.GetInterfaces();
            foreach (var iFace in iFaces)
            {
                var iEnum = FindIEnumerable(iFace);
                if (iEnum != null)
                    return iEnum;
            }

            if (seqType.BaseType != null && seqType.BaseType != typeof(object))
                return FindIEnumerable(seqType.BaseType);

            return null;
        }

        public class Operator
        {
            public string Name { get; set; }
            public OperatorType Type { get; set; }
            public int NumberOfInputs { get; set; }
            public bool SimpleInputs { get; set; }
        }

        public class Member
        {
            public static BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public;

            private static readonly string[] LogicOperators =
            {
                MreOperator.And.ToString("g"),
                MreOperator.AndAlso.ToString("g"),
                MreOperator.Or.ToString("g"),
                MreOperator.OrElse.ToString("g")
            };

            private static readonly string[] ComparisonOperators =
            {
                MreOperator.Equal.ToString("g"),
                MreOperator.GreaterThan.ToString("g"),
                MreOperator.GreaterThanOrEqual.ToString("g"),
                MreOperator.LessThan.ToString("g"),
                MreOperator.LessThanOrEqual.ToString("g"),
                MreOperator.NotEqual.ToString("g")
            };

            private static readonly string[] HardCodedStringOperators =
            {
                MreOperator.IsMatch.ToString("g"),
                MreOperator.IsInteger.ToString("g"),
                MreOperator.IsSingle.ToString("g"),
                MreOperator.IsDouble.ToString("g"),
                MreOperator.IsDecimal.ToString("g")
            };

            public string Name { get; set; }
            public string Type { get; set; }
            public List<Operator> PossibleOperators { get; set; }

            public static bool IsSimpleType(Type type)
            {
                return
                    type.IsPrimitive ||
                    new[]
                    {
                        typeof(Enum),
                        typeof(string),
                        typeof(decimal),
                        typeof(DateTime),
                        typeof(DateTimeOffset),
                        typeof(TimeSpan),
                        typeof(Guid)
                    }.Contains(type) ||
                    Convert.GetTypeCode(type) != TypeCode.Object ||
                    type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                    IsSimpleType(type.GetGenericArguments()[0])
                    ;
            }

            public static List<Member> GetFields(Type type, string memberName = null, string parentPath = null)
            {
                var toReturn = new List<Member>();
                var fi = new Member();
                fi.Name = string.IsNullOrEmpty(parentPath) ? memberName : $"{parentPath}.{memberName}";
                fi.Type = type.ToString();
                fi.PossibleOperators = Operators(type, string.IsNullOrEmpty(fi.Name));
                toReturn.Add(fi);
                if (IsSimpleType(type))
                    return toReturn;
                var fields = type.GetFields(Flags);
                var properties = type.GetProperties(Flags);
                foreach (var field in fields)
                {
                    var name = ValidateName(field.Name, type, memberName, fi.Name, parentPath,
                        out var useParentName);
                    toReturn.AddRange(GetFields(field.FieldType, name, useParentName));
                }

                foreach (var prop in properties)
                {
                    var name = ValidateName(prop.Name, type, memberName, fi.Name, parentPath,
                        out var useParentName);
                    toReturn.AddRange(GetFields(prop.PropertyType, name, useParentName));
                }

                return toReturn;
            }

            private static string ValidateName(string name, Type parentType, string parentName, string parentPath,
                string grandparentPath, out string useAsParentPath)
            {
                if (name == "Item" && IsGenericList(parentType))
                {
                    useAsParentPath = grandparentPath;
                    return parentName + "[0]";
                }

                useAsParentPath = parentPath;
                return name;
            }

            public static bool IsGenericList(Type type)
            {
                if (type == null) throw new ArgumentNullException(nameof(type));
                return type.GetInterfaces().Where(@interface => @interface.IsGenericType).Any(@interface => @interface.GetGenericTypeDefinition() == typeof(ICollection<>));
            }

            public static List<Operator> Operators(Type type, bool addLogicOperators = false, bool noOverloads = true)
            {
                var operators = new List<Operator>();
                if (addLogicOperators)
                    operators.AddRange(LogicOperators.Select(x => new Operator { Name = x, Type = OperatorType.Logic }));

                if (type == typeof(string))
                    operators.AddRange(HardCodedStringOperators.Select(x => new Operator
                    { Name = x, Type = OperatorType.InternalString }));
                else if (IsSimpleType(type))
                    operators.AddRange(ComparisonOperators.Select(x => new Operator
                    { Name = x, Type = OperatorType.Comparison }));
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                foreach (var method in methods)
                    if (method.ReturnType == typeof(bool) && !method.Name.StartsWith("get_") &&
                        !method.Name.StartsWith("set_") && !method.Name.StartsWith("_op"))
                    {
                        var paramaters = method.GetParameters();
                        var op = new Operator
                        {
                            Name = method.Name,
                            Type = OperatorType.ObjectMethod,
                            NumberOfInputs = paramaters.Length,
                            SimpleInputs = paramaters.All(x => IsSimpleType(x.ParameterType))
                        };
                        if (noOverloads)
                        {
                            var existing = operators.FirstOrDefault(x => x.Name == op.Name && x.Type == op.Type);
                            if (existing == null)
                                operators.Add(op);
                            else if (existing.NumberOfInputs > op.NumberOfInputs)
                                operators[operators.IndexOf(existing)] = op;
                        }
                        else
                        {
                            operators.Add(op);
                        }
                    }

                return operators;
            }
        }
    }
}
