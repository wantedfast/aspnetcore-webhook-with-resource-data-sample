// <copyright file="Decryptor.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace TeamsGraphChangeNotification
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;

    public class Decryptor
    {
        private static readonly int AESInitializationVectorSize = new AesCryptoServiceProvider().LegalBlockSizes[0].MinSize;

        public string Decrypt(string encryptedPayload, string encryptedSymmetricKey, string signature, string certificate)
        {
            if (string.IsNullOrEmpty(encryptedPayload))
            {
                throw new ArgumentNullException("Encrypted payload cannot be null or empty");
            }
            if (string.IsNullOrEmpty(encryptedSymmetricKey))
            {
                throw new ArgumentNullException("Encrypted symmetric key cannot be null or empty");
            }
            if (string.IsNullOrEmpty(signature))
            {
                throw new ArgumentNullException("Signature cannot be null or empty");
            }
            if (string.IsNullOrEmpty(certificate))
            {
                throw new ArgumentNullException("Certificate cannot be null or empty");
            }
            RSA rsaPrivateKey = RSACertificateExtensions.GetRSAPrivateKey(new X509Certificate2(Convert.FromBase64String(certificate)));
            return this.DecryptPayload(encryptedPayload, encryptedSymmetricKey, signature, rsaPrivateKey);
        }

        private string DecryptPayload(string encryptedData, string encryptedSymmetricKey, string signature, RSA asymmetricPrivateKey)
        {
            try
            {
                byte[] key = asymmetricPrivateKey.Decrypt(Convert.FromBase64String(encryptedSymmetricKey), RSAEncryptionPadding.OaepSHA1);
                string base64String = Convert.ToBase64String(new HMACSHA256(key).ComputeHash(Convert.FromBase64String(encryptedData)));
                if (!string.Equals(signature, base64String))
                {
                    throw new InvalidDataException("Signature does not match");
                }
                return Encoding.UTF8.GetString(this.AESDecrypt(Convert.FromBase64String(encryptedData), key));
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Unexpected error occured while trying to decrypt the input", ex);
            }
        }

        private byte[] AESDecrypt(byte[] dataToDecrypt, byte[] key)
        {
            if (dataToDecrypt == null)
            {
                throw new ArgumentNullException("Data to decrypt cannot be null");
            }
            if (key != null)
            {
                if (key.Length >= Decryptor.AESInitializationVectorSize / 8)
                {
                    try
                    {
                        using (AesCryptoServiceProvider cryptoServiceProvider = new AesCryptoServiceProvider())
                        {
                            cryptoServiceProvider.Mode = CipherMode.CBC;
                            cryptoServiceProvider.Padding = PaddingMode.PKCS7;
                            cryptoServiceProvider.Key = key;
                            byte[] numArray = new byte[Decryptor.AESInitializationVectorSize / 8];
                            Array.Copy((Array)key, (Array)numArray, numArray.Length);
                            cryptoServiceProvider.IV = numArray;
                            using (MemoryStream memoryStream = new MemoryStream())
                            {
                                CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, cryptoServiceProvider.CreateDecryptor(), CryptoStreamMode.Write);
                                cryptoStream.Write(dataToDecrypt, 0, dataToDecrypt.Length);
                                cryptoStream.FlushFinalBlock();
                                return memoryStream.ToArray();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new ApplicationException("Unexpected error occured while trying to decrypt the input", ex);
                    }
                }
            }
            throw new ArgumentException("Invalid symmetric key:the key size must me at least: " + (Decryptor.AESInitializationVectorSize / 8).ToString());
        }
    }
}
