
-- sp_RegisterUser

CREATE OR ALTER PROCEDURE sp_RegisterUser
    @FullName     NVARCHAR(150),
    @Email        NVARCHAR(200),
    @PasswordHash NVARCHAR(500),
    @Phone        NVARCHAR(15)  = NULL,
    @UserId       INT OUTPUT,
    @Message      NVARCHAR(200) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        -- Check if email already exists
        IF EXISTS (SELECT 1 FROM Users WHERE Email = @Email)
        BEGIN
            SET @UserId  = -1;
            SET @Message = 'An account with this email already exists';
            ROLLBACK; RETURN;
        END

        -- Get Patient RoleId dynamically (not hardcoded)
        DECLARE @PatientRoleId INT;
        SELECT @PatientRoleId = RoleId FROM Roles WHERE RoleName = 'Patient';

        INSERT INTO Users (FullName, Email, PasswordHash, Phone, RoleId)
        VALUES (@FullName, @Email, @PasswordHash, @Phone, @PatientRoleId);

        SET @UserId  = SCOPE_IDENTITY();
        SET @Message = 'User registered successfully';
        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        SET @UserId = -1;
        -- Handle unique constraint violation cleanly
        IF ERROR_NUMBER() IN (2627, 2601)
            SET @Message = 'An account with this email already exists';
        ELSE
            SET @Message = ERROR_MESSAGE();
    END CATCH
END
GO

-- sp_GetUserByEmail

CREATE OR ALTER PROCEDURE sp_GetUserByEmail
    @Email NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.UserId,
           u.Email,
           u.PasswordHash,
           u.RoleId,
           u.IsActive,
           r.RoleName,
           u.FullName
    FROM Users u
    INNER JOIN Roles r ON u.RoleId = r.RoleId
    WHERE u.Email = @Email;
END
GO

-- sp_GetUserById

CREATE OR ALTER PROCEDURE sp_GetUserById
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.UserId,
           u.Email,
           u.PasswordHash,
           u.RoleId,
           u.IsActive,
           r.RoleName,
           u.FullName
    FROM Users u
    INNER JOIN Roles r ON u.RoleId = r.RoleId
    WHERE u.UserId = @UserId;
END
GO

-- sp_SaveRefreshToken

CREATE PROCEDURE sp_SaveRefreshToken
    @UserId    INT,
    @Token     NVARCHAR(500),
    @ExpiresAt DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE RefreshTokens
    SET IsRevoked = 1
    WHERE UserId = @UserId AND IsRevoked = 0;

    INSERT INTO RefreshTokens (UserId, Token, ExpiresAt)
    VALUES (@UserId, @Token, @ExpiresAt);
END
GO

CREATE PROCEDURE sp_GetRefreshToken
    @Token NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT rt.TokenId, rt.UserId, rt.Token, rt.ExpiresAt,
           rt.IsRevoked, rt.CreatedAt,
           u.Email, u.RoleId, r.RoleName, u.FullName
    FROM RefreshTokens rt
    INNER JOIN Users u ON rt.UserId = u.UserId
    INNER JOIN Roles r ON u.RoleId  = r.RoleId
    WHERE rt.Token = @Token;
END
GO

CREATE PROCEDURE sp_RevokeRefreshToken
    @Token NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE RefreshTokens
    SET IsRevoked = 1
    WHERE Token = @Token;
END
GO