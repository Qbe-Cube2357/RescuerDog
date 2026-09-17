from pathlib import Path
import sys

from reportlab.lib.colors import Color, HexColor, white
from reportlab.lib.pagesizes import A4
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.pdfgen import canvas
from reportlab.lib.utils import ImageReader


ROOT = Path("/Users/x24024xx/TGS/2026/Game/RescuerDog")
IMAGE_DIR = ROOT / "画像"
OUTPUT_DIR = ROOT / "output" / "pdf"
TMP_DIR = ROOT / "tmp" / "pdfs"

PAGE_W, PAGE_H = A4

NAVY = HexColor("#064B71")
DEEP_BLUE = HexColor("#087CA7")
AQUA = HexColor("#5DD8E8")
ORANGE = HexColor("#F5A12B")
PALE_BLUE = HexColor("#EAFBFD")
DARK = HexColor("#17364A")


def register_fonts():
    pdfmetrics.registerFont(
        TTFont(
            "Mochiy",
            str(ROOT / "Assets/TextMesh Pro/Fonts/MochiyPopOne-Regular.ttf"),
        )
    )
    pdfmetrics.registerFont(
        TTFont(
            "Yusei",
            str(ROOT / "Assets/TextMesh Pro/Fonts/YuseiMagic-Regular.ttf"),
        )
    )
    pdfmetrics.registerFont(
        TTFont(
            "Liberation",
            str(ROOT / "Assets/TextMesh Pro/Fonts/LiberationSans.ttf"),
        )
    )


def fit_image(c, path, x, y, w, h, align="center"):
    image = ImageReader(str(path))
    iw, ih = image.getSize()
    scale = min(w / iw, h / ih)
    dw, dh = iw * scale, ih * scale
    dx = x + (w - dw) / 2
    if align == "top":
        dy = y + h - dh
    elif align == "bottom":
        dy = y
    else:
        dy = y + (h - dh) / 2
    c.drawImage(image, dx, dy, width=dw, height=dh, mask="auto")


def draw_panel(c, x, y, w, h, title, heading_font, title_size=12):
    c.saveState()
    c.setFillColor(Color(1, 1, 1, alpha=0.91))
    c.setStrokeColor(Color(0.03, 0.49, 0.65, alpha=0.28))
    c.setLineWidth(1.2)
    c.roundRect(x, y, w, h, 15, fill=1, stroke=1)
    pill_w = min(w - 28, max(104, pdfmetrics.stringWidth(title, heading_font, title_size) + 38))
    c.setFillColor(NAVY)
    c.roundRect(x + 14, y + h - 34, pill_w, 27, 13.5, fill=1, stroke=0)
    c.setFillColor(white)
    c.setFont(heading_font, title_size)
    c.drawString(x + 27, y + h - 26, title)
    c.restoreState()


def wrap_cjk(text, font_name, font_size, max_width):
    lines = []
    current = ""
    for char in text:
        proposal = current + char
        if current and pdfmetrics.stringWidth(proposal, font_name, font_size) > max_width:
            lines.append(current)
            current = char
        else:
            current = proposal
    if current:
        lines.append(current)
    return lines


def wrap_words(text, font_name, font_size, max_width):
    words = text.split()
    lines = []
    current = ""
    for word in words:
        proposal = word if not current else current + " " + word
        if current and pdfmetrics.stringWidth(proposal, font_name, font_size) > max_width:
            lines.append(current)
            current = word
        else:
            current = proposal
    if current:
        lines.append(current)
    return lines


def draw_paragraph(c, text, x, top_y, max_width, font_name, font_size, leading, language):
    wrap = wrap_cjk if language == "jp" else wrap_words
    lines = wrap(text, font_name, font_size, max_width)
    c.setFont(font_name, font_size)
    c.setFillColor(DARK)
    y = top_y
    for line in lines:
        c.drawString(x, y, line)
        y -= leading
    return y


def draw_bullets(c, items, x, top_y, max_width, font_name, font_size, leading, language, gap=5):
    wrap = wrap_cjk if language == "jp" else wrap_words
    y = top_y
    text_x = x + 15
    text_width = max_width - 15
    for item in items:
        lines = wrap(item, font_name, font_size, text_width)
        c.setFillColor(ORANGE)
        c.circle(x + 4.5, y + 3, 3.1, fill=1, stroke=0)
        c.setFillColor(DARK)
        c.setFont(font_name, font_size)
        for line in lines:
            c.drawString(text_x, y, line)
            y -= leading
        y -= gap
    return y


