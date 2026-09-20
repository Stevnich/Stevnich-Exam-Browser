# **TO INITIALIZE THE DEFAULT CONFIGURATION (SEB Configuration Tool) DataValues,** 
### (These file is to set the default value per each component property, when user open SEB Configuation Tool for the first time):
it's inside the SEBSettings.cs
```
(seb-win-refactoring-master\SebWindowsConfig\SEBSettings.cs)
```

# TO MANAGE CONFIGURATION DATA 
### (The default SEB configuration use, even if the user didn't create a configuration file using SEB Config Tool, or even touch it):
it's inside the DataValue.cs
```
(seb-win-refactoring-master\SafeExamBrowser.Configuration\ConfigurationData\DataValues.cs)
```

# TO MAKE DummyProctor Listens to Enable Proctor Configuration 
### (If user launch SafeExamBrowser using a configuration that enable the proctor, DummyProctor will automatically displayed ):
it's inside the DisclaimerOperation.cs
```
(seb-win-refactoring-master\SafeExamBrowser.Runtime\Operations\Session\DisclaimerOperation.cs)
```
