#!/usr/bin/env python3
"""Patch menu/lobby/game scenes: 1280x720 scaler, dark buttons, bootstrap + SessionFlowUI."""
from __future__ import annotations

import re
from pathlib import Path

SCENES_DIR = Path(__file__).resolve().parents[1] / "Scenes"
BOOTSTRAP_GUID = "4a05cd6f175d84daaaafce6c3b66265d"
SESSION_FLOW_GUID = "86c0cb9e7f5284e2383814f4c1bb5d28"
DARK_BTN = "{r: 0.22, g: 0.28, b: 0.38, a: 1}"
DARK_INPUT = "{r: 0.15, g: 0.15, b: 0.18, a: 1}"
PANEL_BG = "{r: 0.1, g: 0.1, b: 0.12, a: 0.94}"

GAME_UI_CANVAS_RT = "1898910439"
INPUT_SYSTEM_UI_MODULE_GUID = "01614664b831546d2ae94a42149d80ac"
HUD_ROOT_NAMES = {"CharacterSelectPanel", "ChatPanel"}


def _find_blocks(text: str) -> list[tuple[int, str, str, str]]:
    """Return [(start, end, object_id, block_text_including_header), ...]."""
    blocks: list[tuple[int, str, str, str]] = []
    for match in re.finditer(r"--- !u!\d+ &(\d+)\n", text):
        blocks.append((match.start(), -1, match.group(1), ""))
    for index, (start, _, obj_id, _) in enumerate(blocks):
        end = blocks[index + 1][0] if index + 1 < len(blocks) else len(text)
        blocks[index] = (start, end, obj_id, text[start:end])
    return blocks


def _block_body(block: str) -> str:
    return block.split("\n", 1)[1] if "\n" in block else ""


def _gameobject_name(body: str) -> str | None:
    match = re.search(r"^  m_Name: (.+)$", body, re.MULTILINE)
    return match.group(1).strip() if match else None


def _gameobject_components(body: str) -> list[str]:
    return re.findall(r"  - component: {fileID: (\d+)}", body)


def _rect_transform_children(body: str) -> list[str]:
    section = re.search(r"  m_Children:\n((?:  - \{fileID: \d+\}\n)*)", body)
    if not section:
        return []
    return re.findall(r"  - \{fileID: (\d+)\}", section.group(1))


def _rect_transform_gameobject(body: str) -> str | None:
    match = re.search(r"  m_GameObject: {fileID: (\d+)}", body)
    return match.group(1) if match else None


def _collect_transform_descendants(by_id: dict[str, str], root_transform_ids: set[str]) -> set[str]:
    to_remove: set[str] = set()
    queue = list(root_transform_ids)
    while queue:
        transform_id = queue.pop()
        if transform_id in to_remove:
            continue
        transform_block = by_id.get(transform_id)
        if transform_block is None:
            continue
        to_remove.add(transform_id)
        transform_body = _block_body(transform_block)
        go_id = _rect_transform_gameobject(transform_body)
        if go_id:
            to_remove.add(go_id)
            go_block = by_id.get(go_id)
            if go_block:
                to_remove.update(_gameobject_components(_block_body(go_block)))
        for child_id in _rect_transform_children(transform_body):
            queue.append(child_id)
    return to_remove


def _collect_hud_object_ids(text: str) -> set[str]:
    blocks = _find_blocks(text)
    by_id = {obj_id: block for _, _, obj_id, block in blocks}

    root_transform_ids: set[str] = set()
    for obj_id, block in by_id.items():
        if not block.startswith("--- !u!1 &"):
            continue
        name = _gameobject_name(_block_body(block))
        if name not in HUD_ROOT_NAMES:
            continue
        for comp_id in _gameobject_components(_block_body(block)):
            comp_block = by_id.get(comp_id, "")
            if comp_block.startswith("--- !u!224 &"):
                root_transform_ids.add(comp_id)

    return _collect_transform_descendants(by_id, root_transform_ids)


