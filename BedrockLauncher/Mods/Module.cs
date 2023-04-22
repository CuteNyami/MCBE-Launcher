using System.Collections.Generic;
using System.Windows.Documents;

namespace BedrockLauncher.Mods;

public class Module
{
	public string Name { get; set; }

    public string Description { get; set; }

	public Module(string name, string description)
	{
		Name = name;
        Description = description;
	}
}