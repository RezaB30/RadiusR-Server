sc create RadiusRServer binpath= "%~dp0RadiusR Server.exe" displayname= "RadiusR Server" start= auto && sc description RadiusRServer "RadiusR Server manages radius requests." && net start RadiusRServer && sc sdset RadiusRServer D:(A;;CCLCSWRPWPDTLOCRRC;;;SY)(A;;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;BA)(A;;CCLCSWLOCRRC;;;IU)(A;;CCLCSWLOCRRC;;;SU)(A;;CCLCSWLOCRRCRPWPDT;;;AU)
pause
