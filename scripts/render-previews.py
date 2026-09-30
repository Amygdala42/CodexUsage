"""Draw documentation examples from layout measurements; never load the application.

Inputs are synthetic. Output PNGs have no account, desktop, or metadata capture.
Pillow and system fonts are local drawing tools, not release dependencies.
"""
import argparse
import os
import re
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

SCALE = 4
OUTPUT_WIDTH = 360
BG = (20, 27, 38)
CARD = (29, 40, 56)
BORDER = (43, 58, 78)
TEXT = (230, 237, 245)
MUTED = (164, 178, 196)
AQUA = (120, 169, 238)
BLUE = (134, 180, 242)
WIDGET_BG = (24, 34, 48)
QUOTA = (62, 134, 227)
TIME = (134, 180, 242)
TIME_TEXT = BLUE


class Canvas:
    def __init__(self, height, fonts):
        self.image = Image.new("RGBA", (360 * SCALE, height * SCALE), (0, 0, 0, 0))
        self.draw = ImageDraw.Draw(self.image)
        self.fonts = fonts
        self.cache = {}

    def font(self, size, bold=False):
        key = (size, bold)
        if key not in self.cache:
            self.cache[key] = ImageFont.truetype(str(self.fonts[bool(bold)]), round(size * SCALE), index=1)
        return self.cache[key]

    def round(self, x, y, w, h, radius, fill, outline=None):
        self.draw.rounded_rectangle(tuple(round(v * SCALE) for v in (x, y, x + w, y + h)),
                                    round(radius * SCALE), fill, outline, width=SCALE)

    def text(self, text, x, y, w, h, size, color=TEXT, bold=False, align="left"):
        font = self.font(size, bold)
        if self.draw.textlength(text, font=font) > w * SCALE:
            while text and self.draw.textlength(text + "…", font=font) > w * SCALE:
                text = text[:-1]
            text += "…"
        anchor = {"left": "lm", "center": "mm", "right": "rm"}[align]
        xpos = x if align == "left" else x + (w / 2 if align == "center" else w)
        self.draw.text((xpos * SCALE, (y + h / 2) * SCALE), text, fill=color, font=font, anchor=anchor)

    def line(self, points, color, width=1):
        self.draw.line([(round(x * SCALE), round(y * SCALE)) for x, y in points], color, width=round(width * SCALE))

    def disk(self, x, y, diameter, percent, color):
        bounds = tuple(round(v * SCALE) for v in (x, y, x + diameter, y + diameter))
        self.draw.ellipse(bounds, BORDER)
        if percent >= 100:
            self.draw.ellipse(bounds, color)
        elif percent > 0:
            self.draw.pieslice(bounds, -90, -90 + percent * 3.6, color)

    def save(self, path):
        size = (OUTPUT_WIDTH, round(self.image.height * OUTPUT_WIDTH / self.image.width))
        self.image.resize(size, Image.Resampling.LANCZOS).save(path, format="PNG", optimize=True)


