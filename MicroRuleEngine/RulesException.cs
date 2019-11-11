using System;

namespace MicroRuleEngine
{
    public class RulesException : ApplicationException
    {
        public RulesException()
        {
        }

        public RulesException(string message) : base(message)
        {
        }

        public RulesException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}