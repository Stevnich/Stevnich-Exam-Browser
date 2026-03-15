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