def build_pdf(language):
    is_jp = language == "jp"
    output_name = "SwimmingDog_日本語.pdf" if is_jp else "SwimmingDog_英語.pdf"
    output_path = OUTPUT_DIR / output_name
    c = canvas.Canvas(str(output_path), pagesize=A4, pageCompression=1)
    c.setTitle("SwimmingDog 日本語説明書" if is_jp else "SwimmingDog English Instructions")

    background = IMAGE_DIR / "背景.jpg"
    logo = IMAGE_DIR / "タイトルテキスト画像.png"
    dog = IMAGE_DIR / "ゲーム内のコーギーモデル_背景透過_斜め正面.png"
    cookie = IMAGE_DIR / "クッキーイラスト_背景透過.png"
    handle = IMAGE_DIR / "操作説明用のハンドル写真_背景透過_斜め正面.png"

    c.drawImage(str(background), 0, 0, width=PAGE_W, height=PAGE_H)

    # Header/logo
    fit_image(c, logo, 55, 718, 485, 116)
    c.setFillColor(Color(1, 1, 1, alpha=0.95))
    c.roundRect(54, 684, 487, 34, 17, fill=1, stroke=0)
    tagline = (
        "犬かきで、おもちゃを集めまくれ！"
        if is_jp
        else "Dog-paddle and collect as many toys as you can!"
    )
    tagline_font = "Mochiy" if is_jp else "Liberation"
    tagline_size = 15.2 if is_jp else 14.8
    c.setFillColor(NAVY)
    c.setFont(tagline_font, tagline_size)
    c.drawCentredString(PAGE_W / 2, 695, tagline)

    heading_font = "Mochiy" if is_jp else "Liberation"
    body_font = "Yusei" if is_jp else "Liberation"

    # How to play
    how_title = "遊び方" if is_jp else "HOW TO PLAY"
    draw_panel(c, 31, 565, 533, 101, how_title, heading_font, 11.5)
    how_text = (
        "コーギーになって、プールに散らばったおもちゃを集めよう。飼い主のもとへ届けるとクッキーを獲得。ハイスコアを目指して全力で犬かきしよう！"
        if is_jp
        else "Become a corgi and collect the toys scattered around the pool. Bring them back to your owner to earn cookies. Dog-paddle with all your might and aim for a high score!"
    )
    draw_paragraph(
        c,
        how_text,
        49,
        615,
        495,
        body_font,
        10.7 if is_jp else 10.2,
        16 if is_jp else 14,
        language,
    )

    # Controls panel
    controls_title = "操作方法" if is_jp else "CONTROLS"
    draw_panel(c, 31, 278, 253, 271, controls_title, heading_font, 11.5)
    fit_image(c, handle, 54, 390, 207, 129)
    c.setStrokeColor(AQUA)
    c.setLineWidth(3)
    c.line(61, 389, 253, 389)
    controls = (
        [
            "左右のハンドルを交互に前へ回す：前進",
            "左右のハンドルを交互に後ろへ回す：後退",
            "片方だけハンドル回す：方向転換",
            "左右互いに反対方向にハンドルを回す：その場で方向転換",
        ]
        if is_jp
        else [
            "Alternate the left and right handles forward: Move forward",
            "Alternate the left and right handles backward: Move backward",
            "Turn only one handle: Steer",
            "Turn the handles in opposite directions: Turn in place",
        ]
    )
    draw_bullets(
        c,
        controls,
        49,
        372,
        218,
        body_font,
        8.7 if is_jp else 8.35,
        12.2 if is_jp else 10.8,
        language,
        gap=3.5,
    )

    # Features panel
    features_title = "ゲームの特徴" if is_jp else "GAME FEATURES"
    draw_panel(c, 296, 278, 268, 271, features_title, heading_font, 11.5)
    features = (
        [
            "本当に犬かきをしているような、左右2本のハンドル操作！",
            "おもちゃを集めるほど、コーギーの後ろに長く連なる！",
            "クッキーを集めてゲージが満タンになると、覚醒モード！！！",
            "鳥は君のおもちゃを狙っているぞ！うまく回避しろ！",
            "特別なカメラを届けると、プレイヤーの記念写真を撮影！",
        ]
        if is_jp
        else [
            "Two physical handles make it feel like you are really dog-paddling!",
            "The more toys you collect, the longer the trail behind the corgi grows!",
            "Fill the cookie gauge to unleash AWAKENING MODE!!!",
            "The birds are after your toys - dodge them!",
            "Deliver the special camera to take a souvenir photo of the player!",
        ]
    )
    draw_bullets(
        c,
        features,
        314,
        500,
        232,
        body_font,
        9.0 if is_jp else 8.65,
        13.0 if is_jp else 11.3,
        language,
        gap=6 if is_jp else 5,
    )

    # Bottom visual strip
    c.saveState()
    c.setFillColor(Color(1, 1, 1, alpha=0.88))
    c.roundRect(31, 30, 533, 228, 20, fill=1, stroke=0)
    c.restoreState()

    fit_image(c, dog, 328, 37, 205, 210, align="bottom")
    fit_image(c, cookie, 56, 112, 153, 124)

    c.setFillColor(NAVY)
    c.setFont(heading_font, 12 if is_jp else 11)
    bottom_line = "泳ぐ  •  集める  •  届ける" if is_jp else "SWIM  •  COLLECT  •  DELIVER"
    c.drawCentredString(170, 91, bottom_line)
    c.setFillColor(ORANGE)
    c.roundRect(64, 49, 212, 29, 14.5, fill=1, stroke=0)
    c.setFillColor(white)
    c.setFont(heading_font, 11 if is_jp else 10)
    footer = "全力で犬かきしよう！" if is_jp else "DOG-PADDLE WITH ALL YOUR MIGHT!"
    c.drawCentredString(170, 58, footer)

    c.showPage()
    c.save()
    return output_path


if __name__ == "__main__":
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    TMP_DIR.mkdir(parents=True, exist_ok=True)
    register_fonts()
    languages = sys.argv[1:] or ["jp", "en"]
    for lang in languages:
        if lang not in {"jp", "en"}:
            raise ValueError(f"Unsupported language: {lang}")
        print(build_pdf(lang))
