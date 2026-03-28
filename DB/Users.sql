-- 1. ROLES TABLE
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Roles' AND xtype='U')
BEGIN
    CREATE TABLE Roles (
        RoleId      INT IDENTITY(1,1) PRIMARY KEY,
        RoleName    NVARCHAR(50) NOT NULL UNIQUE,
        CreatedAt   DATETIME2 NOT NULL DEFAULT GETDATE()
    );

    INSERT INTO Roles (RoleName) VALUES ('Admin'), ('Doctor'), ('Patient');
END
GO

-- 2. USERS TABLE
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Users' AND xtype='U')
BEGIN
    CREATE TABLE Users (
        UserId          INT IDENTITY(1,1) PRIMARY KEY,
        FullName        NVARCHAR(150) NOT NULL,
        Email           NVARCHAR(200) NOT NULL UNIQUE,
        Phone           NVARCHAR(15),
        PasswordHash    NVARCHAR(500) NOT NULL,
        RoleId          INT NOT NULL,
        IsActive        BIT NOT NULL DEFAULT 1,
        CreatedAt       DATETIME2 NOT NULL DEFAULT GETDATE(),
        UpdatedAt       DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
    );
END
GO

-- 3. REFRESH TOKENS TABLE
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='RefreshTokens' AND xtype='U')
BEGIN
    CREATE TABLE RefreshTokens (
        TokenId     INT IDENTITY(1,1) PRIMARY KEY,
        UserId      INT NOT NULL,
        Token       NVARCHAR(500) NOT NULL,
        ExpiresAt   DATETIME2 NOT NULL,
        IsRevoked   BIT NOT NULL DEFAULT 0,
        CreatedAt   DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId) REFERENCES Users(UserId)
    );
END
GO

-- 4. INDEXES
CREATE NONCLUSTERED INDEX IX_Users_Email     ON Users(Email);
CREATE NONCLUSTERED INDEX IX_Users_RoleId    ON Users(RoleId);
CREATE NONCLUSTERED INDEX IX_RefreshTokens_UserId ON RefreshTokens(UserId);
GO

-- STORED PROCEDURE: sp_RegisterUser

CREATE OR ALTER PROCEDURE sp_RegisterUser
    @FullName       NVARCHAR(150),
    @Email          NVARCHAR(200),
    @Phone          NVARCHAR(15),
    @PasswordHash   NVARCHAR(500),
    @RoleId         INT,
    @NewUserId      INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        IF EXISTS (SELECT 1 FROM Users WHERE Email = @Email)
        BEGIN
            RAISERROR('Email already registered.', 16, 1);
            RETURN;
        END

        IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleId = @RoleId)
        BEGIN
            RAISERROR('Invalid role specified.', 16, 1);
            RETURN;
        END

        INSERT INTO Users (FullName, Email, Phone, PasswordHash, RoleId)
        VALUES (@FullName, @Email, @Phone, @PasswordHash, @RoleId);

        SET @NewUserId = SCOPE_IDENTITY();

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- STORED PROCEDURE: sp_LoginUser
CREATE OR ALTER PROCEDURE sp_LoginUser
    @Email NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.UserId,
        u.FullName,
        u.Email,
        u.Phone,
        u.PasswordHash,
        u.IsActive,
        u.RoleId,
        r.RoleName
    FROM Users u
    INNER JOIN Roles r ON u.RoleId = r.RoleId
    WHERE u.Email = @Email;
END
GO

-- STORED PROCEDURE: sp_SaveRefreshToken
CREATE OR ALTER PROCEDURE sp_SaveRefreshToken
    @UserId     INT,
    @Token      NVARCHAR(500),
    @ExpiresAt  DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
       
        UPDATE RefreshTokens SET IsRevoked = 1 WHERE UserId = @UserId AND IsRevoked = 0;

        INSERT INTO RefreshTokens (UserId, Token, ExpiresAt)
        VALUES (@UserId, @Token, @ExpiresAt);
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

-- STORED PROCEDURE: sp_GetRefreshToken

CREATE OR ALTER PROCEDURE sp_GetRefreshToken
    @Token NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        rt.TokenId,
        rt.UserId,
        rt.Token,
        rt.ExpiresAt,
        rt.IsRevoked,
        u.Email,
        u.RoleId,
        r.RoleName
    FROM RefreshTokens rt
    INNER JOIN Users u ON rt.UserId = u.UserId
    INNER JOIN Roles r ON u.RoleId = r.RoleId
    WHERE rt.Token = @Token AND rt.IsRevoked = 0;
END
GO