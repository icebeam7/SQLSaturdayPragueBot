using System;

namespace SQLSaturdayPragueBot.Models
{
    public class Product_Model
    {
        public int ProductID { get; set; }
        public string Name { get; set; }
        public string Color { get; set; }
        public decimal ListPrice { get; set; }
        public string Photo { get; set; }
        public string Category { get; set; }
        public string Model { get; set; }

        public byte[] PhotoBytes => Convert.FromBase64String(Photo);
    }
}
