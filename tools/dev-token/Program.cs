// Dev-only utility: mints an RS256 JWT compatible with ms-forgot-information's validation
// (ADR-008 payload shape), without needing a real iam-service. See ../../TESTING.md.
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

var keysDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "keys");
Directory.CreateDirectory(keysDir);
var privateKeyPath = Path.Combine(keysDir, "private.pem");
var publicKeyPath = Path.Combine(keysDir, "public.pem");

using var rsa = RSA.Create(2048);

if (File.Exists(privateKeyPath))
{
    rsa.ImportFromPem(File.ReadAllText(privateKeyPath));
}
else
{
    File.WriteAllText(privateKeyPath, rsa.ExportRSAPrivateKeyPem());
    File.WriteAllText(publicKeyPath, rsa.ExportSubjectPublicKeyInfoPem());
    Console.WriteLine($"Generated a new RSA key pair in {keysDir}");
}

var options = ParseArgs(args);

var claims = new[]
{
    new Claim(JwtRegisteredClaimNames.Sub, options.ProfileId),
    new Claim(JwtRegisteredClaimNames.Email, options.Email),
    new Claim("role", options.Role)
};

var credentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);

var token = new JwtSecurityToken(
    issuer: options.Issuer,
    audience: options.Audience,
    claims: claims,
    expires: DateTime.UtcNow.AddHours(1),
    signingCredentials: credentials);

var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

Console.WriteLine();
Console.WriteLine("=== Jwt:PublicKey (paste into .env / appsettings) ===");
Console.WriteLine(File.ReadAllText(publicKeyPath));
Console.WriteLine("=== profileId used (register this in the wiremock stub) ===");
Console.WriteLine(options.ProfileId);
Console.WriteLine("=== Authorization header ===");
Console.WriteLine($"Authorization: Bearer {tokenString}");

return;

static DevTokenOptions ParseArgs(string[] args)
{
    var map = new Dictionary<string, string>();
    for (var i = 0; i < args.Length - 1; i += 2)
    {
        map[args[i].TrimStart('-')] = args[i + 1];
    }

    return new DevTokenOptions(
        map.GetValueOrDefault("profile-id", Guid.NewGuid().ToString()),
        map.GetValueOrDefault("email", "parent@example.com"),
        map.GetValueOrDefault("role", "PARENT"),
        map.GetValueOrDefault("issuer", "school-guardian"),
        map.GetValueOrDefault("audience", "school-guardian-clients"));
}

internal sealed record DevTokenOptions(string ProfileId, string Email, string Role, string Issuer, string Audience);
