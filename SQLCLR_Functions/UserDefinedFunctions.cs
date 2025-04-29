using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using System.Text.RegularExpressions;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.IO;

public partial class UserDefinedFunctions
{
    /// <summary>
    /// Encrypt a string to bytes
    /// </summary>
    /// <param name="passwordChars">password for an encryption</param>
    /// <param name="textToBeEncryptedChars">The input string to encrypted</param>
    /// <returns>varbinary(8000)</returns>
    [Microsoft.SqlServer.Server.SqlFunction]
    public static SqlBinary encrypt([SqlFacet(MaxSize = 100)] SqlString passwordChars, [SqlFacet(MaxSize = 4000)] SqlString textToBeEncryptedChars)
    {
        if (passwordChars.IsNull || textToBeEncryptedChars.IsNull)
            return null;

        string password = (string)passwordChars.Value;
        string textToBeEncrypted = (string)textToBeEncryptedChars.Value;
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(textToBeEncrypted))
            return null;

        RijndaelManaged rijndaelCipher = new RijndaelManaged();

        try
        {
            byte[] plainText = System.Text.Encoding.Unicode.GetBytes(textToBeEncrypted);
            byte[] salt = Encoding.ASCII.GetBytes(password.Length.ToString());
            PasswordDeriveBytes secretKey = new PasswordDeriveBytes(password, salt);
            //Creates a symmetric encryptor object. 
            ICryptoTransform encryptor = rijndaelCipher.CreateEncryptor(secretKey.GetBytes(32), secretKey.GetBytes(16));
            using (MemoryStream memoryStream = new MemoryStream())
            {
                //Defines a stream that links data streams to cryptographic transformations
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(plainText, 0, plainText.Length);
                    //Writes the final state and clears the buffer
                    cryptoStream.FlushFinalBlock();
                    byte[] cipherBytes = memoryStream.ToArray();

                    return cipherBytes;
                }
            }
        }
        catch
        {
            return null;
        }
    }


    /// <summary>
    /// Decrypt an encrypted bytes back to a plain text
    /// </summary>
    /// <param name="passwordChars">password for the decryption</param> 
    /// <param name="bytesToBeDecrypted">The encrypted bytes</param>
    /// <returns>nvarchar(max)</returns>
    [Microsoft.SqlServer.Server.SqlFunction]
    public static SqlString decrypt([SqlFacet(MaxSize = 100)] SqlString passwordChars, SqlBinary bytesToBeDecrypted)
    {
        if (passwordChars.IsNull || bytesToBeDecrypted.IsNull)
            return null;

        string password = (string)passwordChars.Value;
        string textToBeDecrypted = ByteArrayToString(bytesToBeDecrypted.Value);

        RijndaelManaged rijndaelCipher = new RijndaelManaged();
        string decryptedData;

        try
        {
            byte[] decryptedBytes = StringToByteArray(textToBeDecrypted);

            byte[] salt = Encoding.ASCII.GetBytes(password.Length.ToString());
            //Making of the key for decryption
            PasswordDeriveBytes secretKey = new PasswordDeriveBytes(password, salt);
            //Creates a symmetric Rijndael decryptor object.
            ICryptoTransform decryptor = rijndaelCipher.CreateDecryptor(secretKey.GetBytes(32), secretKey.GetBytes(16));

            using (MemoryStream memoryStream = new MemoryStream(decryptedBytes))
            {
                //Defines the cryptographics stream for decryption. The stream contains decrypted data
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                {

                    var plainText = new byte[decryptedBytes.Length];
                    int decryptedCount = cryptoStream.Read(plainText, 0, plainText.Length);
                    memoryStream.Close();

                    //Converting to string
                    decryptedData = Encoding.Unicode.GetString(plainText, 0, decryptedCount);
                }
            }
        }
        catch
        {
            decryptedData = null;
        }
        return decryptedData;
    }

    public static string ByteArrayToString(byte[] ba)
    {
        StringBuilder hex = new StringBuilder(ba.Length * 2);
        foreach (byte b in ba)
            hex.AppendFormat("{0:x2}", b);
        return hex.ToString();
    }

    public static byte[] StringToByteArray(String hex)
    {
        int NumberChars = hex.Length;
        byte[] bytes = new byte[NumberChars / 2];
        for (int i = 0; i < NumberChars; i += 2)
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        return bytes;
    }
}

