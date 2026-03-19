using System.Security.Cryptography;
using System.Text;
using Application.Abstractions.Authentication;
using Konscious.Security.Cryptography;

namespace Infrastructure.Authentication;

internal sealed class PasswordHasher : IPasswordHasher
{
    /// <summary>
    /// Size of the salt in bytes
    /// </summary>
    private const int SaltSize = 16;

    /// <summary>
    /// Size of the hash in bytes
    /// </summary>
    private const int HashSize = 32;

    /// <summary>
    /// Number of threads to use for parallel computation
    /// </summary>
    private const int DegreeOfParallelism = 1;

    /// <summary>
    /// Number of iterations for the Argon2id algorithm
    /// </summary>
    private const int Iterations = 2;

    /// <summary>
    /// Memory size in KB to use
    /// </summary>
    private const int MemorySize = 19456; // 19 MB

    /// <summary>
    /// Generates a secure hash of a password using Argon2id with a random salt
    /// </summary>
    /// <param name="password">The plain text password to hash</param>
    /// <returns>A Base64 string containing the combined salt and hash</returns>
    /// <exception cref="ArgumentNullException">Thrown when password is null</exception>
    public string Hash(string password)
    {
        // Generate a random salt
        byte[] salt = new byte[SaltSize];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        // Create hash
        byte[] hash = HashPassword(password, salt);

        // Combine salt and hash
        byte[] combinedBytes = new byte[salt.Length + hash.Length];
        Array.Copy(salt, 0, combinedBytes, 0, salt.Length);
        Array.Copy(hash, 0, combinedBytes, salt.Length, hash.Length);

        // Convert to base64 for storage
        return Convert.ToBase64String(combinedBytes);
    }

    /// <summary>
    /// Generates a password hash using Argon2id with a specific salt
    /// </summary>
    /// <param name="password">The plain text password</param>
    /// <param name="salt">The salt to use for hashing</param>
    /// <returns>A byte array containing the password hash</returns>
    private byte[] HashPassword(string password, byte[] salt)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = DegreeOfParallelism,
            Iterations = Iterations,
            MemorySize = MemorySize,
        };

        return argon2.GetBytes(HashSize);
    }

    /// <summary>
    /// Verifies if a password matches a stored hash
    /// </summary>
    /// <param name="password">The plain text password to verify</param>
    /// <param name="passwordHash">The stored hash in Base64 format</param>
    /// <returns>True if the password matches the hash, false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown when password or passwordHash are null</exception>
    /// <exception cref="FormatException">Thrown when passwordHash is not in valid Base64 format</exception>
    public bool Verify(string password, string passwordHash)
    {
        // Decode the stored hash
        byte[] combinedBytes = Convert.FromBase64String(passwordHash);

        // Extract salt and hash
        byte[] salt = new byte[SaltSize];
        byte[] hash = new byte[HashSize];
        Array.Copy(combinedBytes, 0, salt, 0, SaltSize);
        Array.Copy(combinedBytes, SaltSize, hash, 0, HashSize);

        // Compute hash for the input password
        byte[] newHash = HashPassword(password, salt);

        // Compare the hashes
        return CryptographicOperations.FixedTimeEquals(hash, newHash);
    }
}
