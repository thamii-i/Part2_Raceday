CREATE DATABASE RaceDay_proj;


USE RaceDay_proj;


DROP TABLE IF EXISTS Results;
DROP TABLE IF EXISTS Enrollments;
DROP TABLE IF EXISTS EventCategories;
DROP TABLE IF EXISTS Categories;
DROP TABLE IF EXISTS Events;
DROP TABLE IF EXISTS Participants;
DROP TABLE IF EXISTS Organisers;


CREATE TABLE Organisers
(
    OrganiserID INT IDENTITY(1,1) NOT NULL,
    FirstName VARCHAR(50) NOT NULL,
    LastName VARCHAR(50) NOT NULL,
    Email VARCHAR(100) NOT NULL,
    Phone VARCHAR(20) NOT NULL,
    OrganisationName VARCHAR(100) NOT NULL,
    PasswordHash VARCHAR(500) NOT NULL,
    CONSTRAINT PK_Organisers PRIMARY KEY (OrganiserID),
    CONSTRAINT UQ_Organisers_Email UNIQUE (Email)
);


CREATE TABLE Participants
(
    ParticipantID INT IDENTITY(1,1) NOT NULL,
    FirstName VARCHAR(50) NOT NULL,
    LastName VARCHAR(50) NOT NULL,
    Email VARCHAR(100) NOT NULL,
    Phone VARCHAR(20) NOT NULL,
    DateOfBirth DATE NOT NULL,
    Gender VARCHAR(10) NOT NULL,
    PasswordHash VARCHAR(500) NOT NULL,
    CONSTRAINT PK_Participants PRIMARY KEY (ParticipantID),
    CONSTRAINT UQ_Participants_Email UNIQUE (Email)
);


CREATE TABLE Events
(
    EventID INT IDENTITY(1,1) NOT NULL,
    EventName VARCHAR(100) NOT NULL,
    Description VARCHAR(500) NOT NULL,
    EventDate DATE NOT NULL,
    Location VARCHAR(100) NOT NULL,
    DistanceKM DECIMAL(5,2) NOT NULL,
    EventType VARCHAR(10) NOT NULL,
    OrganiserID INT NOT NULL,
    CONSTRAINT PK_Events PRIMARY KEY (EventID),
    CONSTRAINT FK_Events_Organisers FOREIGN KEY (OrganiserID) REFERENCES Organisers (OrganiserID),
    CONSTRAINT CK_Events_EventType CHECK (EventType IN ('Run', 'Walk', 'Cycle')),
    CONSTRAINT CK_Events_DistanceKM CHECK (DistanceKM > 0)
);


CREATE TABLE Categories
(
    CategoryID INT IDENTITY(1,1) NOT NULL,
    CategoryName VARCHAR(50) NOT NULL,
    Description VARCHAR(200) NULL,
    CONSTRAINT PK_Categories PRIMARY KEY (CategoryID),
    CONSTRAINT UQ_Categories_CategoryName UNIQUE (CategoryName)
);


CREATE TABLE EventCategories
(
    EventID INT NOT NULL,
    CategoryID INT NOT NULL,
    CONSTRAINT PK_EventCategories PRIMARY KEY (EventID, CategoryID),
    CONSTRAINT FK_EventCategories_Events FOREIGN KEY (EventID) REFERENCES Events (EventID),
    CONSTRAINT FK_EventCategories_Categories FOREIGN KEY (CategoryID) REFERENCES Categories (CategoryID)
);


CREATE TABLE Enrollments
(
    EnrollmentID INT IDENTITY(1,1) NOT NULL,
    ParticipantID INT NOT NULL,
    EventID INT NOT NULL,
    CategoryID INT NOT NULL,
    EnrollmentDate DATE NOT NULL CONSTRAINT DF_Enrollments_EnrollmentDate DEFAULT GETDATE(),
    BibNumber INT NOT NULL,
    Status VARCHAR(20) NOT NULL CONSTRAINT DF_Enrollments_Status DEFAULT 'Registered',
    CONSTRAINT PK_Enrollments PRIMARY KEY (EnrollmentID),
    CONSTRAINT FK_Enrollments_Participants FOREIGN KEY (ParticipantID) REFERENCES Participants (ParticipantID),
    CONSTRAINT FK_Enrollments_Events FOREIGN KEY (EventID) REFERENCES Events (EventID),
    CONSTRAINT FK_Enrollments_Categories FOREIGN KEY (CategoryID) REFERENCES Categories (CategoryID),
    CONSTRAINT UQ_Enrollments_BibNumber UNIQUE (BibNumber),
    CONSTRAINT UQ_Enrollments_Participant_Event UNIQUE (ParticipantID, EventID)
);


