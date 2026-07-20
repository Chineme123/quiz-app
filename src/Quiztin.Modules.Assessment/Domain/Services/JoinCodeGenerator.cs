using System.Security.Cryptography;

namespace Quiztin.Modules.Assessment.Domain.Services
{
    /// <summary>
    /// Generates a short, human friendly classroom join code (spec 0008). Six characters
    /// from a 31 character alphabet with the confusable ones removed (no I, L, O, 0, 1), so a
    /// code read off a board or dictated over a call transcribes cleanly. Uses a cryptographic
    /// RNG so codes are not guessable in sequence. Uniqueness is enforced by the DB unique
    /// index on Classroom.JoinCode; the application layer regenerates on the rare collision.
    /// </summary>
    public static class JoinCodeGenerator
    {
        private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

        /// <summary>Length of a generated code; also the DB column max length.</summary>
        public const int Length = 6;

        public static string Generate()
        {
            var chars = new char[Length];
            for (var i = 0; i < Length; i++)
            {
                chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
            }

            return new string(chars);
        }
    }
}
