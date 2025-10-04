using System.Globalization;
using Azure;
using Azure.AI.DocumentIntelligence;

namespace Splity.Shared.AI;

public class DocumentIntelligenceService(string documentIntelligenceApiKey, string documentIntelligenceEndpoint)
    : IDocumentIntelligenceService
{
    private readonly AzureKeyCredential _credential = new(documentIntelligenceApiKey);

    public async Task<Receipt> AnalyzeReceipt(string url)
    {
        var receiptResult = new Receipt();
        DocumentIntelligenceClient client = new(new Uri(documentIntelligenceEndpoint), _credential);

        var receiptUri = new Uri(url);

        var operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-receipt", receiptUri);

        var receipts = operation.Value;

        // To see the list of the supported fields returned by service and its corresponding types, consult:
        // https://aka.ms/formrecognizer/receiptfields
        foreach (var receipt in receipts.Documents)
        {
            if (receipt.Fields.TryGetValue("MerchantName", out var merchantNameField))
            {
                if (merchantNameField.FieldType == DocumentFieldType.String)
                {
                    receiptResult.MerchantName = merchantNameField.ValueString;
                }
            }

            if (receipt.Fields.TryGetValue("TransactionDate", out var transactionDateField))
            {
                var transactionDate = transactionDateField.ValueDate;
                if (transactionDateField.FieldType == DocumentFieldType.Date)
                {
                    receiptResult.TransactionDate = transactionDate;
                }
            }

            if (receipt.Fields.TryGetValue("Items", out var itemsField))
            {
                if (itemsField.FieldType == DocumentFieldType.List)
                {
                    foreach (var itemField in itemsField.ValueList)
                    {
                        ReceiptItem item = new();

                        if (itemField.FieldType == DocumentFieldType.Dictionary)
                        {
                            IReadOnlyDictionary<string, DocumentField> itemFields = itemField.ValueDictionary;

                            if (itemFields.TryGetValue("Description", out var itemDescriptionField))
                            {
                                if (itemDescriptionField.FieldType == DocumentFieldType.String)
                                {
                                    var itemDescription = itemDescriptionField.ValueString;

                                    item.Description = itemDescription;
                                }
                            }

                            if (!itemFields.TryGetValue("TotalPrice", out var itemTotalPriceField))
                            {
                                continue;
                            }

                            if (itemTotalPriceField.FieldType != DocumentFieldType.Currency)
                            {
                                continue;
                            }

                            var itemTotalPrice = itemTotalPriceField.ValueCurrency.Amount;
                            if (itemFields.TryGetValue("Quantity", out var itemQuantity))
                            {
                                item.Quantity = int.TryParse((itemQuantity.ValueDouble ?? 1).ToString(CultureInfo.InvariantCulture), out var value) ? value : 1;
                            }

                            item.TotalItemPrice = itemTotalPrice;
                            receiptResult.Items.Add(item);
                        }
                    }
                }
            }
        }

        return receiptResult;
    }
}

public class Receipt
{
    public string? MerchantName { get; set; }
    public DateTimeOffset? TransactionDate { get; set; }
    public List<ReceiptItem> Items { get; set; } = new();
}

public class ReceiptItem
{
    public string? Description { get; set; }
    public double TotalItemPrice { get; set; }
    public double Quantity { get; set; } = 1;
    public double SingleItemPrice => TotalItemPrice / Quantity;
}