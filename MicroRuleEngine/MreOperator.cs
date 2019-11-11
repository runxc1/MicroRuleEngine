namespace MicroRuleEngine
{
    public enum MreOperator
    {
        //
        // Summary:
        //     An addition operation, such as a + b, without overflow checking, for numeric
        //     operands.
        Add = 0,

        //
        // Summary:
        //     A bitwise or logical AND operation, such as (a & b) in C# and (a And b) in Visual
        //     Basic.
        And = 2,

        //
        // Summary:
        //     A conditional AND operation that evaluates the second operand only if the first
        //     operand evaluates to true. It corresponds to (a && b) in C# and (a AndAlso b)
        //     in Visual Basic.
        AndAlso = 3,

        //
        // Summary:
        //     A node that represents an equality comparison, such as (a == b) in C# or (a =
        //     b) in Visual Basic.
        Equal = 13,

        //
        // Summary:
        //     A "greater than" comparison, such as (a > b).
        GreaterThan = 15,

        //
        // Summary:
        //     A "greater than or equal to" comparison, such as (a >= b).
        GreaterThanOrEqual = 16,

        //
        // Summary:
        //     A "less than" comparison, such as (a < b).
        LessThan = 20,

        //
        // Summary:
        //     A "less than or equal to" comparison, such as (a <= b).
        LessThanOrEqual = 21,

        //
        // Summary:
        //     An inequality comparison, such as (a != b) in C# or (a <> b) in Visual Basic.
        NotEqual = 35,

        //
        // Summary:
        //     A bitwise or logical OR operation, such as (a | b) in C# or (a Or b) in Visual
        //     Basic.
        Or = 36,

        //
        // Summary:
        //     A short-circuiting conditional OR operation, such as (a || b) in C# or (a OrElse
        //     b) in Visual Basic.
        OrElse = 37,

        /// <summary>
        ///     Checks that a string value matches a Regex expression
        /// </summary>
        IsMatch = 100,

        /// <summary>
        ///     Checks that a value can be 'TryParsed' to an Int32
        /// </summary>
        IsInteger = 101,

        /// <summary>
        ///     Checks that a value can be 'TryParsed' to a Single
        /// </summary>
        IsSingle = 102,

        /// <summary>
        ///     Checks that a value can be 'TryParsed' to a Double
        /// </summary>
        IsDouble = 103,

        /// <summary>
        ///     Checks that a value can be 'TryParsed' to a Decimal
        /// </summary>
        IsDecimal = 104
    }
}