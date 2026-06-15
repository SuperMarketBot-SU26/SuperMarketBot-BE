// Generate PBKDF2 hash matching SmartMarketBot.AuthService.HashPassword
using System;
using System.Security.Cryptography;

const string password = "123456";
const int iterations = 100_000;
var salt = RandomNumberGenerator.GetBytes(16);
var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, 32);
Console.WriteLine($"pbkdf2${iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}");
