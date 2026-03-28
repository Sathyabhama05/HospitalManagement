USE HealthcareDB;
GO

-- ROLES TABLE

CREATE TABLE Roles (
    RoleId    INT IDENTITY(1,1) PRIMARY KEY,
    RoleName  NVARCHAR(50)  NOT NULL UNIQUE,
    CreatedAt DATETIME2     NOT NULL DEFAULT GETUTCDATE()
);
GO

-- Seed default roles
INSERT INTO Roles (RoleName) VALUES ('Admin'), ('Doctor'), ('Patient');
GO

-- USERS TABLE

CREATE TABLE Users (
    UserId       INT IDENTITY(1,1) PRIMARY KEY,
    FullName     NVARCHAR(150) NOT NULL,
    Email        NVARCHAR(200) NOT NULL UNIQUE,
    Phone        NVARCHAR(15)  NULL,
    PasswordHash NVARCHAR(500) NOT NULL,
    RoleId       INT           NOT NULL,
    IsActive     BIT           NOT NULL DEFAULT 1,
    CreatedAt    DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt    DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
);
GO

-- REFRESH TOKENS TABLE

CREATE TABLE RefreshTokens (
    TokenId   INT IDENTITY(1,1) PRIMARY KEY,
    UserId    INT           NOT NULL,
    Token     NVARCHAR(500) NOT NULL,
    ExpiresAt DATETIME2     NOT NULL,
    IsRevoked BIT           NOT NULL DEFAULT 0,
    CreatedAt DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId) REFERENCES Users(UserId)
);
GO

-- INDEXES

CREATE NONCLUSTERED INDEX IX_Users_Email         ON Users(Email);
CREATE NONCLUSTERED INDEX IX_Users_RoleId        ON Users(RoleId);
CREATE NONCLUSTERED INDEX IX_RefreshTokens_UserId ON RefreshTokens(UserId);
GO