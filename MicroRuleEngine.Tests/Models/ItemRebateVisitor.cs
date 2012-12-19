using System;

namespace MicroRuleEngine.Tests.Models
{
    public class ItemRebateVisitor
        : IVisitor<Item>
    {
        private readonly Func<Item, bool> _rule;
        private decimal _rebate;

        public ItemRebateVisitor(Func<Item, bool> rule)
        {
            _rule = rule;
            _rebate = 0;
        }

        public void Visit(Item element)
        {
            if (_rule(element))
                _rebate = _rebate + (element.Cost * 0.1M);
        }
    }
}