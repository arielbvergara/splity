# Splity: A Serverless Expense Sharing Application with Receipt Analysis

Splity is a serverless expense sharing application built with .NET 8 and AWS Lambda that automates receipt processing through Azure Document Intelligence. It uses a microservices architecture with separate Lambda functions for different operations and PostgreSQL for data persistence.

The application provides comprehensive expense management features including party creation, expense tracking, and automated receipt data extraction through intelligent OCR capabilities. Built on AWS services for scalability and Azure's Document Intelligence for advanced receipt analysis.

## Architecture Overview

### Core Components

- **Lambda Functions**: Independent microservices for each operation (Party management, Expense management, User management)
- **Shared Libraries**: Common functionality across all Lambda functions
  - `Splity.Shared.Database`: Data models, repositories, and PostgreSQL connection handling
  - `Splity.Shared.AI`: Azure Document Intelligence service integration  
  - `Splity.Shared.Storage`: S3 bucket operations for receipt image storage
  - `Splity.Shared.Common`: API Gateway helpers and common utilities

### Data Flow

1. Client uploads receipt through API Gateway
2. Lambda function stores image in S3 bucket
3. Azure Document Intelligence extracts receipt data
4. Processed data stored in PostgreSQL database
5. Party and expense management through dedicated Lambda functions

```ascii
[Client] -> [API Gateway] -> [Lambda Functions]
                                    |
                                    v
[S3 Storage] <- [Receipt Upload] -> [Azure Document Intelligence]
                                    |
                                    v
                              [PostgreSQL DB]
```

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
├── splity-infrastructure-cf-template.yaml # Complete CloudFormation template for all entities
├── api-gateway-cf-template.yaml          # Legacy API Gateway template (reference)
├── create-party-lambda-function-cf-template.yaml # Legacy individual Lambda templates (reference)
└── delete-party-lambda-function-cf-template.yaml # Legacy individual Lambda templates (reference)
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

#### Deploy Complete Infrastructure
The application uses a comprehensive CloudFormation template for infrastructure as code. Deploy the entire Splity infrastructure:

```bash
# Deploy complete Splity infrastructure stack
aws cloudformation deploy \
  --template-file splity-infrastructure-cf-template.yaml \
  --stack-name splity-complete-infrastructure-dev \
  --parameter-overrides Environment=dev \
  --capabilities CAPABILITY_NAMED_IAM \
  --region eu-west-2
```

#### Deploy All Lambda Code
After infrastructure is deployed, update all Lambda functions with actual .NET code:

```bash
# Party Functions
cd Splity.Party.Create/src/Splity.Party.Create && dotnet lambda deploy-function --function-name SplityCreateParty-dev --region eu-west-2
cd ../../../Splity.Party.Get/src/Splity.Party.Get && dotnet lambda deploy-function --function-name SplityGetParty-dev --region eu-west-2
cd ../../../Splity.Party.Update/src/Splity.Party.Update && dotnet lambda deploy-function --function-name SplityUpdateParty-dev --region eu-west-2
cd ../../../Splity.Party.Delete/src/Splity.Party.Delete && dotnet lambda deploy-function --function-name SplityDeleteParty-dev --region eu-west-2

# Expense Functions
cd ../../../Splity.Expenses.Create/src/Splity.Expenses.Create && dotnet lambda deploy-function --function-name SplityCreateExpenses-dev --region eu-west-2
cd ../../../Splity.Expenses.Delete/src/Splity.Expenses.Delete && dotnet lambda deploy-function --function-name SplityDeleteExpenses-dev --region eu-west-2
cd ../../../Splity.Expenses.Extract/src/Splity.Expenses.Extract && dotnet lambda deploy-function --function-name SplityExtractExpenses-dev --region eu-west-2

# User Functions
cd ../../../Splity.User.Create/src/Splity.User.Create && dotnet lambda deploy-function --function-name SplityCreateUser-dev --region eu-west-2
cd ../../../Splity.User.Get/src/Splity.User.Get && dotnet lambda deploy-function --function-name SplityGetUser-dev --region eu-west-2
cd ../../../Splity.User.Update/src/Splity.User.Update && dotnet lambda deploy-function --function-name SplityUpdateUser-dev --region eu-west-2
```

