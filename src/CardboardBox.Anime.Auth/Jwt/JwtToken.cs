using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CardboardBox.Anime.Auth
{
    public class JwtToken
    {
        private List<Claim> _claims = new List<Claim>();

        public string? this[string key]
        {
            get => _claims.Find(t => t.Type == key)?.Value;
            set
            {
                var claim = _claims.Find(t => t.Type == key);

                if (claim != null)
                    _claims.Remove(claim);

                _claims.Add(new Claim(key, value ?? ""));
            }
        }

        public string? Email
        {
            get => this[ClaimTypes.Email];
            set => this[ClaimTypes.Email] = value;
        }

        public SymmetricSecurityKey Key { get; }

        public string Issuer { get; set; } = "";
        public string Audience { get; set; } = "";
        public int ExpireyMinutes { get; set; } = 10080;
        public string SigningAlgorithm { get; set; } = SecurityAlgorithms.HmacSha256;

        public JwtToken(SymmetricSecurityKey key)
        {
            Key = key;
        }
        public JwtToken(SymmetricSecurityKey key, string token)
        {
            Key = key;
            Read(token);
        }
        public JwtToken(string key)
        {
            Key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(key));
        }
        public JwtToken(string key, string token)
        {
            Key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(key));
            Read(token);
        }

        public JwtToken AddClaim(params Claim[] claims)
        {
            foreach (var claim in claims)
                _claims.Add(claim);
            return this;
        }
        public JwtToken AddClaim(string key, string value)
        {
            return AddClaim(new Claim(key, value));
        }
        public JwtToken AddClaim(params (string, string)[] claims)
        {
            foreach (var claim in claims)
                AddClaim(claim.Item1, claim.Item2);

            return this;
        }
        public JwtToken Expires(int minutes)
        {
            ExpireyMinutes = minutes;
            return this;
        }
        public JwtToken SetEmail(string email)
        {
            Email = email;
            return this;
        }
        public JwtToken SetIssuer(string issuer)
        {
            Issuer = issuer;
            return this;
        }
        public JwtToken SetAudience(string audience)
        {
            Audience = audience;
            return this;
        }

        public string Write()
        {
            this[JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString();

            var token = new JwtSecurityToken
            (
                issuer: Issuer,
                audience: Audience,
                claims: _claims,
                expires: DateTime.UtcNow.AddMinutes(ExpireyMinutes),
                signingCredentials: new SigningCredentials(Key, SigningAlgorithm)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private void Read(string token)
        {
            var handler = new JwtSecurityTokenHandler();

            var validations = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                IssuerSigningKey = Key,
                ValidateIssuerSigningKey = true
            };

            _claims = handler.ValidateToken(token, validations, out SecurityToken ts).Claims.ToList();

            var t = (JwtSecurityToken)ts;
            Issuer = t.Issuer;
            Audience = t.Audiences.First();
            ExpireyMinutes = (t.ValidTo - DateTime.Now).Minutes;
            SigningAlgorithm = t.SignatureAlgorithm;
        }
    }
}
