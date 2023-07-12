// See https://aka.ms/new-console-template for more information
using System.Numerics;
using System.Text;
using System.Web;

const string Vehicles = "Vehicles";
const string Characters = "Characters";

string pathPictures = "C:\\Repos\\LegoDimensions.wiki\\.attachments";
string pathWiki = "C:\\Repos\\LegoDimensions.wiki";
string wikiAbsolutePath = "https://github.com/Ellerbach/LegoDimensions/wiki/.attachments";

List<string> listAllPictures = new List<string>();
StringBuilder sb;

// First, let's have all the images and ensure the folders are created and clean

foreach (var file in Directory.GetFiles(pathPictures))
{
    listAllPictures.Add(file.Substring(file.LastIndexOf(Path.DirectorySeparatorChar) + 1));
}

//if (Directory.Exists(Path.Combine(pathWiki, Vehicles)))
//{
//    Directory.Delete(Path.Combine(pathWiki, Vehicles), true);
//}

//Directory.CreateDirectory(Path.Combine(pathWiki, Vehicles));

//if (Directory.Exists(Path.Combine(pathWiki, Characters)))
//{
//    Directory.Delete(Path.Combine(pathWiki, Characters), true);
//}

//Directory.CreateDirectory(Path.Combine(pathWiki, Characters));

// Then , let's create the vehicles and characters pages

string[] themes = LegoDimensions.Tag.Vehicle.Vehicles.Select(m => m.World).Distinct().ToArray();
foreach (string theme in themes)
{
    if (theme == "Unknown")
    {
        continue;
    }

    var models = LegoDimensions.Tag.Vehicle.Vehicles.Where(m => m.World == theme).ToArray();
    sb = new StringBuilder();
    sb.Append($"# Vehicles - {theme}\r\n\r\n");
    sb.Append("You will find all the vehicles for this theme. The title contains the ID and the name.\r\n");

    foreach (var model in models)
    {
        sb.Append($"\r\n## {model.Id}-{model.Name}\r\n\r\n");
        sb.Append($"Rebuild: {model.Rebuild}\r\n");

        sb.Append("\r\n");

        var pic = listAllPictures.Where(m => m.StartsWith($"{model.Id}-")).FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(pic))
        {
            sb.Append($"![{model.Id}-{model.Name}]({wikiAbsolutePath}/{HttpUtility.UrlPathEncode(pic)})\r\n\r\n");
        }

        sb.Append("Abilities:\r\n\r\n");

        foreach (var cap in model.Abilities)
        {
            sb.Append($"* {cap}\r\n");
        }
    }

    File.WriteAllText(Path.Combine(pathWiki, $"{Vehicles} {theme.Replace(":", "")}.md"), sb.ToString());
}

// And the index of vehicles

sb = new StringBuilder();
sb.Append("# List of all the vehicle themes\r\n\r\n");
foreach (string theme in themes)
{
    if (theme == "Unknown")
    {
        continue;
    }

    sb.Append($"* [{theme}]({HttpUtility.UrlPathEncode($"{Vehicles} {theme.Replace(":", "")}")})\r\n");
}

File.WriteAllText(Path.Combine(pathWiki, "All vehicle themes.md"), sb.ToString());

themes = LegoDimensions.Tag.Character.Characters.Select(m => m.World).Distinct().ToArray();
foreach (string theme in themes)
{
    if (theme == "Unknown")
    {
        continue;
    }

    var models = LegoDimensions.Tag.Character.Characters.Where(m => m.World == theme).ToArray();
    sb = new StringBuilder();
    sb.Append($"# Characters - {theme}\r\n\r\n");
    sb.Append("You will find all the characters for this theme. The title contains the ID and the name.\r\n");

    foreach (var model in models)
    {
        sb.Append($"\r\n## {model.Id}-{model.Name}\r\n\r\n");

        var pic = listAllPictures.Where(m => m.StartsWith($"{model.Id}-")).FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(pic))
        {
            sb.Append($"![{model.Id}-{model.Name}]({wikiAbsolutePath}/{HttpUtility.UrlPathEncode(pic)})\r\n\r\n");
        }

        sb.Append("Abilities:\r\n\r\n");

        foreach (var cap in model.Abilities)
        {
            sb.Append($"* {cap}\r\n");
        }
    }

    File.WriteAllText(Path.Combine(pathWiki, $"{Characters} {theme.Replace(":", "")}.md"), sb.ToString());
}

