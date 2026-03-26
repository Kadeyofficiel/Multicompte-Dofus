from PIL import Image, ImageDraw

SIZE = 512
img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
draw = ImageDraw.Draw(img)

# Rounded dark background
pad = 24
draw.rounded_rectangle(
    (pad, pad, SIZE - pad, SIZE - pad),
    radius=120,
    fill=(12, 18, 40, 255)
)

# Controller-like bar
draw.rounded_rectangle((120, 190, 392, 330), radius=64, outline=(75, 225, 255, 255), width=24)

# Left d-pad
draw.rectangle((165, 240, 215, 280), fill=(75, 225, 255, 255))
draw.rectangle((185, 220, 195, 300), fill=(75, 225, 255, 255))

# Right buttons
draw.ellipse((300, 228, 336, 264), fill=(255, 80, 170, 255))
draw.ellipse((338, 256, 374, 292), fill=(255, 80, 170, 255))

# Lightning bolt overlay
bolt = [(248, 108), (194, 246), (252, 246), (218, 394), (320, 220), (264, 220)]
draw.polygon(bolt, fill=(255, 80, 170, 255))

png_path = "assets/abdefus_logo.png"
ico_path = "assets/abdefus.ico"
img.save(png_path, "PNG")
img.save(ico_path, format="ICO", sizes=[(16,16), (24,24), (32,32), (48,48), (64,64), (128,128), (256,256)])

print("Created", png_path, "and", ico_path)
