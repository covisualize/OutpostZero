"""Face, eye, and clothing variation shared by the character generator and tests."""

EYE = {
    "walker": (0.75, 1.0, 0.35),
    "runner": (0.95, 0.12, 0.08),
    "brute": (0.95, 0.45, 0.12),
    "survivor": (0.25, 0.18, 0.14),
    "merchant": (0.22, 0.16, 0.12),
    "colonist": (0.28, 0.2, 0.15),
}

STRENGTH = {
    "walker": 2.5,
    "runner": 2.5,
    "brute": 1.4,
    "survivor": 0.35,
    "merchant": 0.35,
    "colonist": 0.35,
}

OUTFITS = (
    (0.24, 0.32, 0.20),
    (0.32, 0.24, 0.18),
    (0.18, 0.22, 0.28),
)

HEAD = (0.90, 1.0, 1.08, 0.82)

GORE_LINE = 0.4


def role_of(name):
    text = (name or "").lower()
    if "runner" in text:
        return "runner"
    if "brute" in text:
        return "brute"
    if "merchant" in text:
        return "merchant"
    if "colonist" in text:
        return "colonist"
    if "survivor" in text or "leader" in text or "player" in text:
        return "survivor"
    return "walker"


def eye_color(role):
    return EYE.get(role_of(role), EYE["walker"])


def eye_strength(role):
    return STRENGTH.get(role_of(role), STRENGTH["walker"])


def glows(role):
    return role_of(role) in ("walker", "runner", "brute")


def face_parts(head):
    x, y, z = head
    return (
        ("Face", (x, y + 0.13, z - 0.01), (0.11, 0.015, 0.09), "box"),
        ("EyeL", (x - 0.04, y + 0.145, z + 0.02), 0.018, "sphere"),
        ("EyeR", (x + 0.04, y + 0.145, z + 0.02), 0.018, "sphere"),
    )


def thumb_box(hand, side):
    x, y, z = hand
    sign = -1 if side == "L" else 1
    return (x + 0.05 * sign, y + 0.06, z - 0.02), (0.035, 0.05, 0.04)


def _channel(seed, index):
    mixed = (seed * 747796405 + index * 2891336453) & 0xFFFFFFFF
    unit = (mixed & 0xFFFF) / 65535.0
    return 0.92 + unit * 0.16


def clothing_tint(seed):
    outfit = OUTFITS[seed % 3]
    base = OUTFITS[0]
    head = HEAD[seed % 4]
    return (
        (outfit[0] / base[0]) * _channel(seed, 1),
        (outfit[1] / base[1]) * _channel(seed, 2) * head,
        (outfit[2] / base[2]) * _channel(seed, 3),
    )


def wounded(current, maximum):
    if maximum <= 0.01:
        return False
    return current / maximum < GORE_LINE
