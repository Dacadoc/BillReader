using MailKit.Net.Imap;
using MailKit.Search;
using System.Text.RegularExpressions;
using System.Text.Json;
using EmailReader;
using System.Text;
using System.Configuration;
public class Program
{
    public static void Main(string[] args)
    {
        GenerateProductHistory();
        //ReadEBills();
    }

    private static void ReadEBills()
    {
        using (var client = new ImapClient())
        {
            string email = ConfigurationManager.AppSettings["Email"];
            string password = ConfigurationManager.AppSettings["Password"];
            string imapServer = ConfigurationManager.AppSettings["ImapServer"];
            int imapPort = int.Parse(ConfigurationManager.AppSettings["ImapPort"]);
            string searchEmail = ConfigurationManager.AppSettings["SearchEmail"];
            string emailSubject = ConfigurationManager.AppSettings["EmailSubject"];
            string folderPath = ConfigurationManager.AppSettings["FolderPath"];
            client.Connect(imapServer, imapPort, true);

            // Use your real username and App Password here
            client.Authenticate(email, password);

            var inbox = client.Inbox;
            inbox.Open(MailKit.FolderAccess.ReadOnly);

            // Search for messages where the From field contains a specific email address
            var query = SearchQuery.FromContains(searchEmail);
            var uids = inbox.Search(query);
            var count = 0;
            foreach (var uid in uids)
            {
                var message = inbox.GetMessage(uid);

                // Check if the message subject contains a specific keyword
                if (message.Subject.Contains(emailSubject))
                {
                    var htmlBody = message.HtmlBody;

                    // Remove the HTML tags to get the plain text content
                    var plainTextBody = Regex.Replace(htmlBody, "<.*?>", string.Empty);
                    string[] sections = Regex.Split(plainTextBody, "(?<=Amount)");
                    string[] lineParts = Regex.Split(sections[1], "(?=123456789)");
                    var regexRemoveAdditionalLines = new Regex(@"<tr[^>]*>(.*?)");
                    var lines = regexRemoveAdditionalLines.Replace(lineParts[0], string.Empty);
                    var regexReadLines = new Regex(@"s*(.\d+)s*\s*(.\d+)\s*([^\s][^\n]*[^\s])\s*(.\S*)\s*(.\S*)\s*(.\S*)");
                    var matches = regexReadLines.Matches(lines);
                    var regexInfo = new Regex(@"(.*?)\s*:\s*(.*?)\s{10,}");
                    var matches2 = regexInfo.Matches(sections[0]);

                    var keyValuePairs = new Dictionary<string, string>();
                    /*
                    foreach (Match match in matches2)
                    {
                        // The first group is the key and the second group is the value
                        string key = match.Groups[1].Value.Trim();
                        string value = match.Groups[2].Value.Trim();

                        // Add the key-value pair to the dictionary
                        keyValuePairs[key] = value;
                    }
                    foreach (var pair in keyValuePairs)
                    {
                        Console.WriteLine($"{pair.Key}: {pair.Value}");
                    }
                    // Console.WriteLine(parts1[0]);
                    foreach (Match match in matches)
                    {
                        if (match.Groups[2].Value!="123456789"){
                        Console.WriteLine($"Product Number: {match.Groups[1].Value}");
                        Console.WriteLine($"Product Code: {match.Groups[2].Value}");
                        Console.WriteLine($"Product Name: {match.Groups[3].Value}");
                        Console.WriteLine($"Quantity: {match.Groups[4].Value}");
                        Console.WriteLine($"Unit Price: {match.Groups[5].Value}");
                        Console.WriteLine($"Total Price: {match.Groups[6].Value}");

                        }
                    }*/

                    // Create a new Bill object
                    var bill = new Bill();
                    try
                    {
                        // Populate the Bill properties
                        foreach (Match match in matches2)
                        {
                            string key = match.Groups[1].Value.Trim();
                            string value = match.Groups[2].Value.Trim();

                            switch (key)
                            {
                                case "Bill Date":
                                    bill.BillDate = value;
                                    break;
                                case "Billed Time":
                                    bill.BilledTime = value;
                                    break;
                                case "Billed Store":
                                    bill.BilledStore = value;
                                    break;
                                case "Store Address":
                                    bill.StoreAddress = value;
                                    break;
                                case "Bill No":
                                    bill.BillNo = value;
                                    break;
                                case "Nexus No":
                                    bill.NexusNo = value;
                                    break;
                                case "Cashier ID":
                                    bill.CashierID = value;
                                    break;
                                case "Store Mobile":
                                    bill.StoreMobile = value;
                                    break;
                                case "Your bill for this transaction":
                                    bill.TransactionAmount = decimal.Parse(value);
                                    break;
                            }
                        }

                        // Initialize the Products list
                        bill.Products = new List<Product>();

                        // Populate the Products list
                        foreach (Match match in matches)
                        {
                            if (match.Groups[2].Value != "123456789")
                            {
                                var product = new Product
                                {
                                    ProductNumber = int.Parse(match.Groups[1].Value),
                                    ProductCode = match.Groups[2].Value,
                                    ProductName = match.Groups[3].Value,
                                    Quantity = decimal.Parse(match.Groups[4].Value),
                                    UnitPrice = decimal.Parse(match.Groups[5].Value),
                                    TotalPrice = decimal.Parse(match.Groups[6].Value)
                                };

                                bill.Products.Add(product);
                            }
                        }
                        string json = JsonSerializer.Serialize(bill);
                        File.WriteAllText(string.Format(@"{0}{1}.json", folderPath, message.Date.ToString("yyyyMMddHHmmss")), json);
                    }
                    catch (Exception ex)
                    {
                        File.WriteAllText(string.Format(@"{0}{1}.error.txt", folderPath, message.Date.ToString("yyyyMMddHHmmss")), plainTextBody + ex.Message);
                    }
                }
            }

            client.Disconnect(true);
        }
    }
    public static void GenerateProductHistory()
    {

        string folderPath = ConfigurationManager.AppSettings["FolderPath"];
        string[] files = Directory.GetFiles(folderPath, "*.json");
        var csvData = new StringBuilder();

        csvData.AppendLine("BillDate,BilledTime,BilledStore,StoreAddress,BillNo,NexusNo,CashierID,StoreMobile,TransactionAmount,ProductNumber,ProductCode,ProductName,Quantity,UnitPrice,TotalPrice");
               
        List<Bill> bills = new List<Bill>();

        foreach (var file in files)
        {
            string content = File.ReadAllText(file);
            Bill bill = JsonSerializer.Deserialize<Bill>(content);
           if (bill != null && bill.Products != null && bill.Products.Count > 0)
            bills.Add(bill);
        }
        foreach (var bill in bills)
        {
            foreach (var product in bill.Products) 
            {
                // Add a CSV record for each product
                csvData.AppendLine($"{bill.BillDate},{bill.BilledTime},{bill.BilledStore},{bill.StoreAddress.Replace(",","_")},{bill.BillNo},{bill.NexusNo},{bill.CashierID},{bill.StoreMobile},{bill.TransactionAmount},{product.ProductNumber},{product.ProductCode},{product.ProductName.Replace(",","_")},{product.Quantity},{product.UnitPrice},{product.TotalPrice}");
            }
        }
        // Write the CSV data to a file
        File.WriteAllText(string.Format(@"{0}bills.csv",folderPath), csvData.ToString());

        // Extract all distinct products and their unit prices against dates
        var products = bills
            .SelectMany(b => b.Products)
            .GroupBy(p => p.ProductName)
            .Select(g => new
            {
                 ProductName = g.Key,
                ProductCode = g.First().ProductCode,
                Prices = g.Select(p => new PriceValue{ Date = bills.First(b => b.Products.Contains(p)).BillDate, Price = p.UnitPrice }).Distinct()
            });

        foreach (var product in products)
        {
            var productHistory = new ProductHistory
            {
                ProductCode = product.ProductCode,
                ProductName = product.ProductName,
                Prices = product.Prices.ToList()
            };
            var fileName = product.ProductName.Replace("/", "").Replace("\"", "");
            string json = JsonSerializer.Serialize(productHistory);
           // File.WriteAllText($@"C:\Emails\{fileName}_{product.Prices.Count()}.prodct.json", json);
        }

    }
}

