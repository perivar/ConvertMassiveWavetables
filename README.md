# ConvertMassiveWavetables
##Convert NI Massive Wavetables to Singe Cycle Waveforms and u-he Zebra 2 Wavetables

*This tool takes wavetables extracted from NI Massive and converts them into Single Cycle Waveforms to be used in synthesizers as well as presets for u-he Zebra 2.

The pre-requsites are the extracted wavetables:
1. This can either be done by using the python script created by Lukáš Lalinský
https://gist.github.com/lalinsky/8f2cd9e8f80e62c82af2 

or

2. By using the ExtractMassiveWavetables tool ported by myself
https://github.com/perivar/ExtractMassiveWavetables/releases

*Usage*
```
ConvertMassiveWavetables.exe "path-to-directory-containing-the-extracted-files" "path-to-output-directory"
```
Note! It expects to find the folder 'wt', 'pt' and so on in the directory specified by the first command line argument.


This will create a Converted folder with 'Single Cycle Waveforms' and 'Zebra2 Osc Presets' sub-folders.
Within the first sub-folder you will find the Single Cycle Waveforms and within the second the u-he zebra 2 oscillator preset files.


Enjoy!

Kind Regards,
Per Ivar Nerseth