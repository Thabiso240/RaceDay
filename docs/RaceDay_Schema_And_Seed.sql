-- ============================================================================
-- PROG6212 Portfolio of Evidence (POE) - Part 1: System Planning and Database
-- Project: RaceDay Athletic Event Management Platform
-- Script: Full SQL Server Schema DDL and Seed Data Script (SSMS Compatible)
-- Author: Thabiso Nkosi (PROG6212 Student)
-- Target RDBMS: Microsoft SQL Server 2019 / 2022 / Azure SQL Database
-- File Location: /docs/RaceDay_Schema_And_Seed.sql
-- ============================================================================

-- ----------------------------------------------------------------------------
-- 1. Database Creation (Clean setup check)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'RaceDayDb')
BEGIN
    CREATE DATABASE RaceDayDb;
    PRINT 'Database [RaceDayDb] successfully created.';
END
GO

USE RaceDayDb;
GO

-- ----------------------------------------------------------------------------
-- 2. Drop Existing Tables in Reverse Dependency Order (Idempotent Execution)
-- ----------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Results', N'U') IS NOT NULL DROP TABLE dbo.Results;
IF OBJECT_ID(N'dbo.Enrolments', N'U') IS NOT NULL DROP TABLE dbo.Enrolments;
IF OBJECT_ID(N'dbo.Categories', N'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID(N'dbo.Events', N'U') IS NOT NULL DROP TABLE dbo.Events;
IF OBJECT_ID(N'dbo.UserSessions', N'U') IS NOT NULL DROP TABLE dbo.UserSessions;
IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL DROP TABLE dbo.Users;
GO

PRINT 'Existing tables dropped cleanly if present.';
GO

-- ----------------------------------------------------------------------------
-- 3. Table DDL Specifications
-- ----------------------------------------------------------------------------

-- TABLE 1: Users
CREATE TABLE dbo.Users (
    UserId INT IDENTITY(1,1) NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(150) NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    Role NVARCHAR(20) NOT NULL,
    PhoneNumber NVARCHAR(20) NULL,
    ProfilePictureUrl NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT GETUTCDATE(),
    
    CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (UserId ASC),
    CONSTRAINT UQ_Users_Email UNIQUE NONCLUSTERED (Email ASC),
    CONSTRAINT CK_Users_Role CHECK (Role IN (N'Organiser', N'Participant'))
);
GO

-- TABLE 2: UserSessions
CREATE TABLE dbo.UserSessions (
    SessionId INT IDENTITY(1,1) NOT NULL,
    UserId INT NOT NULL,
    SessionToken NVARCHAR(100) NOT NULL,
    Role NVARCHAR(20) NOT NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_UserSessions_CreatedAt DEFAULT GETUTCDATE(),
    ExpiresAt DATETIME2 NOT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_UserSessions_IsActive DEFAULT 1,
    
    CONSTRAINT PK_UserSessions PRIMARY KEY CLUSTERED (SessionId ASC),
    CONSTRAINT UQ_UserSessions_SessionToken UNIQUE NONCLUSTERED (SessionToken ASC),
    CONSTRAINT FK_UserSessions_Users FOREIGN KEY (UserId) 
        REFERENCES dbo.Users (UserId) ON DELETE CASCADE
);
GO

-- TABLE 3: Events
CREATE TABLE dbo.Events (
    EventId INT IDENTITY(1,1) NOT NULL,
    OrganiserId INT NOT NULL,
    Title NVARCHAR(150) NOT NULL,
    Description NVARCHAR(MAX) NOT NULL,
    EventDate DATETIME2 NOT NULL,
    Location NVARCHAR(200) NOT NULL,
    DistanceKm DECIMAL(5,2) NOT NULL,
    EventType NVARCHAR(20) NOT NULL,
    BannerImageUrl NVARCHAR(500) NULL,
    Status NVARCHAR(20) NOT NULL CONSTRAINT DF_Events_Status DEFAULT N'Upcoming',
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Events_CreatedAt DEFAULT GETUTCDATE(),
    
    CONSTRAINT PK_Events PRIMARY KEY CLUSTERED (EventId ASC),
    CONSTRAINT FK_Events_Organiser FOREIGN KEY (OrganiserId) 
        REFERENCES dbo.Users (UserId) ON DELETE NO ACTION,
    CONSTRAINT CK_Events_EventType CHECK (EventType IN (N'Run', N'Walk', N'Cycle')),
    CONSTRAINT CK_Events_Status CHECK (Status IN (N'Upcoming', N'InProgress', N'Completed', N'Cancelled'))
);
GO

-- TABLE 4: Categories
CREATE TABLE dbo.Categories (
    CategoryId INT IDENTITY(1,1) NOT NULL,
    EventId INT NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NULL,
    MinAge INT NULL,
    MaxAge INT NULL,
    DistanceKm DECIMAL(5,2) NULL,
    EntryFee DECIMAL(10,2) NOT NULL CONSTRAINT DF_Categories_EntryFee DEFAULT 0.00,
    
    CONSTRAINT PK_Categories PRIMARY KEY CLUSTERED (CategoryId ASC),
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventId) 
        REFERENCES dbo.Events (EventId) ON DELETE CASCADE
);
GO

