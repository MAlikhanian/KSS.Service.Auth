using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace KSS.Helper
{
    public class Authentication
    {
        public static string PasswordHash(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public static bool PasswordVerify(string hash, string inputPassword)
        {
            return BCrypt.Net.BCrypt.Verify(inputPassword, hash);
        }

        public static string JwtTokenGenerate(string jwtSecret, string username, int validMinutes = 60)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim("userName", username)
            };

            var secret = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

            var credentials = new SigningCredentials(secret, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken
            (
                issuer: null,
                audience: null,
                claims: claims,
                expires: DateTime.Now.AddMinutes(validMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
