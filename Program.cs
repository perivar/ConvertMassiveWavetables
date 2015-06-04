using System;

namespace ConvertMassiveWavetables
{
	class Program
	{
		const string _version = "0.4";
		
		public static void Main(string[] args)
		{
			if (args.Length < 2) {
				System.Console.Out.WriteLine("Convert NI Massive Wavetables (Extracted from tables.dat)");
				System.Console.Out.WriteLine("into Single Cycle Waveforms and u-he Zebra 2 Wavetables.");
				System.Console.Out.WriteLine("Version {0}", _version);
				System.Console.Out.WriteLine();
				System.Console.Out.WriteLine("Usage ConvertMassiveWavetables.exe <path-to-extracted-massive-content> <path-to-output-directory>");
				return;
			}
			
			if (!MassiveConverter.ConvertMassiveDirectory(args[0], args[1])) {
				System.Console.WriteLine("Massive's extracted content not found!");
			}
		}
	}
}