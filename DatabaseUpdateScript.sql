-- Database Migration Script: Fix ErrorMessage Column in RawMessages Table
-- Run this script to update the database schema

-- For MySQL/MariaDB:
ALTER TABLE RawMessages 
MODIFY COLUMN ErrorMessage VARCHAR(4000) NULL;

-- For SQLite:
-- SQLite doesn't support MODIFY COLUMN directly, so you need to:
-- 1. Create a new table with the correct schema
-- 2. Copy data from the old table
-- 3. Drop the old table
-- 4. Rename the new table

-- For PostgreSQL:
-- ALTER TABLE "RawMessages" 
-- ALTER COLUMN "ErrorMessage" DROP NOT NULL;

-- Verify the change:
SELECT 
    COLUMN_NAME, 
    IS_NULLABLE, 
    DATA_TYPE, 
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'RawMessages' 
    AND COLUMN_NAME = 'ErrorMessage';

-- Expected result: IS_NULLABLE should be 'YES'
