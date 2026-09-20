<<<<<<< HEAD
# Safe Exam Browser, Version 3.x

Refactored version of Safe Exam Browser for Windows with Chromium as integrated browser engine.

## Requirements

> [!NOTE]  
> Starting with version 3.8.0, Safe Exam Browser for Windows requires a minimum operating system version of **Windows 10 version 1803**.

Safe Exam Browser for Windows requires the prerequisites listed below in order to work correctly. These are automatically installed with the setup bundle and need only be manually installed when using the MSI packages.

* .NET Framework 4.8 Runtime: https://dotnet.microsoft.com/download/dotnet-framework/net48
* Visual C++ 2015-2022 Redistributable: https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist

## Project Status

> [!WARNING]
> **The builds linked below are for testing purposes only.** They may be unstable and should thus _never_ be used in a production environment! Always use the latest, official release version of SEB.

| Aspect            | Status                                                                                                                | Details                                                         |
| ----------------- | --------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------- |
| Development Build | ![Development Build Status](https://sebdev.ethz.ch/api/projects/status/kq78qrjtnpk82ti0?svg=true)                     | https://sebdev.ethz.ch/project/appveyor/seb-win-refactoring     |
| Test Build        | ![Test Build Status](https://ci.appveyor.com/api/projects/status/a56akt9r174570m7?svg=true)                           | https://ci.appveyor.com/project/dbuechel/seb-win-refactoring    |
| Test Run          | ![AppVeyor Tests](https://img.shields.io/appveyor/tests/dbuechel/seb-win-refactoring?logo=appveyor&logoColor=%23ccc)  | https://ci.appveyor.com/project/dbuechel/seb-win-refactoring    |
| Code Coverage     | ![Code Coverage](https://codecov.io/gh/SafeExamBrowser/seb-win-refactoring/branch/master/graph/badge.svg)             | https://codecov.io/gh/SafeExamBrowser/seb-win-refactoring       |
| Issue Status      | ![GitHub Issues](https://img.shields.io/github/issues/safeexambrowser/seb-win-refactoring?logo=github)                | https://github.com/SafeExamBrowser/seb-win-refactoring/issues   |
| Downloads         | ![GitHub All Releases](https://img.shields.io/github/downloads/safeexambrowser/seb-win-refactoring/total?logo=github) | https://github.com/SafeExamBrowser/seb-win-refactoring/releases |
| Development       | ![GitHub Last Commit](https://img.shields.io/github/last-commit/safeexambrowser/seb-win-refactoring?logo=github)      | n/a                                                             |
| Repository Size   | ![GitHub Repo Size](https://img.shields.io/github/repo-size/safeexambrowser/seb-win-refactoring?logo=github)          | n/a                                                             |
=======
Ringkasan: Patch ini menambahkan mode debugging dan fitur dummy proctor, serta melakukan penyeragaman warna ikon sistem untuk konsistensi visual dan keamanan.


<h1 style="font-family: consolas;">Things Added</h1>

- ### Debugging Mode Switch
    - **Keybind: Host + L**
    - Switching between two modes:
        - *Normal*
            - Standard SEB-like behavior: Windows keybinds are disabled, Alt+Tab is handled by SEB, and the display switches to kiosk mode with a black background..
        - *Debug*
            - Disables all SEB locking mechanisms: Kiosk mode is disabled, revealing the normal desktop.
			- All Windows key combinations work again, including Alt+Tab.
			- SEB completely disables the internal keybind handler (TURN OFF KEYBIND HANDLER FROM SEB).
			- All Windows key functions (Win+Tab, Win+D, etc.)
- ### Dummy Proctor System
	- If the configuration requires a proctor session but the proctoring feature is disabled, the proctor icon and warning are still displayed visually.
	- However, this display is dummy; no proctoring function is executed (just the icon/warning display with no action).


<h1 style="font-family: consolas;">Changes (Modifications)</h1>

- ### System Icon Color Standardization
    - All system icons have been reverted to black (#000000).
	- Purpose: Maintain visual consistency with the app's background and reduce the risk of visual information being easily visible or confusing to users; and also improve the security of the display.
    
- ### Dark Theme Discontinued
	-  Dark Theme development will be discontinued for the sake of alignment with other interfaces and system interface stability.
	
- ### Companion window stopped
	- To reduce the amount of resources SEB uses while the program is running, Companion will be removed.
	
- ### VM detector removed
	- Safe Exam Browser no longer tries to detect the virtual machine environment when you run SEB in a virtual machine, even if the "***Allow to run inside Virtual Machine***" configuration is turned off.
	
- ### Flexibility in Setting SEB UI
	- Now you can adjust how some components work, such as animation duration and appearance from the left panel of the Action Center, customize the fake proctor icon, and directly edit the web page.

- ### Configuration Location
	- Just like the previous version, all patcher configurations will be saved in the SafeExamBrowser folder within the Roaming folder.
	  
		 ```C:Users\<PC USERNAME>\AppData\Roaming\SafeExamBrowser\p_cfg```
	 
		 You can easily configure these settings using the "SEB Patcher Tool" program or create the file manually.
>>>>>>> 7c65c42cd2fdf81df8db2b23153634ac8c6c731f
