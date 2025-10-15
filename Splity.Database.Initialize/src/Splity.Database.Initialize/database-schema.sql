-- Splity Database Schema for Aurora DSQL
-- This script creates the tables needed for the Splity expense sharing application

-- Create Users table
CREATE TABLE IF NOT EXISTS users (
    UserId UUID PRIMARY KEY,
    Name VARCHAR(255) NOT NULL,
    Email VARCHAR(255) NOT NULL UNIQUE,
    CognitoUserId VARCHAR(255),
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Create Parties table
CREATE TABLE IF NOT EXISTS parties (
    PartyId UUID PRIMARY KEY,
    OwnerId UUID NOT NULL,
    Name VARCHAR(255) NOT NULL,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (OwnerId) REFERENCES users(UserId)
);

-- Create PartyContributors junction table
CREATE TABLE IF NOT EXISTS partycontributors (
    PartyId UUID NOT NULL,
    UserId UUID NOT NULL,
    PRIMARY KEY (PartyId, UserId),
    FOREIGN KEY (PartyId) REFERENCES parties(PartyId) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES users(UserId) ON DELETE CASCADE
);

-- Create Expenses table
CREATE TABLE IF NOT EXISTS expenses (
    ExpenseId UUID PRIMARY KEY,
    PartyId UUID NOT NULL,
    PayerId UUID NOT NULL,
    Description VARCHAR(500) NOT NULL,
    Amount DECIMAL(10, 2) NOT NULL,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (PartyId) REFERENCES parties(PartyId) ON DELETE CASCADE,
    FOREIGN KEY (PayerId) REFERENCES users(UserId)
);

-- Create ExpenseParticipants junction table
CREATE TABLE IF NOT EXISTS expenseparticipants (
    ExpenseId UUID NOT NULL,
    UserId UUID NOT NULL,
    Share DECIMAL(10, 2),
    PRIMARY KEY (ExpenseId, UserId),
    FOREIGN KEY (ExpenseId) REFERENCES expenses(ExpenseId) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES users(UserId) ON DELETE CASCADE
);

-- Create PartyBillsImages table
CREATE TABLE IF NOT EXISTS partybillsimages (
    BillId UUID PRIMARY KEY,
    BillFileTitle VARCHAR(255) NOT NULL,
    PartyId UUID NOT NULL,
    ImageURL TEXT NOT NULL,
    FOREIGN KEY (PartyId) REFERENCES parties(PartyId) ON DELETE CASCADE
);

-- Create indexes for better performance
CREATE INDEX IF NOT EXISTS idx_parties_owner ON parties(OwnerId);
CREATE INDEX IF NOT EXISTS idx_expenses_party ON expenses(PartyId);
CREATE INDEX IF NOT EXISTS idx_expenses_payer ON expenses(PayerId);
CREATE INDEX IF NOT EXISTS idx_partybillsimages_party ON partybillsimages(PartyId);
CREATE INDEX IF NOT EXISTS idx_users_email ON users(Email);
CREATE INDEX IF NOT EXISTS idx_users_cognito ON users(CognitoUserId);