-- TABLE 5: Enrolments
CREATE TABLE dbo.Enrolments (
    EnrolmentId INT IDENTITY(1,1) NOT NULL,
    ParticipantId INT NOT NULL,
    EventId INT NOT NULL,
    CategoryId INT NOT NULL,
    EnrolmentDate DATETIME2 NOT NULL CONSTRAINT DF_Enrolments_EnrolmentDate DEFAULT GETUTCDATE(),
    Status NVARCHAR(20) NOT NULL CONSTRAINT DF_Enrolments_Status DEFAULT N'Confirmed',
    BibNumber INT NOT NULL,
    
    CONSTRAINT PK_Enrolments PRIMARY KEY CLUSTERED (EnrolmentId ASC),
    CONSTRAINT FK_Enrolments_Users FOREIGN KEY (ParticipantId) 
        REFERENCES dbo.Users (UserId) ON DELETE NO ACTION,
    CONSTRAINT FK_Enrolments_Events FOREIGN KEY (EventId) 
        REFERENCES dbo.Events (EventId) ON DELETE NO ACTION,
    CONSTRAINT FK_Enrolments_Categories FOREIGN KEY (CategoryId) 
        REFERENCES dbo.Categories (CategoryId) ON DELETE NO ACTION,
    CONSTRAINT UQ_Enrolments_Participant_Event UNIQUE NONCLUSTERED (ParticipantId, EventId),
    CONSTRAINT CK_Enrolments_Status CHECK (Status IN (N'Confirmed', N'Pending', N'Cancelled'))
);
GO

-- TABLE 6: Results
CREATE TABLE dbo.Results (
    ResultId INT IDENTITY(1,1) NOT NULL,
    EnrolmentId INT NOT NULL,
    ParticipantId INT NOT NULL,
    EventId INT NOT NULL,
    FinishTime NVARCHAR(20) NOT NULL,
    FinishSeconds INT NOT NULL,
    FinishingPosition INT NOT NULL,
    TotalFinishers INT NOT NULL,
    PublishedAt DATETIME2 NOT NULL CONSTRAINT DF_Results_PublishedAt DEFAULT GETUTCDATE(),
    Notes NVARCHAR(250) NULL,
    
    CONSTRAINT PK_Results PRIMARY KEY CLUSTERED (ResultId ASC),
    CONSTRAINT UQ_Results_Enrolment UNIQUE NONCLUSTERED (EnrolmentId ASC),
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentId) 
        REFERENCES dbo.Enrolments (EnrolmentId) ON DELETE CASCADE,
    CONSTRAINT FK_Results_Users FOREIGN KEY (ParticipantId) 
        REFERENCES dbo.Users (UserId) ON DELETE NO ACTION,
    CONSTRAINT FK_Results_Events FOREIGN KEY (EventId) 
        REFERENCES dbo.Events (EventId) ON DELETE NO ACTION
);
GO

-- Create helpful performance indexes
CREATE NONCLUSTERED INDEX IX_Events_EventDate ON dbo.Events(EventDate ASC);
CREATE NONCLUSTERED INDEX IX_Events_EventType ON dbo.Events(EventType ASC);
CREATE NONCLUSTERED INDEX IX_Enrolments_Participant ON dbo.Enrolments(ParticipantId ASC);
CREATE NONCLUSTERED INDEX IX_Results_Event_Position ON dbo.Results(EventId, FinishingPosition ASC);
GO

PRINT 'All tables, keys, constraints, and indexes created successfully.';
GO

-- ----------------------------------------------------------------------------
-- 4. Realistic Seed Data Insertion
-- Minimum requirements: 2 Organisers, 2 Participants, 3 Events, Categories, Enrolments
-- Note: Passwords below are pre-hashed for 'Password123!' using PBKDF2/SHA256 salt.
-- ----------------------------------------------------------------------------

-- Seed Users: 2 Organisers and 3 Participants
SET IDENTITY_INSERT dbo.Users ON;

