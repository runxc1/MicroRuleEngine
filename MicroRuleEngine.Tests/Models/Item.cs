namespace MicroRuleEngine.Tests.Models
{
    public class Item :
        IVisitable<Item>
    {
        public decimal Cost { get; set; }
        public string ItemCode { get; set; }

        public void Accept(IVisitor<Item> visitor)
        {
            visitor.Visit(this);
        }

        public static Item Make(string itemCode, decimal cost)
        {
            return new Item { ItemCode = itemCode, Cost = cost };
        }
    }
}