def _remove_orphan_rect_transforms(text: str) -> tuple[str, set[str]]:
    removed_total: set[str] = set()
    while True:
        blocks = _find_blocks(text)
        by_id = {obj_id: block for _, _, obj_id, block in blocks}
        existing = set(by_id.keys())
        orphan_roots: set[str] = set()

        for obj_id, block in by_id.items():
            if not block.startswith("--- !u!224 &"):
                continue
            body = _block_body(block)
            father = re.search(r"  m_Father: {fileID: (\d+)}", body)
            if father is None:
                continue
            parent_id = father.group(1)
            if parent_id != "0" and parent_id not in existing:
                orphan_roots.add(obj_id)

        if not orphan_roots:
            break

        batch = _collect_transform_descendants(by_id, orphan_roots)
        if not batch:
            break
        text = _remove_yaml_ids(text, batch)
        removed_total |= batch

    return text, removed_total


def _remove_yaml_ids(text: str, ids_to_remove: set[str]) -> str:
    if not ids_to_remove:
        return text

    blocks = _find_blocks(text)
    kept = [block for _, _, obj_id, block in blocks if obj_id not in ids_to_remove]
    header = text[: blocks[0][0]] if blocks else ""
    cleaned = header + "".join(kept)

    for obj_id in ids_to_remove:
        cleaned = re.sub(rf"  - {{fileID: {obj_id}}}\n", "", cleaned)
        cleaned = re.sub(rf"  - component: {{fileID: {obj_id}}}\n", "", cleaned)

    return cleaned


def strip_legacy_game_hud(text: str) -> str:
    """Remove legacy CharacterSelectPanel / ChatPanel hierarchies from 02_Game."""
    text = re.sub(
        rf"(--- !u!224 &{GAME_UI_CANVAS_RT}\nRectTransform:.*?)m_Children:\n(?:  - \{{fileID: \d+\}}\n)*",
        r"\1m_Children: []\n",
        text,
        count=1,
        flags=re.DOTALL,
    )

    text = re.sub(
        r"(m_EditorClassIdentifier: FusionMultiplayer\.Runtime::FusionMultiplayer\.UI\.CharacterSelectUI\n)"
        r"(?:  _characterButtons:\n(?:  - \{fileID: \d+\}\n)*|  _characterButtons: \[\]\n)"
        r"  _statusText: \{fileID: \d+\}\n"
        r"  _panelRoot: \{fileID: \d+\}",
        r"\1  _characterButtons: []\n  _statusText: {fileID: 0}\n  _panelRoot: {fileID: 0}",
        text,
    )

    text = re.sub(
        r"(m_EditorClassIdentifier: FusionMultiplayer\.Runtime::FusionMultiplayer\.UI\.ChatUI\n)"
        r"  _input: \{fileID: \d+\}\n"
        r"  _sendButton: \{fileID: \d+\}\n"
        r"  _log: \{fileID: \d+\}\n"
        r"  _whisperTargetNick: \{fileID: \d+\}",
        r"\1  _input: {fileID: 0}\n  _sendButton: {fileID: 0}\n  _log: {fileID: 0}\n  _whisperTargetNick: {fileID: 0}",
        text,
    )

    remove_ids = _collect_hud_object_ids(text)
    if remove_ids:
        text = _remove_yaml_ids(text, remove_ids)
        print(f"  Removed {len(remove_ids)} legacy HUD object blocks from 02_Game")

    text, orphan_ids = _remove_orphan_rect_transforms(text)
    if orphan_ids:
        print(f"  Removed {len(orphan_ids)} orphan UI object blocks from 02_Game")

    return text


