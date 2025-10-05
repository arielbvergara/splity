CREATE TABLE Users
(
    UserId UUID PRIMARY KEY,
    Name   TEXT        NOT NULL,
    Email  TEXT UNIQUE NOT NULL,
    CreatedAt TIMESTAMP DEFAULT NOW()
);

CREATE TABLE Parties
(
    PartyId   UUID PRIMARY KEY,
    OwnerId   UUID NOT NULL, -- Reference to Users.UserId (app-enforced)
    Name      TEXT NOT NULL,
    CreatedAt TIMESTAMP DEFAULT NOW()
);

CREATE TABLE Expenses
(
    ExpenseId   UUID PRIMARY KEY,
    PartyId     UUID           NOT NULL, -- Reference to Parties.PartyId (app-enforced)
    PayerId     UUID           NOT NULL, -- Reference to Users.UserId (app-enforced)
    Description TEXT           NOT NULL,
    Amount      NUMERIC(10, 2) NOT NULL,
    CreatedAt   TIMESTAMP DEFAULT NOW()
);

CREATE TABLE PartyContributors
(
    PartyId UUID NOT NULL, -- Reference to Parties.PartyId
    UserId  UUID NOT NULL, -- Reference to Users.UserId
    PRIMARY KEY (PartyId, UserId)
);

CREATE TABLE ExpenseParticipants
(
    ExpenseId UUID NOT NULL,  -- Reference to Expenses.ExpenseId
    UserId    UUID NOT NULL,  -- Reference to Users.UserId
    Share     NUMERIC(10, 2), -- Optional: how much they contribute
    PRIMARY KEY (ExpenseId, UserId)
);

CREATE TABLE PartyBillsImages
(
    BillId   UUID PRIMARY KEY,
    BillFileTitle TEXT NOT NULL,
    PartyId  UUID NOT NULL, -- Reference to Parties.PartyId
    ImageURL TEXT NOT NULL
);
