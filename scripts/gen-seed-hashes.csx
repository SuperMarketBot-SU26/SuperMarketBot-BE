using System.Security.Cryptography;

// Re-generate PBKDF2 hashes cho seed accounts (admin/staff/member1)
// Dùng đúng format của AuthService.HashPassword

string HashPwd(string password)
{
    const int iterations = 100_000;
    var salt = RandomNumberGenerator.GetBytes(16);
    var hash = Rfc2898DeriveBytes.Pbkdf2(
        password, salt, iterations, System.Security.Cryptography.HashAlgorithmName.SHA256, 32);
    return $"pbkdf2${iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
}

var accounts = new Dictionary<string, string>
{
    ["admin@smartmarket.local"]  = "Admin@123",
    ["staff@smartmarket.local"]  = "Staff@123",
    ["member1@smartmarket.local"] = "Member@123"
};

foreach (var (email, pwd) in accounts)
{
    Console.WriteLine($"-- {email} / {pwd}");
    Console.WriteLine(HashPwd(pwd));
}
