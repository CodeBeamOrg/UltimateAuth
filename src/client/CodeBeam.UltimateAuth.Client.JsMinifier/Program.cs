using NUglify;
using System.Text;

if (args.Length < 2)
{
    Console.Error.WriteLine("Missing input/output arguments.");
    Environment.Exit(1);
}

var input = args[0];
var output = args[1];

Console.WriteLine($"Minifying: {input}");
Console.WriteLine($"Output   : {output}");

if (!File.Exists(input))
{
    Console.Error.WriteLine($"Input file not found: {input}");
    Environment.Exit(1);
}

var js = File.ReadAllText(input, Encoding.UTF8);
var result = Uglify.Js(js);

if (result.HasErrors)
{
    foreach (var error in result.Errors)
        Console.Error.WriteLine(error);

    Environment.Exit(1);
}

File.WriteAllText(output, result.Code!, Encoding.UTF8);

Console.WriteLine("✔ uauth.min.js generated successfully");