#### Complete API Endpoints
After deployment, the following comprehensive RESTful API is available:

**Party Operations:**
- `POST /party` - Create a new party
- `GET /party/{id}` - Get party by ID
- `PUT /party/{id}` - Update party by ID
- `DELETE /party/{id}` - Delete party by ID

**Expense Operations:**
- `POST /expenses` - Create expenses for a party
- `DELETE /expenses` - Delete expenses (bulk operation)
- `PUT /party/{partyId}/extract` - Upload receipt and extract expense data

**User Operations:**
- `POST /users` - Create a new user
- `GET /users/{id}` - Get user by ID
- `PUT /users/{id}` - Update user by ID

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

## TODO: Missing Backend Features

The following features are missing from the current backend implementation and need to be added:

### Party Management
- [ ] `GET /parties` - List all parties for a user
- [ ] `POST /party/{partyId}/invite` - Invite members to party
- [ ] `DELETE /party/{partyId}/members/{userId}` - Remove member from party
- [ ] `GET /party/{partyId}/members` - List party members
- [ ] Party join functionality via invite codes

### Expense Management
- [ ] `GET /party/{partyId}/expenses` - Get expenses for a party
- [ ] `PUT /expenses/{expenseId}` - Update individual expense
- [ ] `GET /expenses/{expenseId}` - Get individual expense details
- [ ] Expense splitting algorithms (equal, percentage, custom amounts)
- [ ] Expense categories and tagging

### Settlement & Payments
- [ ] `GET /party/{partyId}/settlements` - Calculate who owes whom
- [ ] `POST /party/{partyId}/settle` - Mark settlements as paid
- [ ] Payment tracking and history
- [ ] Settlement optimization algorithms

### User & Authentication
- [ ] User authentication and session management
- [ ] `GET /users/{userId}/parties` - Get user's parties
- [ ] `GET /users/{userId}/expenses` - Get user's expenses
- [ ] User profile management

### File & Receipt Management
- [ ] Receipt image storage and retrieval
- [ ] Receipt data validation and editing
- [ ] Multiple receipt formats support
- [ ] Receipt OCR confidence scoring

### Analytics & Reporting
- [ ] Spending analytics by category
- [ ] Monthly/yearly expense reports
- [ ] Party expense summaries
- [ ] Export functionality (CSV, PDF)

### Notifications & Real-time
- [ ] Email notifications for new expenses
- [ ] Real-time updates via WebSocket/SSE
- [ ] Push notifications for mobile
- [ ] Expense reminder system

### Data Validation & Business Logic
- [ ] Expense amount validation
- [ ] Party member limits
- [ ] Receipt duplicate detection
- [ ] Data consistency checks

## Infrastructure

### CloudFormation Templates
- `splity-infrastructure-cf-template.yaml`: Complete application infrastructure (recommended)
- Legacy templates (for reference):
  - `api-gateway-cf-template.yaml`: API Gateway configuration
  - Individual Lambda function templates

### Lambda Functions
**Party Management:**
- SplityCreateParty-{env}: Creates new parties
- SplityGetParty-{env}: Retrieves party information  
- SplityUpdateParty-{env}: Updates existing parties
- SplityDeleteParty-{env}: Deletes parties and related data

**Expense Management:**
- SplityCreateExpenses-{env}: Creates new expenses for parties
- SplityDeleteExpenses-{env}: Bulk deletes expenses by IDs
- SplityExtractExpenses-{env}: Processes receipt images via Azure Document Intelligence

**User Management:**
- SplityCreateUser-{env}: Creates new users
- SplityGetUser-{env}: Retrieves user information with details
- SplityUpdateUser-{env}: Updates existing user information

**Total: 10 Lambda functions** deployed with comprehensive CRUD operations

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
