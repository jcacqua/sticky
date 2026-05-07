Sous Windows 11, pour compiler le projet et créer un .exe , procéder comme suit : 

Ouvrir une fenêtre Powershell 
taper les commandes : 

$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
& $csc /target:winexe /win32icon:app.ico /r:System.dll,System.Windows.Forms.dll,System.Drawing.dll,System.Xml.dll /out:StickyNotes.exe sticky.cs
