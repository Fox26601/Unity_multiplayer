#!/usr/bin/env python3
"""Patch 00_MainMenu.unity: dark buttons, layout, readable fonts."""
from pathlib import Path

SCENE = Path(__file__).resolve().parents[1] / "Scenes/00_MainMenu.unity"

LAYOUT = {
    "1541868983": (0, 80, 560, 64),
    "1140267592": (0, 0, 560, 64),
    "1657523005": (-220, -90, 96, 96),
    "1832269040": (140, -90, 320, 64),
    "1445607679": (0, -200, 400, 72),
    "56758951": (0, -290, 400, 72),
}

DARK_BTN = "{r: 0.22, g: 0.28, b: 0.38, a: 1}"
DARK_INPUT = "{r: 0.15, g: 0.15, b: 0.18, a: 1}"
BOOTSTRAP_GUID = "4a05cd6f175d84daaaafce6c3b66265d"


def patch_file(content: str) -> str:
    lines = content.splitlines(keepends=True)
    current_go = ""
    i = 0
    while i < len(lines):
        line = lines[i]
        if "m_Name:" in line and "m_Name: " in line:
            current_go = line.split("m_Name:", 1)[1].strip()

        if line.startswith("--- !u!224 &"):
            fid = line.split("&", 1)[1].strip()
            if fid in LAYOUT:
                ax, ay, w, h = LAYOUT[fid]
                j = i + 1
                while j < len(lines) and not lines[j].startswith("--- !u!"):
                    if "m_AnchoredPosition:" in lines[j]:
                        lines[j] = f"  m_AnchoredPosition: {{x: {ax}, y: {ay}}}\n"
                    elif "m_SizeDelta:" in lines[j]:
                        lines[j] = f"  m_SizeDelta: {{x: {w}, y: {h}}}\n"
                    j += 1

        if "m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image" in line:
            j = i + 1
            while j < len(lines) and not lines[j].startswith("--- !u!"):
                if "m_Color:" in lines[j] and "{r: 1, g: 1, b: 1, a: 1}" in lines[j]:
                    if current_go.startswith("Btn"):
                        lines[j] = f"  m_Color: {DARK_BTN}\n"
                    elif "Field" in current_go:
                        lines[j] = f"  m_Color: {DARK_INPUT}\n"
                j += 1

        if "m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Button" in line:
            j = i + 1
            while j < len(lines) and not lines[j].startswith("--- !u!"):
                if "m_NormalColor:" in lines[j]:
                    lines[j] = f"    m_NormalColor: {DARK_BTN}\n"
                elif "m_HighlightedColor:" in lines[j]:
                    lines[j] = "    m_HighlightedColor: {r: 0.28, g: 0.35, b: 0.48, a: 1}\n"
                elif "m_PressedColor:" in lines[j]:
                    lines[j] = "    m_PressedColor: {r: 0.18, g: 0.22, b: 0.32, a: 1}\n"
                j += 1

        if "m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Text" in line:
            j = i + 1
            while j < len(lines) and not lines[j].startswith("--- !u!"):
                if "m_Color:" in lines[j] and "{r: 1, g: 1, b: 1, a: 1}" in lines[j]:
                    lines[j] = "  m_Color: {r: 1, g: 1, b: 1, a: 1}\n"
                elif "m_FontSize:" in lines[j]:
                    lines[j] = "    m_FontSize: 28\n"
                elif "m_BestFit:" in lines[j]:
                    lines[j] = "    m_BestFit: 0\n"
                elif "m_MaxSize:" in lines[j]:
                    lines[j] = "    m_MaxSize: 48\n"
                j += 1

        i += 1

    content = "".join(lines)
    return add_bootstrap(content)


def add_bootstrap(content: str) -> str:
    if BOOTSTRAP_GUID in content:
        return content

    content = content.replace(
        "  - component: {fileID: 1176105651}\n  m_Layer: 0\n  m_Name: MainMenuCanvas",
        "  - component: {fileID: 1176105651}\n  - component: {fileID: 1176105656}\n  m_Layer: 0\n  m_Name: MainMenuCanvas",
    )

    bootstrap = f"""--- !u!114 &1176105656
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 1176105650}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {BOOTSTRAP_GUID}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: FusionMultiplayer.Runtime::FusionMultiplayer.UI.UiReadabilityBootstrap
"""
    marker = "m_EditorClassIdentifier: FusionMultiplayer.Runtime::FusionMultiplayer.UI.MainMenuUI"
    idx = content.find(marker)
    if idx == -1:
        return content
    end = content.find("--- !u!", idx + 1)
    return content[:end] + bootstrap + content[end:]


def main():
    text = SCENE.read_text(encoding="utf-8")
    SCENE.write_text(patch_file(text), encoding="utf-8")
    print(f"Patched {SCENE}")


if __name__ == "__main__":
    main()
