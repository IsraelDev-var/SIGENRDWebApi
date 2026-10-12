using System;
using System.Security.Cryptography;
class Program {
    static void Main() {
        using var rsa = RSA.Create(3072);
        var bytes = rsa.ExportPkcs8PrivateKey();
        var b64 = Convert.ToBase64String(bytes);
        Console.WriteLine(b64);
    }
}