// And the index of characters

sb = new StringBuilder();
sb.Append("# List of all the characters themes\r\n\r\n");
foreach (string theme in themes)
{
    if (theme == "Unknown")
    {
        continue;
    }

    sb.Append($"* [{theme}]({HttpUtility.UrlPathEncode($"{Characters} {theme.Replace(":", "")}")})\r\n");
}

File.WriteAllText(Path.Combine(pathWiki, "All characters themes.md"), sb.ToString());

// Third, let's create the list of all characters and vehicles
sb = new StringBuilder();
sb.Append($"# List of all known Vehicles\r\n\r\n");

sb.Append("| Id | Name | Rebuild | World | Characteristics |\r\n");
sb.Append("| --- | --- | --- | --- | --- |\r\n");
foreach (var vec in LegoDimensions.Tag.Vehicle.Vehicles)
{
    if (vec.World == "Unknown")
    {
        continue;
    }

    sb.Append($"|{vec.Id}|{vec.Name}|{vec.Rebuild}|[{vec.World}]({HttpUtility.UrlPathEncode($"{Vehicles} {vec.World}")})|{string.Join(",", vec.Abilities)}|\r\n");
}

File.WriteAllText(Path.Combine(pathWiki, "All known vehicles.md"), sb.ToString());

sb = new StringBuilder();
sb.Append($"# List of all known Characters\r\n\r\n");

sb.Append("| Id | Name | World | Characteristics |\r\n");
sb.Append("| --- | --- | --- | --- |\r\n");
foreach (var car in LegoDimensions.Tag.Character.Characters)
{
    if (car.World == "Unknown")
    {
        continue;
    }

    sb.Append($"|{car.Id}|{car.Name}|[{car.World}]({HttpUtility.UrlPathEncode($"{Characters} {car.World}")})|{string.Join(",", car.Abilities)}|\r\n");
}

File.WriteAllText(Path.Combine(pathWiki, $"All known characters.md"), sb.ToString());

// Now let's create per ability the list of vehicles and characters

List<string> abilities = new List<string>();
foreach (var ability in LegoDimensions.Tag.Vehicle.Vehicles.Select(m => m.Abilities))
{
    abilities.AddRange(ability);
}

abilities = abilities.Distinct().ToList();

sb = new StringBuilder();
sb.Append($"# List of all abilities and the associates vehicles\r\n");

foreach (var ability in abilities)
{
    if (ability == string.Empty || ability == "Unknown")
    {
        continue;
    }

    sb.Append($"\r\n## {ability}\r\n\r\n");
    var vecs = LegoDimensions.Tag.Vehicle.Vehicles.Where(m => m.Abilities.Contains(ability));
    foreach (var vec in vecs)
    {
        if (ability.Contains(","))
        {
            Console.WriteLine($"Warning, ability {ability} is multiple, fix me in vehicle {vec.Id}");
        }

        sb.Append($"* {vec.Id}-{vec.Name}, {vec.Rebuild}, [{vec.World}]({HttpUtility.UrlPathEncode($"{Vehicles} {vec.World}")})\r\n");
    }
}

File.WriteAllText(Path.Combine(pathWiki, $"{Vehicles} characteristics.md"), sb.ToString());

abilities = new List<string>();
foreach (var ability in LegoDimensions.Tag.Character.Characters.Select(m => m.Abilities))
{
    abilities.AddRange(ability);
}

abilities = abilities.Distinct().ToList();
sb = new StringBuilder();
sb.Append($"# List of all abilities and the associates vehicles\r\n");

foreach (var ability in abilities)
{
    if (ability == string.Empty || ability == "Unknown")
    {
        continue;
    }

    sb.Append($"\r\n## {ability}\r\n\r\n");
    var cars = LegoDimensions.Tag.Character.Characters.Where(m => m.Abilities.Contains(ability));
    foreach (var car in cars)
    {
        if (ability.Contains(","))
        {
            Console.WriteLine($"Warning, ability {ability} is multiple, fix me in character {car.Id}");
        }

        sb.Append($"* {car.Id}-{car.Name}, [{car.World}]({HttpUtility.UrlPathEncode($"{Characters} {car.World}")})\r\n");
    }
}

File.WriteAllText(Path.Combine(pathWiki, $"{Characters} characteristics.md"), sb.ToString());