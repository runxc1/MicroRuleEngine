using System.Collections.Generic;
using System.Linq;

namespace MicroRuleEngine.Tests.Models
{
    public class Order :
        IVisitable<Order>
    {
        public Order()
        {
            Items = new List<Item>();
        }

        public int OrderId { get; set; }
        public Customer Customer { get; set; }
        public List<Item> Items { get; set; }

        public bool HasItem(string itemCode)
        {
            return Items.Any(x => x.ItemCode == itemCode);
        }

        public void Accept(IVisitor<Order> visitor)
        {
            visitor.Visit(this);
        }
    }
}
