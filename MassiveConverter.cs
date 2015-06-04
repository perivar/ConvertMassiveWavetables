using System;
using System.Text;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using System.Xml.Linq;
using ExtractMassiveWavetables; // for the massive mapping methods
using Wav2Zebra2Osc; // for the zebra export methods

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
			
			// generate the sine data (used for zebra morphing)
			var sineData = OscillatorGenerator.Sine();
			
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
							SplitIntoSingleCycleWaveforms(inputDirectory, wavFile, singleCycleLength, map, outputDirectory, sineData);
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
		
		static void SplitIntoSingleCycleWaveforms(string baseDirectory, string wavPath, int singleCycleLength, Dictionary<string, MassiveMapElement> map, string outputDirectory, float[] sineData) {
			
			// use path after the base dir as map key
			string mapKey = IOUtils.GetRightPartOfPath(wavPath, baseDirectory + Path.DirectorySeparatorChar);
			
			#region Output Correct Filenames
			// determine correct filename
			if (map.ContainsKey(mapKey)) {
				var mapElement = map[mapKey];
				if (!mapElement.GroupName.Equals("")
				    && !mapElement.CorrectFileName.Equals("")) {
					
					// create single cycle directory
					string singleCycleDirectory = Path.Combine(outputDirectory, "Single Cycle Waveforms", StringUtils.MakeValidFileName(mapElement.GroupName));
					
					if (!Directory.Exists(singleCycleDirectory)) {
						Directory.CreateDirectory(singleCycleDirectory);
					}

					// create preset file directory
					string presetFileDirectory = Path.Combine(outputDirectory, "Zebra2 Osc Presets", StringUtils.MakeValidFileName(mapElement.GroupName));
					
					if (!Directory.Exists(presetFileDirectory)) {
						Directory.CreateDirectory(presetFileDirectory);
					}
					
					// read audio data as mono
					int sampleRate = -1;
					int bitsPerSample = -1;
					long byteLength = -1;
					float[] audioData = BassProxy.ReadMonoFromFile(wavPath, out sampleRate, out bitsPerSample, out byteLength, BassProxy.MonoSummingType.Mix);
					
					// temporary storage for the 128 single cycle samples
					var waveforms = new List<float[]>();
					int cycleCount = 1;
					
					// find each single cycle waveforms
					for (int i = 0; i < audioData.Length; i += singleCycleLength) {
						var singleCycleData = new float[singleCycleLength];
						Array.Copy(audioData, i, singleCycleData, 0, singleCycleLength);
						
						// output the corrected filenames
						string newFileName = string.Format("{0}_{1}_{2}.wav", mapElement.GroupIndex, mapElement.CorrectFileName, cycleCount);
						string newFilePath = Path.Combine(singleCycleDirectory, newFileName);
						
						Console.Out.WriteLine("Creating file {0}.", newFilePath);
						BassProxy.SaveFile(singleCycleData, newFilePath, 1, sampleRate, bitsPerSample);
						
						// resample to 128 samples for u-he zebra conversion
						float[] singeCycle128 = MathUtils.Resample(singleCycleData, 128);
						
						// zebra only supports 16 slots
						if (cycleCount < 17) {
							waveforms.Add(singeCycle128);
						} else {
							Console.Out.WriteLine("Zebra only support 16 waves in it's wavetables.");
							break;
						}
						cycleCount++;
					}
					
					#region Save a non-morphed and a morphed version of the zebra preset
					
					// single cycle waveform array for non-morphed data
					float[][] soundData = MathUtils.CreateJaggedArray<float[][]>(16, 128);

					// single cycle waveform arrays for morphed daya
					float[][] morphData = MathUtils.CreateJaggedArray<float[][]>(16, 128);
					
					// distribute the waves found evenly within the 16 available slots
					var evenDistribution = MathUtils.GetEvenDistributionPreferEdges(waveforms.Count, 16);
					var indices = MathUtils.IndexOf(evenDistribution, 1);
					
					if (waveforms.Count() == 1) {
						// set sine data to last element (for morphing later)
						Array.Copy(sineData, 0, morphData[15], 0, 128);
					}
					
					if (indices.Count() == waveforms.Count()) {
						var enabledMorphSlots = new bool[16];
						var enabledSoundSlots = new bool[16];
						
						for (int i = 0; i < waveforms.Count; i++) {
							// add the waves successively to the normal sound data
							Array.Copy(waveforms[i], 0, soundData[i], 0, 128);
							enabledSoundSlots[i] = true;
							
							// spread out the waves to later be morphed
							int index = indices[i];
							Array.Copy(waveforms[i], 0, morphData[index], 0, 128);
							enabledMorphSlots[index] = true; // before morphing this is used to tell what slots are loaded
						}
						
						// morph between the added waveforms (slots)
						Zebra2OSCPreset.MorphAllSegments(enabledMorphSlots, ref morphData);
						
						// before writing the file ensure all of the morhp slots are enabled
						for (int j = 0; j < 16; j++) {
							enabledMorphSlots[j] = true;
						}

						// save the non-morphed u-he zebra preset
						string zebraPreset = string.Format("{0}_{1}.h2p", mapElement.GroupIndex, mapElement.CorrectFileName);
						string zebraPresetFilePath = Path.Combine(presetFileDirectory, zebraPreset);
						Zebra2OSCPreset.Write(soundData, enabledSoundSlots, zebraPresetFilePath);

						// save the morphed u-he zebra preset
						string zebraMorphPreset = string.Format("{0}_{1}_Morph.h2p", mapElement.GroupIndex, mapElement.CorrectFileName);
						string zebraMorphPresetFilePath = Path.Combine(presetFileDirectory, zebraMorphPreset);
						Zebra2OSCPreset.Write(morphData, enabledMorphSlots, zebraMorphPresetFilePath);
						#endregion
						
					} else {
						// this should never happen
						Console.Out.WriteLine("Failed! Could not set wavetables correctly.");
					}
				}
			}
			#endregion
		}
	}
	
	class WaveTableSlot {
		float[] soundData;
		bool enabled = false;

		public bool Enabled {
			get {
				return enabled;
			}
			set {
				enabled = value;
			}
		}
		
		public float[] SoundData {
			get {
				return soundData;
			}
			set {
				soundData = value;
			}
		}
		
		public WaveTableSlot(float[] soundData, bool enabled) {
			this.SoundData = soundData;
			this.Enabled = enabled;
		}
	}
}
