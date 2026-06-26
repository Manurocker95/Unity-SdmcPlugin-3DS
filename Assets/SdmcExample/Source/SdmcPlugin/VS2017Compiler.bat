call "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\Common7\Tools\VsDevCmd.bat"
msbuild SdmcPlugin.vcxproj /p:Configuration=Release /p:Platform=CTR /p:VisualStudioVersion=14.0 /p:PlatformToolset=v140
pause