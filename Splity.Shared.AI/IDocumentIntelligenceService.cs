namespace Splity.Shared.AI;

public interface IDocumentIntelligenceService
{
    Task<Receipt> AnalyzeReceipt(string url);
}