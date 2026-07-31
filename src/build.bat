@echo off
rem Builds NumixCursorsManager.exe with all cursor files embedded as resources,
rem making the exe fully self-contained (no cursors/ folder needed at runtime).

set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe

%CSC% /out:NumixCursorsManager.exe /target:winexe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /win32manifest:app.manifest "/win32icon:..\assets\logo.ico" ^
/resource:cursors\dark\static\crosshair.cur,NumixCursors.cursors.dark.static.crosshair.cur ^
/resource:cursors\dark\static\default.cur,NumixCursors.cursors.dark.static.default.cur ^
/resource:cursors\dark\static\fleur.cur,NumixCursors.cursors.dark.static.fleur.cur ^
/resource:cursors\dark\static\help.cur,NumixCursors.cursors.dark.static.help.cur ^
/resource:cursors\dark\static\not-allowed.cur,NumixCursors.cursors.dark.static.not-allowed.cur ^
/resource:cursors\dark\static\pencil.cur,NumixCursors.cursors.dark.static.pencil.cur ^
/resource:cursors\dark\static\pointer.cur,NumixCursors.cursors.dark.static.pointer.cur ^
/resource:cursors\dark\static\size_bdiag.cur,NumixCursors.cursors.dark.static.size_bdiag.cur ^
/resource:cursors\dark\static\size_fdiag.cur,NumixCursors.cursors.dark.static.size_fdiag.cur ^
/resource:cursors\dark\static\size_hor.cur,NumixCursors.cursors.dark.static.size_hor.cur ^
/resource:cursors\dark\static\size_ver.cur,NumixCursors.cursors.dark.static.size_ver.cur ^
/resource:cursors\dark\static\text.cur,NumixCursors.cursors.dark.static.text.cur ^
/resource:cursors\dark\static\up-arrow.cur,NumixCursors.cursors.dark.static.up-arrow.cur ^
/resource:cursors\dark\animated\progress.ani,NumixCursors.cursors.dark.animated.progress.ani ^
/resource:cursors\dark\animated\wait.ani,NumixCursors.cursors.dark.animated.wait.ani ^
/resource:cursors\light\static\crosshair.cur,NumixCursors.cursors.light.static.crosshair.cur ^
/resource:cursors\light\static\default.cur,NumixCursors.cursors.light.static.default.cur ^
/resource:cursors\light\static\fleur.cur,NumixCursors.cursors.light.static.fleur.cur ^
/resource:cursors\light\static\help.cur,NumixCursors.cursors.light.static.help.cur ^
/resource:cursors\light\static\not-allowed.cur,NumixCursors.cursors.light.static.not-allowed.cur ^
/resource:cursors\light\static\pencil.cur,NumixCursors.cursors.light.static.pencil.cur ^
/resource:cursors\light\static\pointer.cur,NumixCursors.cursors.light.static.pointer.cur ^
/resource:cursors\light\static\size_bdiag.cur,NumixCursors.cursors.light.static.size_bdiag.cur ^
/resource:cursors\light\static\size_fdiag.cur,NumixCursors.cursors.light.static.size_fdiag.cur ^
/resource:cursors\light\static\size_hor.cur,NumixCursors.cursors.light.static.size_hor.cur ^
/resource:cursors\light\static\size_ver.cur,NumixCursors.cursors.light.static.size_ver.cur ^
/resource:cursors\light\static\text.cur,NumixCursors.cursors.light.static.text.cur ^
/resource:cursors\light\static\up-arrow.cur,NumixCursors.cursors.light.static.up-arrow.cur ^
/resource:cursors\light\animated\progress.ani,NumixCursors.cursors.light.animated.progress.ani ^
/resource:cursors\light\animated\wait.ani,NumixCursors.cursors.light.animated.wait.ani ^
Program.cs MainForm.cs

echo Build complete: %CD%\NumixCursorsManager.exe
