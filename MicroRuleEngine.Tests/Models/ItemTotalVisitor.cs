using System;

namespace MicroRuleEngine.Tests.Models
{
    public class ItemTotalVisitor
        : IVisitor<Item>
    {
        private readonly Func<Item, bool> _rule;
        private decimal _total;

        public ItemTotalVisitor(Func<Item, bool> rule)
        {
            _rule = rule;
            _total = 0;
        }

        public void Visit(Item element)
        {
            if (_rule(element))
                _total = _total + element.Cost;
        }
    }
}