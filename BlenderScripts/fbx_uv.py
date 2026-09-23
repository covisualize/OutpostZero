"""Read UV layouts straight from binary FBX files and measure how much of each layout overlaps.

No bpy: the audit and CI run this on the committed files. Only the node records the check needs are
decoded (Geometry > PolygonVertexIndex and LayerElementUV > UV/UVIndex); everything else is skipped.
"""

import struct
import zlib

HEADER = b"Kaydara FBX Binary  \x00"
ARRAY_TYPES = {"d": ("d", 8), "f": ("f", 4), "i": ("i", 4), "l": ("q", 8), "b": ("?", 1)}
SCALAR_TYPES = {"Y": ("h", 2), "C": ("?", 1), "I": ("i", 4), "F": ("f", 4), "D": ("d", 8), "L": ("q", 8)}
RESOLUTION = 256
"""Texels per side when rasterising a layout; fine enough to see a 0.02 island margin collapse."""
OVERLAP_LIMIT = 0.02
"""Share of covered texels that more than one triangle may claim before the layout counts as overlapping."""


class FbxError(ValueError):
    pass


class Node(object):
    __slots__ = ("name", "props", "children")

    def __init__(self, name, props, children):
        self.name = name
        self.props = props
        self.children = children

    def child(self, name):
        for node in self.children:
            if node.name == name:
                return node
        return None

    def all(self, name):
        return [node for node in self.children if node.name == name]


def _read_prop(data, at):
    kind = chr(data[at])
    at += 1
    if kind in SCALAR_TYPES:
        fmt, size = SCALAR_TYPES[kind]
        return struct.unpack_from("<" + fmt, data, at)[0], at + size
    if kind in ARRAY_TYPES:
        fmt, size = ARRAY_TYPES[kind]
        count, encoding, length = struct.unpack_from("<III", data, at)
        at += 12
        raw = data[at:at + length]
        if encoding == 1:
            raw = zlib.decompress(raw)
        return list(struct.unpack("<%d%s" % (count, fmt), raw[:count * size])), at + length
    if kind in ("S", "R"):
        (length,) = struct.unpack_from("<I", data, at)
        at += 4
        return bytes(data[at:at + length]), at + length
    raise FbxError("unknown property type %r" % kind)


def _read_node(data, at, wide):
    if wide:
        end, count, _length = struct.unpack_from("<QQQ", data, at)
        at += 24
    else:
        end, count, _length = struct.unpack_from("<III", data, at)
        at += 12
    name_length = data[at]
    at += 1
    if end == 0:
        return None, at + name_length
    name = data[at:at + name_length].decode("ascii", "replace")
    at += name_length
    props = []
    for _ in range(count):
        value, at = _read_prop(data, at)
        props.append(value)
    children = []
    sentinel = 25 if wide else 13
    while at < end - sentinel:
        child, at = _read_node(data, at, wide)
        if child is None:
            break
        children.append(child)
    return Node(name, props, children), end


def parse(data):
    """Top-level FBX nodes of a binary file."""
    if not data.startswith(HEADER):
        raise FbxError("not a binary FBX")
    (version,) = struct.unpack_from("<I", data, 23)
    wide = version >= 7500
    at = 27
    nodes = []
    while at < len(data):
        node, at = _read_node(data, at, wide)
        if node is None:
            break
        nodes.append(node)
    return nodes


def layouts(data):
    """One (name, triangles) per mesh, each triangle three (u, v) points from its first UV layer."""
    result = []
    for top in parse(data):
        if top.name != "Objects":
            continue
        for geometry in top.all("Geometry"):
            polygons = geometry.child("PolygonVertexIndex")
            layer = geometry.child("LayerElementUV")
            name = geometry.props[1].split(b"\x00")[0].decode("utf-8", "replace") if len(geometry.props) > 1 else ""
            if polygons is None or layer is None:
                result.append((name, None))
                continue
            uv = layer.child("UV").props[0]
            index_node = layer.child("UVIndex")
            mapping = layer.child("MappingInformationType").props[0]
            corners = polygons.props[0]
            if index_node is not None:
                uv_index = index_node.props[0]
            elif mapping == b"ByPolygonVertex":
                uv_index = list(range(len(corners)))
            else:
                uv_index = [c if c >= 0 else ~c for c in corners]
            triangles = []
            polygon = []
            for corner, vertex in enumerate(corners):
                slot = uv_index[corner]
                polygon.append((uv[slot * 2], uv[slot * 2 + 1]))
                if vertex < 0:
                    for k in range(1, len(polygon) - 1):
                        triangles.append((polygon[0], polygon[k], polygon[k + 1]))
                    polygon = []
            result.append((name, triangles))
    return result


def _edge(a, b, p):
    return (b[0] - a[0]) * (p[1] - a[1]) - (b[1] - a[1]) * (p[0] - a[0])


def overlap(triangles, resolution=RESOLUTION):
    """(covered texels, texels claimed by two or more triangles) for a layout wrapped into 0..1."""
    claims = {}
    for a, b, c in triangles:
        area = _edge(a, b, c)
        if abs(area) < 1e-12:
            continue
        if area < 0:
            b, c = c, b
        us = (a[0], b[0], c[0])
        vs = (a[1], b[1], c[1])
        x0 = int(min(us) * resolution)
        x1 = int(max(us) * resolution) + 1
        y0 = int(min(vs) * resolution)
        y1 = int(max(vs) * resolution) + 1
        for y in range(y0, y1):
            py = (y + 0.5) / resolution
            for x in range(x0, x1):
                p = ((x + 0.5) / resolution, py)
                if _edge(a, b, p) > 0 and _edge(b, c, p) > 0 and _edge(c, a, p) > 0:
                    key = (x % resolution, y % resolution)
                    claims[key] = claims.get(key, 0) + 1
    covered = len(claims)
    doubled = sum(1 for count in claims.values() if count > 1)
    return covered, doubled


def report(path, resolution=RESOLUTION):
    """Per-mesh (name, covered, doubled, share) for one FBX file; share is None when the mesh has no UVs."""
    with open(path, "rb") as handle:
        data = handle.read()
    rows = []
    for name, triangles in layouts(data):
        if triangles is None:
            rows.append((name, 0, 0, None))
            continue
        covered, doubled = overlap(triangles, resolution)
        rows.append((name, covered, doubled, doubled / float(covered) if covered else 0.0))
    return rows


def problems(path, relative, limit=OVERLAP_LIMIT):
    found = []
    for name, _covered, doubled, share in report(path):
        if share is None:
            found.append("{0} {1} has no UV layer".format(relative, name))
        elif share > limit:
            found.append("{0} {1} UVs overlap on {2:.1%} of the layout".format(relative, name, share))
    return found
