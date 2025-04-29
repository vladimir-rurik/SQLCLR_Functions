# SQLCLR_Functions

This project demonstrates how to create and deploy SQL CLR functions in a Microsoft SQL Server database. It includes two main user-defined functions for encryption and decryption of text using a password.

---

## Contents

1. [Project Structure](#project-structure)
2. [Prerequisites](#prerequisites)
3. [Building the Project](#building-the-project)
4. [Deploying to SQL Server](#deploying-to-sql-server)
5. [Demonstration of Usage](#demonstration-of-usage)
6. [Troubleshooting & Notes](#troubleshooting--notes)

---

## Project Structure

```
SQLCLR_Functions/
|-- SQLCLR_Functions.sln                // Visual Studio solution
|-- SQLCLR_Functions.sqlproj            // SQL Server database project (main)
|-- SQLCLR_Functions.sqlproj.user       // User-specific project settings (local)
|-- UserDefinedFunctions.cs             // C# code for the CLR functions
|
|-- bin/
|   |-- Debug/
|   |   ...
|   |-- Release/
|       |-- SQLCLR_Functions.dacpac
|       |-- SQLCLR_Functions.dll        // The compiled assembly to deploy
|       |-- SQLCLR_Functions.pdb
|
|-- obj/
|   |-- Debug/
|   |   ...
|   |-- Release/
|       |-- Model.xml
|       |-- SQLCLR_Functions.dll        // Intermediate output
|       |-- SQLCLR_Functions.pdb
|       |-- SQLCLR_Functions.sqlproj.AssemblyReference.cache
|       |-- SQLCLR_Functions.sqlproj.FileListAbsolute.txt
|
└-- README.md                           // This file (documentation)
```

- **UserDefinedFunctions.cs**: Contains the two main methods:
  1. **encrypt**: Encrypts a string into a varbinary (using RijndaelManaged).
  2. **decrypt**: Decrypts a varbinary back into a string using the same password.

- **SQLCLR_Functions.dll**: Compiled assembly that you will register in SQL Server.

---

## Prerequisites

1. **Microsoft SQL Server** (2012 or higher) with CLR integration enabled.
2. **Visual Studio** (2017 or higher) with SQL Server Data Tools (SSDT) or .NET SDK installed.
3. **Permissions**: The database user deploying the assembly needs appropriate permissions (e.g., `ALTER ASSEMBLY`, `CREATE ASSEMBLY`, or `db_owner` in the target database).

---

## Building the Project

1. **Open the Solution**: In Visual Studio, open the file `SQLCLR_Functions.sln`.
2. **Check Configuration**: Usually set to `Release` for a final build.
3. **Build**: Right-click on the `SQLCLR_Functions` project in Solution Explorer and choose **Build** or **Rebuild**.  
   - This will compile the assembly into `SQLCLR_Functions.dll` located in the `bin\Release\` folder.
4. **Result**: Confirm the `SQLCLR_Functions.dll` file is created inside the `bin\Release` folder.

---

## Deploying to SQL Server

Below is one of the simplest ways to deploy the assembly and create the corresponding SQL functions. Adapt the paths as needed:

1. **Enable CLR Integration** (if not already):
   ```sql
   USE master;
   GO
   EXEC sp_configure 'clr enabled', 1;
   RECONFIGURE;
   GO
   ```

2. **Create the Assembly**:
   ```sql
   CREATE ASSEMBLY [SQLCLR_Functions]
   FROM 'C:\Users\Cesar\source\repos\Otus\SQLCLR_Functions\bin\Release\SQLCLR_Functions.dll'
   WITH PERMISSION_SET = SAFE;
   GO
   ```

3. **Create the Functions**:
   ```sql
   CREATE FUNCTION dbo.fnEncrypt
   (
       @password NVARCHAR(100),
       @text     NVARCHAR(4000)
   )
   RETURNS VARBINARY(8000)
   AS EXTERNAL NAME [SQLCLR_Functions].[UserDefinedFunctions].[encrypt];
   GO

   CREATE FUNCTION dbo.fnDecrypt
   (
       @password NVARCHAR(100),
       @encrypted VARBINARY(8000)
   )
   RETURNS NVARCHAR(MAX)
   AS EXTERNAL NAME [SQLCLR_Functions].[UserDefinedFunctions].[decrypt];
   GO
   ```

4. **Verify**: Ensure the assembly and functions exist:
   ```sql
   SELECT * FROM sys.assemblies WHERE name = 'SQLCLR_Functions';
   SELECT * FROM sys.assembly_modules WHERE assembly_id = (SELECT assembly_id FROM sys.assemblies WHERE name = 'SQLCLR_Functions');
   ```

---

## Demonstration of Usage

Once the functions are created, you can **encrypt** and **decrypt** your data. For example:

```sql
-- 1. Encryption
DECLARE @secretPass NVARCHAR(100) = N'myStrongPassword';
DECLARE @plainText NVARCHAR(4000) = N'Hello, SQL CLR world!';

DECLARE @encrypted VARBINARY(8000);
SET @encrypted = dbo.fnEncrypt(@secretPass, @plainText);

SELECT @encrypted AS [EncryptedData];

-- 2. Decryption
DECLARE @decryptedText NVARCHAR(MAX);
SET @decryptedText = dbo.fnDecrypt(@secretPass, @encrypted);

SELECT @decryptedText AS [DecryptedData];
```

You should see that `@decryptedText` matches the original plain text:  
```
Hello, SQL CLR world!
```

---

## Troubleshooting & Notes

1. **Permission Set**:  
   - The assembly is deployed with `PERMISSION_SET = SAFE`. If you need broader permissions (e.g., external file I/O), consider `EXTERNAL_ACCESS` or `UNSAFE`, but this requires additional steps.

2. **Target .NET Framework**:  
   - SQL Server CLR typically supports up to a certain version of .NET Framework (e.g., v4.x). Ensure your project’s **Target Framework** aligns with the server’s supported CLR version.

3. **Handling Nulls**:  
   - The code returns `NULL` if either parameter is `NULL` or if encryption/decryption fails.

4. **Security**:  
   - Use strong passwords and store them securely.  
   - For production scenarios, consider more robust key management strategies.

5. **Performance**:  
   - CLR integration can be slower if used for very large text in a tight loop. For high-volume encryption tasks, weigh alternatives or optimize the approach (e.g., calling CLR in batches).

---