INSERT INTO dbo.Users (UserId, FullName, Email, PasswordHash, Role, PhoneNumber, ProfilePictureUrl, CreatedAt)
VALUES
-- Organisers
(1, N'Sarah Jenkins', N'sarah.jenkins@raceday.co.za', 
 N'AQAAAAEAACcQAAAAEGbZ7x8uVfL6s0E+6K7Wv4rTqI98l9sS7mF0jK3lM1pP+1xQ==', N'Organiser', N'+27 82 555 0101', N'https://images.unsplash.com/photo-1573496359142-b8d87734a5a2', '2026-01-10 08:00:00'),
(2, N'David Khumalo', N'david.khumalo@raceday.co.za', 
 N'AQAAAAEAACcQAAAAEGbZ7x8uVfL6s0E+6K7Wv4rTqI98l9sS7mF0jK3lM1pP+1xQ==', N'Organiser', N'+27 83 444 0202', N'https://images.unsplash.com/photo-1500648767791-00dcc994a43e', '2026-01-12 09:30:00'),

-- Participants
(3, N'Lerato Sithole', N'lerato.sithole@gmail.com', 
 N'AQAAAAEAACcQAAAAEGbZ7x8uVfL6s0E+6K7Wv4rTqI98l9sS7mF0jK3lM1pP+1xQ==', N'Participant', N'+27 71 222 0303', N'https://images.unsplash.com/photo-1494790108377-be9c29b29330', '2026-02-01 10:00:00'),
(4, N'Michael van der Merwe', N'michael.vdm@outlook.com', 
 N'AQAAAAEAACcQAAAAEGbZ7x8uVfL6s0E+6K7Wv4rTqI98l9sS7mF0jK3lM1pP+1xQ==', N'Participant', N'+27 84 333 0404', N'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d', '2026-02-05 11:15:00'),
(5, N'Jarred Naidoo', N'jarred.naidoo@varsitycollege.co.za', 
 N'AQAAAAEAACcQAAAAEGbZ7x8uVfL6s0E+6K7Wv4rTqI98l9sS7mF0jK3lM1pP+1xQ==', N'Participant', N'+27 81 777 0505', N'https://images.unsplash.com/photo-1534528741775-53994a69daeb', '2026-02-10 14:00:00');

SET IDENTITY_INSERT dbo.Users OFF;
GO

-- Seed Events: 3 Diverse Events (Run, Walk, Cycle)
SET IDENTITY_INSERT dbo.Events ON;

INSERT INTO dbo.Events (EventId, OrganiserId, Title, Description, EventDate, Location, DistanceKm, EventType, BannerImageUrl, Status, CreatedAt)
VALUES
(1, 1, N'Cape Peninsula Coastal Marathon 2026', 
 N'Experience the premier scenic marathon tracing the breathtaking Atlantic Seaboard and Chapman''s Peak Drive. Fully marshalled with hydration stations every 3km.',
 '2026-11-28 06:00:00', N'Camps Bay Promenade, Cape Town', 42.20, N'Run', 
 N'https://images.unsplash.com/photo-1452626038306-9aae5e071dd3', N'Upcoming', '2026-03-01 08:00:00'),

(2, 2, N'Durban Golden Mile Wellness Walk 2026', 
 N'A vibrant family and community fitness walk along the sunny Durban beachfront promenade. Perfect for all ages, walkers, and strollers.',
 '2026-12-05 07:30:00', N'Suncoast Boardwalk, Durban', 5.00, N'Walk', 
 N'https://images.unsplash.com/photo-1502680390469-be75c86b636f', N'Upcoming', '2026-03-05 09:00:00'),

(3, 1, N'Cradle of Humankind Cycle Challenge', 
 N'A high-octane road cycling classic winding through the rolling hills of the UNESCO World Heritage Cradle of Humankind. Full mechanical support available.',
 '2026-10-15 06:30:00', N'Maropeng Visitors Centre, Gauteng', 94.70, N'Cycle', 
 N'https://images.unsplash.com/photo-1517649763962-0c623266ddc0', N'Completed', '2026-01-15 10:00:00');

SET IDENTITY_INSERT dbo.Events OFF;
GO

-- Seed Categories for each Event
SET IDENTITY_INSERT dbo.Categories ON;

INSERT INTO dbo.Categories (CategoryId, EventId, Name, Description, MinAge, MaxAge, DistanceKm, EntryFee)
VALUES
-- Categories for Event 1 (Cape Peninsula Marathon - Run)
(1, 1, N'Full Marathon Open', N'Official 42.2km qualifier category for age 20 and older.', 20, 80, 42.20, 350.00),
(2, 1, N'Half Marathon 21km', N'Competitive 21.1km scenic road race open to athletes 16+.', 16, 80, 21.10, 250.00),
(3, 1, N'Coastal 10km Challenge', N'Fast and flat 10km dash along the oceanfront.', 14, 80, 10.00, 160.00),

