namespace CardboardBox.Epub.Management;

public interface IManagementSystem
{
	void Initialize();
	Task Finish();
	Task Add(string filename, Stream content);
}