def migrate_event_system(text: str) -> str:
    """Replace StandaloneInputModule with InputSystemUIInputModule on disk."""
    lines = text.splitlines(keepends=True)
    output: list[str] = []
    index = 0
    migrated = 0

    while index < len(lines):
        line = lines[index]
        if not line.startswith("--- !u!114 &"):
            output.append(line)
            index += 1
            continue

        block_start = index
        index += 1
        while index < len(lines) and not lines[index].startswith("--- !u!"):
            index += 1
        block = lines[block_start:index]

        block_text = "".join(block)
        if "UnityEngine.UI::UnityEngine.EventSystems.StandaloneInputModule" not in block_text:
            output.extend(block)
            continue

        comp_match = re.search(r"--- !u!114 &(\d+)", block[0])
        go_match = re.search(r"m_GameObject: {fileID: (\d+)}", block_text)
        if comp_match is None or go_match is None:
            output.extend(block)
            continue

        comp_id = comp_match.group(1)
        go_id = go_match.group(1)
        migrated += 1
        output.append(
            f"--- !u!114 &{comp_id}\n"
            "MonoBehaviour:\n"
            "  m_ObjectHideFlags: 0\n"
            "  m_CorrespondingSourceObject: {fileID: 0}\n"
            "  m_PrefabInstance: {fileID: 0}\n"
            "  m_PrefabAsset: {fileID: 0}\n"
            f"  m_GameObject: {{fileID: {go_id}}}\n"
            "  m_Enabled: 1\n"
            "  m_EditorHideFlags: 0\n"
            f"  m_Script: {{fileID: 11500000, guid: {INPUT_SYSTEM_UI_MODULE_GUID}, type: 3}}\n"
            "  m_Name: \n"
            "  m_EditorClassIdentifier: Unity.InputSystem::UnityEngine.InputSystem.UI.InputSystemUIInputModule\n"
            "  m_SendPointerHoverToParent: 1\n"
            "  m_MoveRepeatDelay: 0.5\n"
            "  m_MoveRepeatRate: 0.1\n"
            "  m_XRTrackingOrigin: {fileID: 0}\n"
            "  m_ActionsAsset: {fileID: 0}\n"
            "  m_PointAction: {fileID: 0}\n"
            "  m_MoveAction: {fileID: 0}\n"
            "  m_SubmitAction: {fileID: 0}\n"
            "  m_CancelAction: {fileID: 0}\n"
            "  m_LeftClickAction: {fileID: 0}\n"
            "  m_MiddleClickAction: {fileID: 0}\n"
            "  m_RightClickAction: {fileID: 0}\n"
            "  m_ScrollWheelAction: {fileID: 0}\n"
            "  m_TrackedDevicePositionAction: {fileID: 0}\n"
            "  m_TrackedDeviceOrientationAction: {fileID: 0}\n"
            "  m_DeselectOnBackgroundClick: 1\n"
            "  m_PointerBehavior: 0\n"
            "  m_CursorLockBehavior: 0\n"
            "  m_ScrollDeltaPerTick: 6\n"
        )

    if migrated:
        print(f"  Migrated {migrated} EventSystem module(s) to Input System")
    return "".join(output)


CANVAS_PATCHES = {
    "00_MainMenu.unity": [
        {
            "canvas_name": "MainMenuCanvas",
            "go_id": "1176105650",
            "new_components": [
                ("1176105658", SESSION_FLOW_GUID,
                 "FusionMultiplayer.Runtime::FusionMultiplayer.UI.SessionFlowUI",
                 "  _statusText: {fileID: 0}\n"),
            ],
            "wire_main_menu_flow": ("1176105651", "1176105658"),
        },
    ],
    "01_Lobby.unity": [
        {
            "canvas_name": "LobbyCanvas",
            "go_id": "1176105650",
            "new_components": [
                ("1176105657", BOOTSTRAP_GUID,
                 "FusionMultiplayer.Runtime::FusionMultiplayer.UI.UiReadabilityBootstrap", ""),
                ("1176105658", SESSION_FLOW_GUID,
                 "FusionMultiplayer.Runtime::FusionMultiplayer.UI.SessionFlowUI",
                 "  _statusText: {fileID: 0}\n"),
            ],
        },
    ],
    "02_Game.unity": [
        {
            "canvas_name": "GameUICanvas",
            "go_id": "1898910433",
            "new_components": [
                ("1898910440", BOOTSTRAP_GUID,
                 "FusionMultiplayer.Runtime::FusionMultiplayer.UI.UiReadabilityBootstrap", ""),
            ],
        },
        {
            "canvas_name": "GameOverCanvas",
            "go_id": "1176105650",
            "new_components": [
                ("1176105657", BOOTSTRAP_GUID,
                 "FusionMultiplayer.Runtime::FusionMultiplayer.UI.UiReadabilityBootstrap", ""),
            ],
        },
    ],
}


def fix_resolution(text: str) -> str:
    return text.replace(
        "m_ReferenceResolution: {x: 1920, y: 1080}",
        "m_ReferenceResolution: {x: 1280, y: 720}",
    )


