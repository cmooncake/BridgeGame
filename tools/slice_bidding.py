from PIL import Image, ImageDraw, ImageFilter, ImageFont
from pathlib import Path

art = Path(r"C:\git_proj\MoonBridgeClient\MoonBridgeClient\Assets\Art\UI\Bidding")
res = Path(r"C:\git_proj\MoonBridgeClient\MoonBridgeClient\Assets\Resources\UI\Bidding")
art.mkdir(parents=True, exist_ok=True)
res.mkdir(parents=True, exist_ok=True)


def rounded(size, radius, fill, outline, outline_w=3, shadow=False):
    w, h = size
    pad = 14 if shadow else 0
    canvas = Image.new("RGBA", (w + pad * 2, h + pad * 2), (0, 0, 0, 0))
    if shadow:
        s = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
        sd = ImageDraw.Draw(s)
        sd.rounded_rectangle(
            (pad + 3, pad + 6, pad + w + 3, pad + h + 8),
            radius,
            fill=(0, 0, 0, 80),
        )
        canvas.alpha_composite(s.filter(ImageFilter.GaussianBlur(7)))
    layer = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    box = (pad, pad, pad + w - 1, pad + h - 1)
    d.rounded_rectangle(box, radius, fill=fill, outline=outline, width=outline_w)
    canvas.alpha_composite(layer)
    return canvas


def save_both(name, image):
    image.save(art / name)
    image.save(res / name)
    print("wrote", name, image.size)


save_both(
    "panel_bg.png",
    rounded((900, 720), 28, (246, 241, 228, 255), (196, 184, 160, 255), 3, True),
)
save_both(
    "bid_cell.png",
    rounded((160, 72), 10, (255, 252, 247, 255), (188, 178, 164, 255), 2, False),
)
save_both(
    "btn_pass.png",
    rounded((280, 88), 12, (68, 145, 68, 255), (42, 104, 42, 255), 2, False),
)
save_both(
    "btn_double.png",
    rounded((280, 88), 12, (53, 122, 189, 255), (32, 82, 136, 255), 2, False),
)
save_both(
    "btn_redouble.png",
    rounded((280, 88), 12, (198, 68, 68, 255), (148, 40, 40, 255), 2, False),
)
save_both(
    "history_bg.png",
    rounded((560, 260), 16, (18, 16, 14, 220), (196, 163, 90, 255), 4, False),
)

# Preview how the assembled panel should look.
panel_w, panel_h = 860, 700
preview = Image.new("RGBA", (1920, 1080), (28, 72, 48, 255))
dim = Image.new("RGBA", preview.size, (0, 0, 0, 90))
preview.alpha_composite(dim)

panel = rounded((panel_w, panel_h), 28, (246, 241, 228, 255), (196, 184, 160, 255), 3, True)
px = (1920 - panel.size[0]) // 2
py = (1080 - panel.size[1]) // 2 - 20
preview.alpha_composite(panel, (px, py))

try:
    title_font = ImageFont.truetype("msyh.ttc", 36)
    cell_font = ImageFont.truetype("msyh.ttc", 28)
    small_font = ImageFont.truetype("msyh.ttc", 16)
    hist_font = ImageFont.truetype("msyh.ttc", 20)
except OSError:
    title_font = ImageFont.load_default()
    cell_font = title_font
    small_font = title_font
    hist_font = title_font

draw = ImageDraw.Draw(preview)
ox, oy = px + 14, py + 14
draw.text((ox + panel_w / 2, oy + 36), "请选择你的叫牌", font=title_font, fill=(31, 26, 20, 255), anchor="mm")

suits = ["♣", "♦", "♥", "♠", "NT"]
suit_colors = [(26, 24, 20, 255), (196, 60, 60, 255), (196, 60, 60, 255), (26, 24, 20, 255), (26, 24, 20, 255)]
cell = rounded((148, 56), 8, (255, 252, 247, 255), (188, 178, 164, 255), 2, False)
grid_w = 5 * 148 + 4 * 10
grid_x = ox + (panel_w - grid_w) / 2
grid_y = oy + 78
for level in range(7):
    for s in range(5):
        x = int(grid_x + s * 158)
        y = int(grid_y + level * 64)
        preview.alpha_composite(cell, (x, y))
        label = f"{level + 1}{suits[s]}"
        draw.text((x + 74, y + 28), label, font=cell_font, fill=suit_colors[s], anchor="mm")

actions = [
    ((68, 145, 68, 255), "PASS", "不叫"),
    ((53, 122, 189, 255), "X", "加倍"),
    ((198, 68, 68, 255), "XX", "再加倍"),
]
btn_y = int(oy + panel_h - 96)
btn_w = 248
gap = 16
row_w = 3 * btn_w + 2 * gap
btn_x0 = int(ox + (panel_w - row_w) / 2)
for i, (color, top, bottom) in enumerate(actions):
    btn = rounded((btn_w, 80), 12, color, tuple(max(0, c - 30) if idx < 3 else 255 for idx, c in enumerate(color)), 2, False)
    x = btn_x0 + i * (btn_w + gap)
    preview.alpha_composite(btn, (x, btn_y))
    draw.text((x + btn_w / 2, btn_y + 28), top, font=cell_font, fill=(255, 255, 255, 255), anchor="mm")
    draw.text((x + btn_w / 2, btn_y + 56), bottom, font=small_font, fill=(255, 255, 255, 255), anchor="mm")

hist = rounded((500, 220), 16, (18, 16, 14, 220), (196, 163, 90, 255), 4, False)
preview.alpha_composite(hist, (1920 - 500 - 28, 24))
draw.text((1920 - 500 - 8, 40), "叫牌过程", font=hist_font, fill=(255, 255, 255, 255), anchor="ra")
for i, name in enumerate(["西", "北", "东", "南"]):
    draw.text((1920 - 500 + 40 + i * 115, 78), name, font=small_font, fill=(216, 198, 140, 255), anchor="mm")
for i, (txt, col) in enumerate([("1♣", (255, 255, 255, 255)), ("1♥", (220, 80, 80, 255)), ("PASS", (255, 255, 255, 255)), ("?", (255, 217, 89, 255))]):
    draw.text((1920 - 500 + 40 + i * 115, 118), txt, font=hist_font, fill=col, anchor="mm")

preview_path = Path(r"C:\git_proj\MoonBridgeClient\tools\_preview_bidding.png")
preview.convert("RGB").save(preview_path)
print("preview", preview_path)
