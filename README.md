# Splity: A Serverless Expense Sharing Application with Receipt Analysis

Splity is a modern serverless application that simplifies expense sharing and management through automated receipt processing and intelligent expense tracking. Built on AWS Lambda with .NET 8, it provides a robust platform for creating and managing shared expenses with automated receipt analysis capabilities.

The application leverages Azure's Document Intelligence for receipt analysis and AWS services for serverless computing and storage. It provides comprehensive expense management features including party creation, expense tracking, and automated receipt data extraction. The system uses PostgreSQL for data persistence and implements a microservices architecture through AWS Lambda functions.

## Repository Structure
```
.
├── Splity.Expenses.*/            # Lambda functions for expense management
│   ├── Create/                   # Creates new expenses
│   ├── Delete/                   # Deletes existing expenses
│   ├── Extract/                  # Processes and analyzes receipt images
│   └── Get/                      # Retrieves expense information
├── Splity.Party.*/              # Lambda functions for party management
│   ├── Create/                   # Creates new parties
│   ├── Delete/                   # Deletes existing parties
│   └── Get/                      # Retrieves party information
├── Splity.Shared.*/             # Shared libraries and utilities
│   ├── AI/                      # Document intelligence services
│   ├── Common/                  # Common utilities and helpers
│   ├── Database/               # Database models and repositories
│   └── Storage/                # S3 storage services
```

## Usage Instructions
### Prerequisites
- .NET 8.0 SDK
- AWS CLI configured with appropriate credentials
- Azure Cognitive Services API key for Document Intelligence
- PostgreSQL database
- AWS account with access to Lambda, S3, and API Gateway services

### Installation

1. Clone the repository:
```bash
git clone <repository-url>
cd splity
```

2. Configure environment variables:
```bash
# AWS Configuration
export AWS_BUCKET_NAME="XXXXXXXXXXXX"
export AWS_BUCKET_REGION="eu-central-1"
export CLUSTER_USERNAME="admin"
export CLUSTER_DATABASE="postgres"

# Azure Configuration
export DOCUMENT_INTELLIGENCE_ENDPOINT="https://di-document-reader-test.cognitiveservices.azure.com/"
export DOCUMENT_INTELLIGENCE_API_KEY="your-api-key"
```

3. Build the solution:
```bash
dotnet restore
dotnet build
```

### Quick Start
1. Create a new party:
```csharp
var createPartyRequest = new CreatePartyRequest 
{
    OwnerId = Guid.NewGuid(),
    Name = "Dinner Party"
};
```

2. Upload and analyze a receipt:
```csharp
// Upload receipt image
var fileContent = File.ReadAllBytes("receipt.jpg");
var uploadResult = await s3BucketService.UploadFileAsync(fileContent, "receipt.jpg", "splity");

// Analyze receipt
var receiptAnalysis = await documentIntelligenceService.AnalyzeReceipt(uploadResult);
```

### More Detailed Examples
1. Creating expenses with participants:
```csharp
var createExpensesRequest = new CreateExpensesRequest
{
    PartyId = partyId,
    PayerId = payerId,
    Expenses = new[]
    {
        new CreateExpenseRequest 
        { 
            Description = "Dinner",
            Amount = 100.00m
        }
    }
};
```

### Troubleshooting
1. Database Connection Issues
- Error: "Unable to connect to PostgreSQL database"
- Solution: Verify environment variables CLUSTER_HOSTNAME, CLUSTER_USERNAME, and CLUSTER_DATABASE
- Check network connectivity and security group settings

2. Receipt Analysis Failures
- Error: "Document Intelligence service unavailable"
- Enable debug logging: Set LOG_LEVEL=Debug
- Verify Azure API key and endpoint configuration
- Check image format and size requirements

## Data Flow
The application processes expenses through a series of microservices that handle different aspects of expense management.

```ascii
[Client] -> [API Gateway] -> [Lambda Functions]
                                    |
                                    v
[S3 Storage] <- [Receipt Upload] -> [Azure Document Intelligence]
                                    |
                                    v
                              [PostgreSQL DB]
```

Key component interactions:
1. Client submits receipt through API Gateway
2. Lambda function uploads image to S3
3. Document Intelligence service analyzes receipt content
4. Extracted data is stored in PostgreSQL database
5. Party and expense data is managed through dedicated Lambda functions
6. S3 maintains persistent storage of receipt images
7. API Gateway provides RESTful interface for all operations

## Infrastructure

### Lambda Functions
- SplityCreateExpenses
- SplityExtractExpenses
- SplityCreateParty
- SplityDeleteParty
- SplityGetParty

### Storage
- S3 Bucket: split-app-v1 (eu-central-1)
- PostgreSQL Database

### External Services
- Azure Document Intelligence API