def fix_colors(text: str) -> str:
    lines = text.splitlines(keepends=True)
    current_name = ""
    i = 0
    while i < len(lines):
        line = lines[i]
        if "m_Name:" in line:
            current_name = line.split("m_Name:", 1)[1].strip()

        if "m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image" in line:
            j = i + 1
            while j < len(lines) and not lines[j].startswith("--- !u!"):
                if "m_Color:" in lines[j] and "{r: 1, g: 1, b: 1, a: 1}" in lines[j]:
                    if current_name.startswith("Btn"):
                        lines[j] = f"  m_Color: {DARK_BTN}\n"
                    elif "Field" in current_name or current_name == "Panel":
                        lines[j] = f"  m_Color: {DARK_INPUT if 'Field' in current_name else PANEL_BG}\n"
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

        i += 1

    return "".join(lines)


def fix_merged_component_lines(text: str) -> str:
    """Repair lines where two component entries were concatenated on one line."""
    return re.sub(
        r"(  - component: \{fileID: \d+\})  - component: (\{fileID: \d+\})",
        r"\1\n  - component: \2",
        text,
    )


def add_component_to_go(text: str, go_id: str, comp_id: str) -> str:
    if f"{{fileID: {comp_id}}}" in text:
        return text

    pattern = (
        rf"(--- !u!1 &{go_id}\nGameObject:.*?m_Component:\n"
        rf"((?:  - component: \{{fileID: \d+\}}\n)+))(  m_Layer:)"
    )
    match = re.search(pattern, text, re.DOTALL)
    if not match:
        return text

    insert = f"  - component: {{fileID: {comp_id}}}\n"
    replacement = match.group(1) + match.group(2) + insert + match.group(3)
    return text[: match.start()] + replacement + text[match.end() :]


def add_mono_behaviour(text: str, go_id: str, comp_id: str, guid: str, class_id: str, extra: str) -> str:
    marker = f"--- !u!114 &{comp_id}\n"
    if marker in text:
        return text
    snippet = f"""--- !u!114 &{comp_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {guid}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: {class_id}
{extra}"""
    anchor = f"  m_GameObject: {{fileID: {go_id}}}"
    idx = text.find(anchor)
    if idx == -1:
        return text + "\n" + snippet
    end = text.find("--- !u!", idx + 1)
    if end == -1:
        return text + "\n" + snippet
    return text[:end] + snippet + "\n" + text[end:]


def wire_session_flow(text: str, menu_id: str, flow_id: str) -> str:
    needle = f"  _randomColorButton: {{fileID:"
    if "_sessionFlow:" in text:
        return re.sub(
            r"  _sessionFlow: \{fileID: \d+\}",
            f"  _sessionFlow: {{fileID: {flow_id}}}",
            text,
            count=1,
        )
    idx = text.find(f"--- !u!114 &{menu_id}\n")
    if idx == -1:
        return text
    end = text.find("--- !u!", idx + 1)
    block = text[idx:end]
    if "_sessionFlow:" in block:
        return text
    insert_at = block.rfind("\n  _")
    if insert_at == -1:
        return text
    line_end = block.find("\n", insert_at + 1)
    new_block = block[:line_end] + f"\n  _sessionFlow: {{fileID: {flow_id}}}" + block[line_end:]
    return text[:idx] + new_block + text[end:]


def patch_scene(path: Path) -> None:
    text = path.read_text(encoding="utf-8")
    text = fix_merged_component_lines(text)
    text = fix_resolution(text)
    text = fix_colors(text)
    text = migrate_event_system(text)

    if path.name == "02_Game.unity":
        text = strip_legacy_game_hud(text)

    for spec in CANVAS_PATCHES.get(path.name, []):
        go_id = spec["go_id"]
        for comp_id, guid, class_id, extra in spec["new_components"]:
            text = add_component_to_go(text, go_id, comp_id)
            text = add_mono_behaviour(text, go_id, comp_id, guid, class_id, extra)
        if "wire_main_menu_flow" in spec:
            menu_id, flow_id = spec["wire_main_menu_flow"]
            text = wire_session_flow(text, menu_id, flow_id)

    path.write_text(text, encoding="utf-8")
    print(f"Patched {path.name}")


def main() -> None:
    for name in ("00_MainMenu.unity", "01_Lobby.unity", "02_Game.unity"):
        scene = SCENES_DIR / name
        if scene.exists():
            patch_scene(scene)
        else:
            print(f"Skip missing {name}")


if __name__ == "__main__":
    main()
