namespace CardboardBox.Epub.Metadata;

public class Creator
{
	public const string ROLE_AUTHOR = "aut";
	public const string ROLE_ILLIUSTRATOR = "ill";
	public const string ROLE_TRANSLATOR = "trl";
	public const string ROLE_EDITOR = "edt";

	public string Name { get; set; }
	public string FileAs { get; set; }
	public string Role { get; set; }

	public Creator(string name, string fileAs, string role)
	{
		Name = name;
		FileAs = fileAs;
		Role = role;
	}
}
