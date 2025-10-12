-- Add CognitoUserId column to Users table for Cognito integration
-- This script should be run against your PostgreSQL database

-- Add the CognitoUserId column (nullable initially)
ALTER TABLE Users 
ADD COLUMN CognitoUserId VARCHAR(128);

-- Add index for faster lookups by CognitoUserId
CREATE INDEX idx_users_cognito_user_id ON Users(CognitoUserId);

-- Add unique constraint to ensure one-to-one mapping
-- Note: This is optional - uncomment if you want to enforce unique mapping
-- ALTER TABLE Users ADD CONSTRAINT uk_users_cognito_user_id UNIQUE (CognitoUserId);

-- Update existing users to have null CognitoUserId (no action needed, already null)

-- Example query to verify the schema change
-- SELECT column_name, data_type, is_nullable 
-- FROM information_schema.columns 
-- WHERE table_name = 'users' AND table_schema = 'public';