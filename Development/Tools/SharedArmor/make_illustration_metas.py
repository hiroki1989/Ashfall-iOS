"""Give generated illustration atlases stable Unity GUIDs and iOS texture settings."""
import re
import uuid
from pathlib import Path

ROOT = Path("D:/Unity/Ashfall-iOS")
ART = ROOT / "Assets/Resources/Art"
TARGET = ART / "SharedArmorIllustration"
REFERENCE = ART / "GeneratedWarden/armor-rebuilt-atlas.png.meta"
FOLDER_REFERENCE = ART / "SharedArmorLUT.meta"


def with_guid(template):
    return re.sub(r"(?m)^guid: [0-9a-f]{32}$", f"guid: {uuid.uuid4().hex}", template)


def main():
    folder_meta = TARGET.with_suffix(".meta")
    if not folder_meta.exists():
        folder_meta.write_text(with_guid(FOLDER_REFERENCE.read_text(encoding="utf-8")), encoding="utf-8")
    template = REFERENCE.read_text(encoding="utf-8").replace("4096", "1024").replace("2048", "1024")
    created = 0
    for image in sorted(TARGET.glob("*.png")):
        meta = image.with_name(image.name + ".meta")
        if not meta.exists():
            meta.write_text(with_guid(template), encoding="utf-8")
            created += 1
    print(f"PNG={len(list(TARGET.glob('*.png')))} new_meta={created} folder_meta={folder_meta.exists()}")


if __name__ == "__main__":
    main()
