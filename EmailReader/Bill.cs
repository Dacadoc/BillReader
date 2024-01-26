public class Bill
{
    public string BillDate { get; set; }
    public string BilledTime { get; set; }
    public string BilledStore { get; set; }
    public string StoreAddress { get; set; }
    public string BillNo { get; set; }
    public string NexusNo { get; set; }
    public string CashierID { get; set; }
    public string StoreMobile { get; set; }
    public decimal TransactionAmount { get; set; }
    public List<Product> Products { get; set; }
}

public class Product
{
    public int ProductNumber { get; set; }
    public string ProductCode { get; set; }
    public string ProductName { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}
