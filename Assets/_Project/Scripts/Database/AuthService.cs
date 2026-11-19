using System;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using LastSignal.Core;

namespace LastSignal.Database
{
    /// <summary>PBKDF2-HMAC-SHA256 with a random 16-byte salt per user; constant-time comparison.</summary>
    public static class PasswordHasher
    {
        public const int Iterations = 60000;
        private const int SaltBytes = 16;
        private const int HashBytes = 32;

        public static (string hash, string salt, int iterations) Hash(string password)
        {
            var salt = new byte[SaltBytes];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
            return (Convert.ToBase64String(Derive(password, salt, Iterations)), Convert.ToBase64String(salt), Iterations);
        }

        public static bool Verify(string password, string hash, string salt, int iterations)
        {
            if (string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(salt)) return false;
            byte[] expected;
            try
            {
                expected = Convert.FromBase64String(hash);
            }
            catch (FormatException)
            {
                return false;
            }
            var actual = Derive(password ?? string.Empty, Convert.FromBase64String(salt), iterations);
            int diff = expected.Length ^ actual.Length;
            for (int i = 0; i < Math.Min(expected.Length, actual.Length); i++) diff |= expected[i] ^ actual[i];
            return diff == 0;
        }

        private static byte[] Derive(string password, byte[] salt, int iterations)
        {
            using var kdf = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            return kdf.GetBytes(HashBytes);
        }
    }

    /// <summary>The logged-in account for this run.</summary>
    public sealed class UserSession
    {
        public long UserId { get; }
        public string Username { get; }
        public UserRole Role { get; }
        public bool IsAdmin => Role == UserRole.Admin;

        public UserSession(long id, string name, UserRole role)
        {
            UserId = id;
            Username = name;
            Role = role;
        }
    }

    /// <summary>
    /// Registration, login and account rules. Results are localization keys (null = success) so the UI can show
    /// them in either language. Rules: unique username (3–20 letters, digits, _ - .), password ≥ 6 characters,
    /// the last administrator cannot be deleted or demoted, nobody can delete their own account while logged in.
    /// </summary>
    public sealed class AuthService
    {
        private static readonly Regex UsernamePattern = new(@"^[A-Za-z0-9_.\-]{3,20}$");
        private readonly DatabaseManager _db;

        public UserSession Current { get; private set; }
        public bool IsLoggedIn => Current != null;
        public bool IsAdmin => Current?.IsAdmin ?? false;

        public event Action<UserSession> SessionChanged;

        public AuthService(DatabaseManager db) => _db = db;

        public bool NeedsFirstAdmin => _db.IsAvailable && _db.Users.CountAdmins() == 0;

        public static string ValidateUsername(string username)
            => UsernamePattern.IsMatch(username ?? string.Empty) ? null : "auth.err.username_format";

        public static string ValidatePassword(string password)
            => (password ?? string.Empty).Length >= 6 ? null : "auth.err.password_short";

        public string Register(string username, string password, string confirm, UserRole role, out long userId)
        {
            userId = 0;
            if (!_db.IsAvailable) return "auth.err.no_database";
            username = username?.Trim();
            var err = ValidateUsername(username) ?? ValidatePassword(password);
            if (err != null) return err;
            if (password != confirm) return "auth.err.password_mismatch";
            if (_db.Users.FindByName(username) != null) return "auth.err.username_taken";
            var (hash, salt, iterations) = PasswordHasher.Hash(password);
            var language = GameServices.IsReady ? LocalizationService.Code(GameServices.Localization.Current) : "en";
            userId = _db.Users.Create(username, hash, salt, iterations, role, language);
            Log.Info(nameof(AuthService), $"Account created: {username} ({role})");
            return null;
        }

        public string Login(string username, string password)
        {
            if (!_db.IsAvailable) return "auth.err.no_database";
            var user = _db.Users.FindByName(username?.Trim() ?? string.Empty);
            // same message for unknown user and wrong password (no account enumeration)
            if (user == null || !PasswordHasher.Verify(password, user.PasswordHash, user.PasswordSalt, user.HashIterations))
                return "auth.err.invalid_login";
            _db.Users.TouchLogin(user.UserId);
            Current = new UserSession(user.UserId, user.Username, user.Role);
            if (GameServices.IsReady)
            {
                if (user.PreferredLanguage == "bg") GameServices.Localization.SetLanguage(Language.Bulgarian);
                else if (user.PreferredLanguage == "en") GameServices.Localization.SetLanguage(Language.English);
            }
            Log.Info(nameof(AuthService), $"Logged in: {user.Username} ({user.Role})");
            SessionChanged?.Invoke(Current);
            return null;
        }

        public void Logout()
        {
            if (Current == null) return;
            Log.Info(nameof(AuthService), $"Logged out: {Current.Username}");
            Current = null;
            SessionChanged?.Invoke(null);
        }

        /// <summary>Admin CRUD guard: may this user be deleted?</summary>
        public string CanDelete(UserRecord user)
        {
            if (Current != null && user.UserId == Current.UserId) return "auth.err.delete_self";
            if (user.Role == UserRole.Admin && _db.Users.CountAdmins() <= 1) return "auth.err.last_admin";
            return null;
        }

        public string CanChangeRole(UserRecord user, UserRole newRole)
        {
            if (user.Role == UserRole.Admin && newRole != UserRole.Admin && _db.Users.CountAdmins() <= 1) return "auth.err.last_admin";
            return null;
        }

        public string ChangePassword(long userId, string password)
        {
            var err = ValidatePassword(password);
            if (err != null) return err;
            var (hash, salt, iterations) = PasswordHasher.Hash(password);
            return _db.Users.UpdatePassword(userId, hash, salt, iterations) ? null : "auth.err.not_found";
        }

        /// <summary>Test/dev hook: sign in without credentials (used by -startScene runs and play-mode tests).</summary>
        public void UseGuestSession() => Current ??= new UserSession(0, "guest", UserRole.Player);
    }
}
