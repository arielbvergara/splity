-- Splity Database Schema for Aurora DSQL
-- Aurora DSQL does not support FOREIGN KEY constraints.
-- Relationships are enforced at the application level.

-- Create Users table
CREATE TABLE IF NOT EXISTS Users
(
    UserId UUID PRIMARY KEY,
    Name VARCHAR(255) NOT NULL,
    Email VARCHAR(255) NOT NULL UNIQUE,
    CognitoUserId VARCHAR(255),
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
    );

-- Create Parties table
CREATE TABLE IF NOT EXISTS Parties
(
    PartyId UUID PRIMARY KEY,
    OwnerId UUID NOT NULL,
    Name VARCHAR(255) NOT NULL,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
    );

-- Create PartyContributors junction table
CREATE TABLE IF NOT EXISTS PartyContributors
(
    PartyId UUID NOT NULL,
    UserId  UUID NOT NULL,
    PRIMARY KEY (PartyId, UserId)
    );

-- Create Expenses table
CREATE TABLE IF NOT EXISTS Expenses
(
    ExpenseId UUID PRIMARY KEY,
    PartyId UUID NOT NULL,
    PayerId UUID NOT NULL,
    Description VARCHAR(500) NOT NULL,
    Amount DECIMAL(10, 2) NOT NULL,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
    );

-- Create ExpenseParticipants junction table
CREATE TABLE IF NOT EXISTS ExpenseParticipants
(
    ExpenseId UUID NOT NULL,
    UserId    UUID NOT NULL,
    Quantity  INT,
    PRIMARY KEY (ExpenseId, UserId)
    );

-- Create PartyBillsImages table
CREATE TABLE IF NOT EXISTS PartyBillsImages
(
    BillId UUID PRIMARY KEY,
    BillFileTitle VARCHAR(255) NOT NULL,
    PartyId UUID NOT NULL,
    ImageURL VARCHAR(500) NOT NULL
    );

-- Add indexes to simulate relational lookups (Aurora DSQL style)
CREATE INDEX ASYNC IF NOT EXISTS idx_parties_owner ON Parties(OwnerId);
CREATE INDEX ASYNC IF NOT EXISTS idx_partycontributors_party ON PartyContributors(PartyId);
CREATE INDEX ASYNC IF NOT EXISTS idx_partycontributors_user ON PartyContributors(UserId);
CREATE INDEX ASYNC IF NOT EXISTS idx_expenses_party ON Expenses(PartyId);
CREATE INDEX ASYNC IF NOT EXISTS idx_expenses_payer ON Expenses(PayerId);
CREATE INDEX ASYNC IF NOT EXISTS idx_expenseparticipants_expense ON ExpenseParticipants(ExpenseId);
CREATE INDEX ASYNC IF NOT EXISTS idx_expenseparticipants_user ON ExpenseParticipants(UserId);
CREATE INDEX ASYNC IF NOT EXISTS idx_partybillsimages_party ON PartyBillsImages(PartyId);
CREATE INDEX ASYNC IF NOT EXISTS idx_users_email ON Users(Email);
CREATE INDEX ASYNC IF NOT EXISTS idx_users_cognito ON Users(CognitoUserId);