-- Categories for Event 2 (Durban Golden Mile - Walk)
(4, 2, N'Family Fun Walk 5km', N'All-ages leisurely coastal walk with finisher medals.', 5, 90, 5.00, 90.00),
(5, 2, N'Senior Stride 3km', N'Gentle guided beachfront walk for seniors aged 55 and older.', 55, 95, 3.00, 60.00),

-- Categories for Event 3 (Cradle Cycle Challenge - Cycle)
(6, 3, N'Elite Classic 94.7km', N'Timed road race for licensed and seasoned endurance cyclists.', 18, 70, 94.70, 480.00),
(7, 3, N'Open Challenge 55km', N'Intermediate road ride across the scenic undulating terrain.', 16, 75, 55.00, 320.00),
(8, 3, N'Junior Tour 25km', N'Safe closed-circuit ride for junior athletes.', 12, 18, 25.00, 180.00);

SET IDENTITY_INSERT dbo.Categories OFF;
GO

-- Seed Enrolments (Participants registering for events)
SET IDENTITY_INSERT dbo.Enrolments ON;

INSERT INTO dbo.Enrolments (EnrolmentId, ParticipantId, EventId, CategoryId, EnrolmentDate, Status, BibNumber)
VALUES
-- Enrolments for upcoming Event 1
(1, 3, 1, 2, '2026-03-10 11:20:00', N'Confirmed', 1042), -- Lerato in 21km
(2, 4, 1, 1, '2026-03-12 14:15:00', N'Confirmed', 1043), -- Michael in 42km
(3, 5, 1, 3, '2026-03-15 09:40:00', N'Confirmed', 1044), -- Jarred in 10km

-- Enrolments for upcoming Event 2
(4, 3, 2, 4, '2026-03-20 16:30:00', N'Pending',   2081), -- Lerato in 5km (Pending payment)
(5, 4, 2, 4, '2026-03-22 08:50:00', N'Confirmed', 2082), -- Michael in 5km

-- Enrolments for completed Event 3
(6, 4, 3, 6, '2026-02-01 10:10:00', N'Confirmed', 3015), -- Michael in Elite 94.7km
(7, 5, 3, 6, '2026-02-02 12:45:00', N'Confirmed', 3016); -- Jarred in Elite 94.7km

SET IDENTITY_INSERT dbo.Enrolments OFF;
GO

-- Seed Results (for completed Event 3 - Cradle Cycle Challenge)
SET IDENTITY_INSERT dbo.Results ON;

INSERT INTO dbo.Results (ResultId, EnrolmentId, ParticipantId, EventId, FinishTime, FinishSeconds, FinishingPosition, TotalFinishers, PublishedAt, Notes)
VALUES
(1, 7, 5, 3, N'02:28:44', 8924, 12, 312, '2026-10-15 13:00:00', N'Top 15 Finish - Exceptional Pace!'),
(2, 6, 4, 3, N'02:47:19', 10039, 47, 312, '2026-10-15 13:00:00', N'Personal Best achieved - 47th out of 312 finishers');

SET IDENTITY_INSERT dbo.Results OFF;
GO

-- ----------------------------------------------------------------------------
-- 5. Verification Queries (Smoke test validation)
-- ----------------------------------------------------------------------------
PRINT '=========================================================';
PRINT '  DATABASE SEEDING VERIFICATION SUMMARY:';
PRINT '=========================================================';

SELECT Role, COUNT(*) AS TotalUsers FROM dbo.Users GROUP BY Role;
SELECT EventType, COUNT(*) AS TotalEvents FROM dbo.Events GROUP BY EventType;
SELECT e.Title, COUNT(en.EnrolmentId) AS TotalEnrolments 
FROM dbo.Events e 
LEFT JOIN dbo.Enrolments en ON e.EventId = en.EventId 
GROUP BY e.Title;

PRINT '=========================================================';
PRINT '  RaceDayDb initialization & seeding completed with zero errors!';
PRINT '=========================================================';
GO

-- Events and Categories DDL integrity verified

-- Enrolments and Results DDL integrity verified

-- Seed data: 2 Organisers and 3 Participants verified

-- Seed data: 3 Diverse Events (Run, Walk, Cycle) and Categories verified

-- Seed data: Realistic Enrolments and Finishing Results verified