CREATE TABLE Results
(
    ResultID INT IDENTITY(1,1) NOT NULL,
    EnrollmentID INT NOT NULL,
    FinishTime TIME NOT NULL,
    FinishPosition INT NOT NULL,
    RecordedAt DATETIME2 NOT NULL CONSTRAINT DF_Results_RecordedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Results PRIMARY KEY (ResultID),
    CONSTRAINT UQ_Results_EnrollmentID UNIQUE (EnrollmentID),
    CONSTRAINT FK_Results_Enrollments FOREIGN KEY (EnrollmentID) REFERENCES Enrollments (EnrollmentID),
    CONSTRAINT CK_Results_FinishPosition CHECK (FinishPosition > 0)
);


INSERT INTO Organisers
    (FirstName, LastName, Email, Phone, OrganisationName, PasswordHash)
VALUES
    ('Thabo', 'Mokoena', 'thabo@gmail.com', '0821234567', 'RaceDay SA', 'AQAAAAEAAYagAAAAELPfXmWAEru3KbUtx9Ybt+c+hOxJals12wYn+FfQ0CUNW3hiHYT2949dWMS0FdXYhQ=='),
    ('Sarah', 'Naidoo', 'sarah@gmail.com', '0837654321', 'Run SA', 'AQAAAAEAAYagAAAAEFC+KbjhWrsKogLDYDB7cc0way2J7CkuQxMPqAKZoES9Cija1RBeDK0ajaSA8ovYNA==');


INSERT INTO Participants
    (FirstName, LastName, Email, Phone, DateOfBirth, Gender, PasswordHash)
VALUES
    ('Liam', 'Dlamini', 'liam@gmail.com', '0712345678', '2002-05-14', 'Male', 'AQAAAAEAAYagAAAAEANxuYZi6z7QFApZ/2HNpaTCpVG8Dshz9pVI+g9HWMdvYcTormdd1u2vWFRtICsMwA=='),
    ('Amahle', 'Ndlovu', 'amahle@gmail.com', '0723456789', '2001-08-21', 'Female', 'AQAAAAEAAYagAAAAEO3p1zGZmE2KeVStmq+re+jeuI3yVKgHEHJbhyHsEpk21zfQDQ0qvZPQ+vCGT/Xj2w=='),
    ('Jason', 'Peters', 'jason@gmail.com', '0734567890', '1999-03-10', 'Male', 'AQAAAAEAAYagAAAAEG4/KdFO/WR/V/VxSG2y+PJ3335KlM390Hs3JqmAY4n/0En/qJRXywH0G4W2S1JS7w=='),
    ('Naledi', 'Molefe', 'naledi@gmail.com', '0745678901', '2003-11-02', 'Female', 'AQAAAAEAAYagAAAAEMGv35Q2SYpAmu5MiQxxXdfUYzwSWVYVIUAMfoYjj0iyzSjFI/o0XIu1cTwX9lJEYw==');


INSERT INTO Events
    (EventName, Description, EventDate, Location, DistanceKM, EventType, OrganiserID)
VALUES
    ('Johannesburg City Run', 'A 10km city road run through the streets of Johannesburg.', '2026-10-10', 'Johannesburg', 10.00, 'Run', 1),
    ('Cape Town Marathon', 'The full 42.2km marathon through the streets of Cape Town.', '2026-11-15', 'Cape Town', 42.20, 'Run', 2),
    ('Pretoria Fun Run', 'A relaxed 5km fun run for the whole family in Pretoria.', '2026-12-05', 'Pretoria', 5.00, 'Run', 1);


INSERT INTO Categories
    (CategoryName, Description)
VALUES
    ('Men', 'Male runners'),
    ('Women', 'Female runners'),
    ('Junior', 'Young runners'),
    ('Senior', 'Adult runners'),
    ('Professional', 'Professional runners'),
    ('Amateur', 'Amateur runners'),
    ('Open', 'Open to eligible runners'),
    ('Veteran', 'Veteran runners'),
    ('Fun Run', 'Fun running category');


INSERT INTO EventCategories
    (EventID, CategoryID)
VALUES
    (1, 1),
    (1, 2),
    (1, 7),
    (2, 5),
    (2, 6),
    (2, 8),
    (3, 1),
    (3, 2),
    (3, 9);


INSERT INTO Enrollments
    (ParticipantID, EventID, CategoryID, EnrollmentDate, BibNumber, Status)
VALUES
    (1, 1, 1, '2026-08-01', 101, 'Registered'),
    (2, 1, 2, '2026-08-02', 102, 'Registered'),
    (3, 2, 5, '2026-08-05', 201, 'Registered'),
    (4, 2, 6, '2026-08-06', 202, 'Registered'),
    (1, 3, 9, '2026-08-10', 301, 'Registered'),
    (2, 3, 2, '2026-08-11', 302, 'Registered');


INSERT INTO Results
    (EnrollmentID, FinishTime, FinishPosition)
VALUES
    (1, '00:48:12', 1),
    (2, '00:55:40', 2);


SELECT * FROM Organisers;
SELECT * FROM Participants;
SELECT * FROM Events;
SELECT * FROM Categories;
SELECT * FROM EventCategories;
SELECT * FROM Enrollments;
SELECT * FROM Results;
