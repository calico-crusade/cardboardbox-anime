using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;

namespace CardboardBox.Anime.Auth;

using Http;

public static class Extensions
{
	public static readonly Random RandomInstance = new();

	public static Dictionary<string, string> GetParameters(this IConfiguration config, string section)
	{
		var dic = new Dictionary<string, string>();
		config.GetSection(section).Bind(dic);
		return dic;
	}

	public static Task<T?> Post<T>(this IApiService api, string url, (string, string)[] data, Action<HttpRequestMessage>? config = null)
	{
		return api.Create(url, "POST")
			.With(config)
			.Body(data)
			.Result<T>();
	}

	public static IServiceCollection AddOAuth(this IServiceCollection services, IConfiguration config)
	{
		services
			.AddTransient<ITokenService, TokenService>()
			.AddTransient<IOAuthService, OAuthService>()
			.AddAuthentication(opt =>
			{
				opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
				opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
				opt.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
			})
			.AddJwtBearer(opt =>
			{
				opt.SaveToken = true;
				opt.RequireHttpsMetadata = false;
				opt.TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuer = true,
					ValidateAudience = true,
					ValidAudience = config["OAuth:Audience"],
					ValidIssuer = config["OAuth:Issuer"],
					IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["OAuth:Key"]))
				};
			});
		return services;
	}

	public static string? GroupId(this ClaimsPrincipal principal) => principal.Claim(ClaimTypes.GroupSid);

	public static string? GroupId(this ControllerBase ctrl) => ctrl?.User?.GroupId();

	public static string? Claim(this ClaimsPrincipal principal, string claim)
	{
		return principal?.FindFirst(claim)?.Value;
	}

	public static string? Claim(this ControllerBase ctrl, string claim)
	{
		if (ctrl.User == null) return null;
		return ctrl.User.Claim(claim);
	}

	public static TokenUser? UserFromIdentity(this ControllerBase ctrl)
	{
		if (ctrl.User == null) return null;

		return ctrl.User.UserFromIdentity();
	}

	public static TokenUser? UserFromIdentity(this ClaimsPrincipal principal)
	{
		if (principal == null) return null;

		var getClaim = (string key) => principal.Claim(key) ?? "";

		var id = getClaim(ClaimTypes.NameIdentifier);
		if (string.IsNullOrEmpty(id)) return null;

		return new TokenUser
		{
			Id = id,
			Nickname = getClaim(ClaimTypes.Name),
			Email = getClaim(ClaimTypes.Email),
			Avatar = getClaim(ClaimTypes.UserData),
			Provider = getClaim(ClaimTypes.PrimarySid),
			ProviderId = getClaim(ClaimTypes.PrimaryGroupSid)
		};
	}

}