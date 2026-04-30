# NumixCursorTheme-WindowsInstaller (v1.0)

![License: Wrapper](https://img.shields.io/badge/license-Wrapper%20License-blue)
![License: Cursors](https://img.shields.io/badge/license-GPL--3.0-green)
![Platform](https://img.shields.io/badge/platform-Windows%2010%20%2F%2011-lightgrey)

![Preview](assets/preview.png)

> A dependency-free Windows installer for the Numix cursor theme.

This project is a **Windows-native port** of the original [numix-cursor-theme](https://github.com/numixproject/numix-cursor-theme) by the [Numix Project](https://github.com/numixproject).  
The original repository provides Linux-first tooling and requires additional dependencies to install the theme on Windows. This project eliminates that friction by packaging all cursor files alongside a standard Windows `.inf` installer — no extra software required.

---

## What's included

- **109 cursor files** (`.cur` and `.ani`) covering the full Numix Dark cursor set
- **`install.inf`** — a native Windows Setup Information file that registers the theme and copies all files to the correct system directory automatically
- **`uninstall.bat`** — a one-click uninstaller that removes the theme from the system cleanly

---

## Installation

### Install

1. Download or clone this repository
2. Open the `src/` folder
3. Right-click `install.inf` → **Install**
4. Open **Mouse Properties** (`Win + R` → `main.cpl` → `Enter`)
5. Go to the **Pointers** tab → select **Numix-Dark** from the scheme dropdown
6. Click **Apply** → **OK**

### Uninstall

> **Note:** Before running the uninstaller, open *Mouse Properties* and switch to a different cursor scheme. If Numix-Dark is still active when it is removed, your cursor may appear broken until the next reboot.

1. Right-click `uninstall.bat` in the `src/` folder → **Run as administrator**
2. Follow the on-screen prompt to confirm you have switched cursor scheme
3. The script will remove the registry entry and delete `C:\Windows\Cursors\Numix-Dark\` automatically

---

## Compatibility

| Windows Version | Status      |
|----------------|-------------|
| Windows 11     | Supported   |
| Windows 10     | Supported   |
| Windows 7 / 8  | Untested    |

> **Windows 11 Note:** The *Location Select* and *Person Select* cursor slots introduced in Windows 11 cannot be themed through the standard cursor scheme mechanism and will remain as Windows defaults. All other cursor slots are fully supported.

---

## Project structure

```
NumixCursorTheme-WindowsInstaller/
├── src/
│   ├── cursors/
│   │   ├── static/        # Static cursor files (.cur)
│   │   └── animated/      # Animated cursor files (.ani)
│   ├── install.inf        # Windows installer definition
│   └── uninstall.bat      # One-click uninstaller (run as administrator)
├── assets/
│   └── preview.png        # Preview image for the README
├── LICENSE
└── README.md
```

---

## Credits

All cursor artwork is part of the **Numix cursor theme**, originally created by the [Numix Project](https://github.com/numixproject) and distributed under the GPL-3.0 license.

This installer wrapper is a convenience repackaging for Windows users. No artwork was modified.

- Original project: https://github.com/numixproject/numix-cursor-theme
- Numix Project: https://github.com/numixproject

---

## License

The **installer wrapper** (`install.inf`, `uninstall.bat`, project structure, and documentation) is licensed under the [Installer Wrapper License](./LICENSE).  
The **cursor assets** (`.cur`, `.ani`) retain their original [GPL-3.0](https://www.gnu.org/licenses/gpl-3.0.html) license from the Numix Project.
