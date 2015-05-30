using System;
using System.Text;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using ExtractMassiveWavetables; // for the massive mapping methods
using CommonUtils;
using CommonUtils.Audio;

namespace ConvertMassiveWavetables
{
	/// <summary>
	/// Description of MassiveConverter.
	/// </summary>
	public static class MassiveConverter
	{
		public static bool ConvertMassiveDirectory(string inputDirectory, string outputDirectory) {
			
			// init the audio system
			var audioSystem = BassProxy.Instance;
			
			string massiveWavetablesPath = Path.Combine(inputDirectory, "wt");
			if (Directory.Exists(massiveWavetablesPath)) {
				
				var map = MassiveMapping.ReadMassiveMapping("massive_map.csv");

				// find all the wtinfo.txt text files
				var files = IOUtils.GetFilesRecursive(massiveWavetablesPath, "wtinfo.txt");
				foreach (var file in files) {

					int singleCycleLength = ReadSingleCycleLength(file);
					if (singleCycleLength > 0) {
						
						// find associated wave files
						string dirPath = Path.GetDirectoryName(file);
						var wavFiles = IOUtils.GetFilesRecursive(dirPath, "*.wav");
						
						// how to deal with more than one wave?
						// that doesn't seem to happen - so we are fine!
						int count = 0;
						foreach (var wavFile in wavFiles) {
							SplitIntoSingleCycleWaveforms(inputDirectory, wavFile, singleCycleLength, map, outputDirectory);
							if (count > 0) {
								Console.Out.WriteLine("Wops, I don't think this is supposed to happen.");
							}
							count++;
						}
					}
				}
				
				return true;
				
			} else {
				return false;
			}
		}
		
		static int ReadSingleCycleLength(string filePath) {
			
			// ensure that the wtinfo.txt file includes SingleWAV=x element
			foreach(var line in File.ReadLines(filePath, Encoding.Default)
			        .Where(l => !String.IsNullOrEmpty(l)))
			{
				if (line.StartsWith("SingleWAV=", StringComparison.Ordinal)) {
					
					Match match = Regex.Match(line, @"SingleWAV=([0-9]+)$",
					                          RegexOptions.IgnoreCase);

					if (match.Success) {
						string singleCycleString = match.Groups[1].Value;
						int singleCycleLength = NumberUtils.IntTryParse(singleCycleString, -1);
						return singleCycleLength;
					}
					break;
				}
			}
			return -1;
		}
		
		static void SplitIntoSingleCycleWaveforms(string baseDirectory, string wavPath, int singleCycleLength, Dictionary<string, MassiveMapElement> map, string outputDirectory) {
			
			// use path after the base dir as map key
			string mapKey = IOUtils.GetRightPartOfPath(wavPath, baseDirectory + Path.DirectorySeparatorChar);
			
			#region Output Correct Filenames
			// determine correct filename
			if (map.ContainsKey(mapKey)) {
				var mapElement = map[mapKey];
				if (!mapElement.GroupName.Equals("")
				    && !mapElement.CorrectFileName.Equals("")) {
					
					// group directory
					string groupDirectory = Path.Combine(outputDirectory, "Single Cycle Waveforms", StringUtils.MakeValidFileName(mapElement.GroupName));
					
					if (!Directory.Exists(groupDirectory)) {
						Directory.CreateDirectory(groupDirectory);
					}
					
					// read audio data as mono
					int sampleRate = -1;
					int bitsPerSample = -1;
					long byteLength = -1;
					float[] audioData = BassProxy.ReadMonoFromFile(wavPath, out sampleRate, out bitsPerSample, out byteLength, BassProxy.MonoSummingType.Mix);
					
					// copy into new single cycle waveform arrays
					int cycleCount = 1;
					for (int i = 0; i < audioData.Length; i += singleCycleLength) {
						var singleCycleData = new float[singleCycleLength];
						Array.Copy(audioData, i, singleCycleData, 0, singleCycleLength);
						
						// output the corrected filenames
						string newFileName = string.Format("{0}_{1}_{2}.wav", mapElement.GroupIndex, mapElement.CorrectFileName, cycleCount);
						string newFilePath = Path.Combine(groupDirectory, newFileName);
						Console.Out.WriteLine("Creating file {0}.", newFilePath);

						BassProxy.SaveFile(singleCycleData, newFilePath, 1, sampleRate, bitsPerSample);
						cycleCount++;
					}
				}
			}
			#endregion
			
		}
	}
}
