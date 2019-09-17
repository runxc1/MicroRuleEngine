using System;
using System.Collections.Generic;
using System.Linq;

namespace MicroRuleEngine.Tests.Models
{
    public enum Status
    {
        Open,
        Cancelled,
        Completed
    };

    public class Order
    {
        public Order()
        {
            Items = new List<Item>();
        }
        public int OrderId { get; set; }
        public Customer Customer { get; set; }
        public List<Item> Items { get; set; }
        public  decimal? Total { get; set; }
        public DateTime OrderDate { get; set; }
        public bool HasItem(string itemCode)
        {
            return Items.Any(x => x.ItemCode == itemCode);
        }

        public Status Status { get; set; }
        public List<int> Codes { get; set; }
    }

    public class Item
    {
        public decimal Cost { get; set; }
        public string ItemCode { get; set; }
    }

    public class Customer
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Country Country { get; set; }
    }

    public class Country
    {
        public string CountryCode { get; set; }
    }
}
