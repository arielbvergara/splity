# Splity: A Serverless Expense Sharing Application with Receipt Analysis

Splity is a modern serverless application that simplifies expense sharing and management through automated receipt processing and intelligent expense tracking. Built on AWS Lambda with .NET 8, it provides a robust platform for creating and managing shared expenses with automated receipt analysis capabilities.

The application leverages Azure's Document Intelligence for receipt analysis and AWS services for serverless computing and storage. It provides comprehensive expense management features including party creation, expense tracking, and automated receipt data extraction. The system uses PostgreSQL for data persistence and implements a microservices architecture through AWS Lambda functions.

## Repository Structure
```
.
├── Splity.Expenses.*/                    # Lambda functions for expense management
│   ├── Create/                           # Creates new expenses
│   ├── Delete/                           # Deletes existing expenses
│   ├── Extract/                          # Processes and analyzes receipt images
│   └── Get/                              # Retrieves expense information
├── Splity.Party.*/                      # Lambda functions for party management
│   ├── Create/                           # Creates new parties
│   ├── Delete/                           # Deletes existing parties
│   ├── Get/                              # Retrieves party information
│   └── Update/                           # Updates existing parties
├── Splity.User.*/                       # Lambda functions for user management
│   ├── Create/                           # Creates new users
│   ├── Get/                              # Retrieves user information
│   └── Update/                           # Updates existing users
├── Splity.Shared.*/                     # Shared libraries and utilities
│   ├── AI/                               # Document intelligence services
│   ├── Common/                           # Common utilities and helpers
│   ├── Database/                         # Database models and repositories
│   └── Storage/                          # S3 storage services
├── party-infrastructure-cf-template.yaml # CloudFormation template for Party entity
├── api-gateway-cf-template.yaml          # API Gateway CloudFormation template
├── create-party-lambda-function-cf-template.yaml # Individual Lambda templates
└── delete-party-lambda-function-cf-template.yaml # Individual Lambda templates
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

### Infrastructure Deployment

#### Deploy Party Infrastructure
The application uses CloudFormation templates for infrastructure as code. Deploy the Party entity infrastructure:

```bash
# Deploy Party infrastructure stack
aws cloudformation deploy \
  --template-file party-infrastructure-cf-template.yaml \
  --stack-name splity-party-infrastructure-dev \
  --parameter-overrides Environment=dev \
  --capabilities CAPABILITY_NAMED_IAM \
  --region eu-west-2
```

#### Deploy Lambda Code
After infrastructure is deployed, update Lambda functions with actual code:

```bash
# Deploy each Party Lambda function
cd Splity.Party.Create/src/Splity.Party.Create
dotnet lambda deploy-function --function-name SplityCreateParty-dev

cd ../../../Splity.Party.Get/src/Splity.Party.Get
dotnet lambda deploy-function --function-name SplityGetParty-dev

cd ../../../Splity.Party.Update/src/Splity.Party.Update
dotnet lambda deploy-function --function-name SplityUpdateParty-dev

cd ../../../Splity.Party.Delete/src/Splity.Party.Delete
dotnet lambda deploy-function --function-name SplityDeleteParty-dev
```

#### API Endpoints
After deployment, the following RESTful endpoints are available:
- `POST /party` - Create a new party
- `GET /party/{id}` - Get party by ID
- `PUT /party/{id}` - Update party by ID
- `DELETE /party/{id}` - Delete party by ID

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

### CloudFormation Templates
- `party-infrastructure-cf-template.yaml`: Complete Party entity infrastructure
- `api-gateway-cf-template.yaml`: API Gateway configuration
- Individual Lambda function templates for reference

### Lambda Functions
**Party Management:**
- SplityCreateParty-{env}: Creates new parties
- SplityGetParty-{env}: Retrieves party information
- SplityUpdateParty-{env}: Updates existing parties
- SplityDeleteParty-{env}: Deletes parties and related data

**Expense Management:**
- SplityCreateExpenses: Creates new expenses
- SplityExtractExpenses: Processes and analyzes receipt images
- SplityDeleteExpenses: Deletes existing expenses
- SplityGetExpenses: Retrieves expense information

**User Management:**
- SplityCreateUser: Creates new users
- SplityUpdateUser: Updates user information

### API Gateway
- **HTTP API** with CORS support
- **Environment-specific stages** (dev, staging, prod)
- **RESTful routing** for all entities
- **Lambda proxy integration** with payload format 2.0

### Storage & Database
- **S3 Bucket**: split-app-v1 (eu-central-1) - Receipt image storage
- **PostgreSQL Database**: AWS DSQL cluster for data persistence
- **KMS**: Encryption for database connections

### Security & Permissions
- **IAM Roles**: Environment-specific Lambda execution roles
- **DSQL Permissions**: Full database access with encryption
- **API Gateway Permissions**: Lambda invocation permissions
- **Resource Tagging**: Environment and service identification

### External Services
- **Azure Document Intelligence API**: Receipt OCR and data extraction
