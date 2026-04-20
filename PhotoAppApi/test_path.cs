using System;
using System.IO;

class Program
{
    static void Main()
    {
        var guid = Guid.NewGuid().ToString();
        var fileName = "../../../test.png";
        var uniqueName = guid + "_" + fileName;
        var folder = "C:\\test\\images";
        var combined = Path.Combine(folder, uniqueName);
        var fullPath = Path.GetFullPath(combined);
        Console.WriteLine("Combined: " + combined);
        Console.WriteLine("Full Path: " + fullPath);
    }
}
