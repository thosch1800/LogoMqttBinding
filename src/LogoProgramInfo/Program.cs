using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace LogoProgramInfo
{
  internal class Program
  {
    private static void Main(string[] args) => new Program().Run(args);

    private void Run(string[] args)
    {
      if (args.Length < 1)
      {
        Console.WriteLine("usage: LogoProgramInfo myLogoProgram.lsc");
        return;
      }

      var filePath = args[0];
      var text = File.ReadAllText(filePath);

      while (true)
      {
        Console.Clear();

        var inputs = FindInputs(text);
        var outputs = FindOutputs(text);

        var variables = new List<NetworkVariable>();
        foreach (var input in inputs)
          variables.Add(FindVirtualMemory(text, input));
        foreach (var output in outputs)
          variables.Add(FindVirtualMemory(text, output));

        foreach (var variable in variables)
          //Console.WriteLine($"{variable.Name} {variable.VB}");
          Console.WriteLine(variable);
      }
    }


    private static IEnumerable<NetworkVariable> FindInputs(string text) => FindBlocks(text, "\0[\u0003|\u0004](?<Name>NI.+)ppq");
    private static IEnumerable<NetworkVariable> FindOutputs(string text) => FindBlocks(text, "\0[\u0003|\u0004](?<Name>NQ.+)ppq");

    private static IEnumerable<NetworkVariable> FindBlocks(string input, string pattern)
    {
      foreach (Match match in Regex.Matches(input, pattern))
        yield return new NetworkVariable(match.Groups["Name"].Value, string.Empty, match.Value, match.Index);
    }

    private static NetworkVariable FindVirtualMemory(string text, NetworkVariable variable)
    {
      var modified = variable with { Source = variable };

      var match = Regex.Match(text.Substring(variable.Index), "(?<VB>V\\d+\\.\\d)ppq");
      if (match.Success)
        modified = modified with
        {
          VB = match.Groups["VB"].Value,
          Index = match.Index,
          Matched = match.Value,
        };

      return modified;
    }

    internal record NetworkVariable(string Name, string VB, string Matched, int Index)
    {
      public NetworkVariable Source { get; set; }
    }
  }
}