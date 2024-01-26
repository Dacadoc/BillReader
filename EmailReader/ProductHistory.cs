namespace EmailReader
{
    public class ProductHistory
    {
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public List<PriceValue> Prices { get; set; }
    }
    public class PriceValue
    {
        public string Date { get; set; }
        public decimal Price { get; set; }
    }

}