def render(plan, english, destination, fonts, version):
    # Synthetic example clock: 2026-09-30 10:00 local time.
    if plan == "Plus":
        windows = [
            ("5h" if english else "5 小时额度", 79, "Resets in 3h 42m" if english else "3小时42分后重置", "09-30 13:42"),
            ("Weekly" if english else "每周额度", 39, "Resets in 4d 12h" if english else "4天12小时后重置", "10-04 22:00"),
        ]
    else:
        windows = [
            ("Weekly" if english else "每周额度", 95, "Resets in 4d 12h" if english else "4天12小时后重置", "10-04 22:00"),
        ]
    cards_height = len(windows) * 74 + (len(windows) - 1) * 8
    choice_y = 84 + cards_height + 48
    footer_y = choice_y + 46
    app_height = footer_y + 44
    top = 26
    canvas = Canvas(top + app_height + 83, fonts)
    canvas.text(plan + (" · Layout illustration · Example data" if english else " · 布局示意 · 示例数据"), 0, 0, 360, 20, 12, MUTED, align="center")
    canvas.round(.5, top + .5, 359, app_height - 1, 18, BG, BORDER)
    canvas.text("CodexUsage", 21, top + 18, 153, 28, 22, bold=True)
    canvas.text(version, 176, top + 18, 43, 28, 12, MUTED, align="center")
    canvas.text("GITHUB" if english else "GITHUB主页", 222, top + 18, 82, 28, 12, BLUE, align="center")
    canvas.round(309, top + 17, 31, 29, 0, CARD, BORDER)
    canvas.text("×", 309, top + 17, 31, 29, 11, align="center")
    canvas.text("Account plan" if english else "账号套餐", 22, top + 52, 83, 17, 10, MUTED)
    canvas.round(108, top + 51, 180, 21, 7, CARD)
    canvas.text(plan, 117, top + 51, 163, 21, 11, AQUA, True)
    for index, (label, amount, countdown, reset) in enumerate(windows):
        y = top + 84 + index * 82
        canvas.round(20.5, y + .5, 319, 73, 11, CARD, BORDER if index == 0 else None)
        canvas.text(label, 32, y + 8, 215, 22, 12, bold=True)
        canvas.text(str(amount) + "%", 253, y + 7, 75, 23, 19, AQUA, True)
        canvas.round(32, y + 37, 296, 5, 2.5, BORDER)
        canvas.round(32, y + 37, 296 * amount / 100, 5, 2.5, QUOTA)
        canvas.text(countdown, 32, y + 49, 170, 17, 9.5, BLUE)
        canvas.text(reset, 204, y + 49, 124, 17, 9.5, MUTED, align="right")
    canvas.text("Latest reset  09-29 16:09  Regular" if english else "最近重置公告  09-29 16:09  即时重置", 20, top + 84 + cards_height + 10, 262, 28, 11, MUTED)
    canvas.text("Source" if english else "来源", 290, top + 84 + cards_height + 10, 50, 28, 11, BLUE, align="right")
    # DetailsLayout.ChoiceRow: language, theme and selected quota share one row.
    # Captions and 11 px UI font match DetailsLayout / DetailsForm at 100% DPI.
    canvas.round(20, top + choice_y, 60, 28, 0, CARD, BORDER)
    canvas.text("中文" if english else "English", 20, top + choice_y, 60, 28, 11, align="center")
    canvas.round(88, top + choice_y, 88, 28, 0, CARD, BORDER)
    canvas.text("Light mode" if english else "浅色模式", 88, top + choice_y, 88, 28, 11, align="center")
    canvas.round(184.5, top + choice_y + .5, 155, 27, 6.44, CARD, BORDER)
    canvas.text(windows[0][0], 189, top + choice_y, 130, 28, 11)
    canvas.line([(321.8, top + choice_y + 11.5), (325.2, top + choice_y + 14.9), (328.6, top + choice_y + 11.5)], MUTED, 1.3)
    canvas.line([(20, top + footer_y - 8), (340, top + footer_y - 8)], BORDER)
    canvas.text("Every 5 min" if english else "额度每5分钟自动刷新", 20, top + footer_y, 124, 28, 10, MUTED)
    canvas.text("Updated 10:00:00" if english else "更新于 10:00:00", 148, top + footer_y, 112, 28, 10, MUTED)
    canvas.round(268, top + footer_y, 72, 28, 0, CARD, BORDER)
    canvas.text("Refresh" if english else "立即刷新", 268, top + footer_y, 72, 28, 11, align="center")
    icon_y = top + app_height + 12
    canvas.round(88.5, icon_y + .5, 85, 39, 8, WIDGET_BG, (75, 94, 119))
    canvas.disk(95, icon_y + 3, 14, windows[0][1], QUOTA)
    hours_left, window_hours = (3.7, 5) if plan == "Plus" else (108.0, 168)
    canvas.disk(95, icon_y + 23, 14, hours_left / window_hours * 100, TIME)
    canvas.text(str(windows[0][1]) + "%", 116, icon_y, 51, 20, 14, TEXT, True, "right")
    canvas.text(f"{hours_left:.1f}h", 116, icon_y + 20, 51, 20, 14, TIME_TEXT, True, "right")
    canvas.draw.ellipse(tuple(round(v * SCALE) for v in (227, icon_y + 4, 259, icon_y + 36)), WIDGET_BG)
    canvas.disk(237, icon_y + 7, 12, 75, QUOTA)
    canvas.disk(237, icon_y + 21, 12, 200 / 3.6, TIME)
    canvas.text("Taskbar widget" if english else "任务栏小条", 69, icon_y + 47, 124, 16, 11, MUTED, align="center")
    canvas.text("Tray icon" if english else "托盘图标", 190, icon_y + 47, 106, 16, 11, MUTED, align="center")
    canvas.save(destination)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("destination", type=Path)
    args = parser.parse_args()
    source = Path(__file__).resolve().parents[1] / "src" / "Program.cs"
    match = re.search(r'AssemblyVersion\("(\d+\.\d+\.\d+)\.\d+"\)', source.read_text(encoding="utf-8-sig"))
    if not match:
        raise SystemExit("Assembly version was not found in src/Program.cs.")
    version = "v" + match.group(1)
    fonts_root = Path(os.environ["WINDIR"]) / "Fonts"
    fonts = (fonts_root / "msyh.ttc", fonts_root / "msyhbd.ttc")
    if not all(path.is_file() for path in fonts):
        raise SystemExit("Microsoft YaHei system fonts are required for these documentation examples.")
    if not args.destination.is_dir():
        raise SystemExit("The destination assets directory must already exist.")
    for plan in ("Plus", "Pro"):
        for language in ("zh", "en"):
            path = args.destination / (plan.lower() + "-preview-" + language + ".png")
            render(plan, language == "en", path, fonts, version)
            with Image.open(path) as image:
                print(path.name, image.size, image.mode, "metadata:", sorted(image.info))


if __name__ == "__main__":
    main()
