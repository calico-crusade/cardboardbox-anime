using Microsoft.AspNetCore.Authorization;

namespace CardboardBox.Anime;

public class AdminAuthorizeAttribute : AuthorizeAttribute
{
	public AdminAuthorizeAttribute()
	{
		Roles = "Admin";
	}
}
