"""Validate exported SCML references and render a source animation preview.

This checks sprite geometry/source composition, not the Unity renderer.
Run with Python + Pillow: verify-anims.py [--preview output-directory].
"""
import argparse
import hashlib
import math
import re
from pathlib import Path
import xml.etree.ElementTree as ET
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
NAMES = ["baiye_isotope_separator", "baiye_tritium_breeder", "baiye_fusion_reactor", "baiye_triple_alpha"]

def symbol(name):
    return re.sub(r"_\d+$", "", Path(name).stem)

def load(name):
    folder = ROOT / "art/source" / name
    tree = ET.parse(folder / (name + ".scml")).getroot()
    files = {f.attrib["id"]: f.attrib for f in tree.findall("folder/file")}
    anims = {a.attrib["name"]: a for a in tree.findall("entity/animation")}
    return folder, files, anims

def verify(name):
    folder, files, anims = load(name)
    assert len(files) == 108 and len(anims) == 10
    for f in files.values():
        with Image.open(folder / f["name"]) as img:
            assert img.size == (int(f["width"]), int(f["height"])), f["name"]
            assert img.mode == "RGBA", f["name"]
    for a in anims.values():
        for timeline in a.findall("timeline"):
            for key in timeline.findall("key"):
                obj = key.find("object")
                assert obj.attrib["file"] in files
                assert symbol(timeline.attrib["name"]) == symbol(files[obj.attrib["file"]]["name"]), (name,a.attrib["name"],timeline.attrib["name"])
    for side in ("left", "right"):
        meter = anims["meter_" + side].find("timeline").findall("key")
        assert len(meter) == 51
        first = files[meter[0].find("object").attrib["file"]]
        last = files[meter[-1].find("object").attrib["file"]]
        assert first["pivot_y"] == last["pivot_y"] == "0"
        with Image.open(folder / first["name"]) as img:
            assert not img.getchannel("A").getbbox(), "empty meter has visible pixels"
        with Image.open(folder / last["name"]) as img:
            assert img.getchannel("A").getbbox(), "full meter invisible"
    for a in ("off", "on", "place", "working_pre", "working_loop", "working_pst"):
        timelines = {t.attrib["name"]: t for t in anims[a].findall("timeline")}
        assert "body_1" not in timelines, "full tanks baked into operating background"
        assert {"motion_0","meter_left_target_0","meter_right_target_0"} <= timelines.keys()
    motion = anims["working_loop"].findall("timeline")[1].findall("key")
    assert len(motion) == 16
    assert len({tuple(sorted(k.find("object").attrib.items())) for k in motion}) > 4
    assert anims["working_loop"].attrib["looping"] == "true"
    with Image.open(folder / "ui_0.png") as ui:
        assert ui.size == (128,128)
    atlas = ROOT / "anim/assets" / name / (name + "_0.png")
    # Converter file names differ by version; find the atlas when needed.
    if not atlas.exists(): atlas = next((ROOT / "anim/assets" / name).glob("*.png"))
    with Image.open(atlas) as img:
        assert img.size == (2048,2048), "unexpected texture memory growth"
    print("PASS",name,"10 animations, 108 files, 51 meter positions, valid references, 2048 atlas")
    return folder,files,anims

def draw_object(canvas, folder, files, obj, origin):
    attrs = obj.attrib
    f = files[attrs["file"]]
    if "target" in f["name"]: return
    image = Image.open(folder / f["name"]).convert("RGBA")
    sx,sy = float(attrs.get("scale_x",1)),float(attrs.get("scale_y",1))
    image = image.resize((max(1,round(image.width*abs(sx))),max(1,round(image.height*abs(sy)))),Image.Resampling.LANCZOS)
    alpha = float(attrs.get("a",1))
    if alpha < 1: image.putalpha(image.getchannel("A").point(lambda v: round(v*alpha)))
    px = float(attrs.get("pivot_x",f.get("pivot_x",.5))) * image.width
    py = (1-float(attrs.get("pivot_y",f.get("pivot_y",.5)))) * image.height
    angle = float(attrs.get("angle",0))
    if angle:
        assert abs(px-image.width/2) < 1 and abs(py-image.height/2) < 1
        image = image.rotate(angle,resample=Image.Resampling.BICUBIC,expand=True)
        px,py = image.width/2,image.height/2
    x = origin[0]+float(attrs.get("x",0))-px
    y = origin[1]-float(attrs.get("y",0))-py
    canvas.alpha_composite(image,(round(x),round(y)))

def render(sample,index,left,right,origin,canvas,animation="working_loop"):
    folder,files,anims = sample
    a=anims[animation]
    keys = a.find("mainline").findall("key")
    key=keys[index % len(keys)]
    timelines={t.attrib["id"]:t for t in a.findall("timeline")}
    objects=[]
    for ref in key.findall("object_ref"):
        timeline=timelines[ref.attrib["timeline"]]
        objects.append((int(ref.attrib["z_index"]),timeline.findall("key")[int(ref.attrib["key"])].find("object")))
    for _,obj in sorted(objects,key=lambda v:v[0]): draw_object(canvas,folder,files,obj,origin)
    for side,ratio in (("left",left),("right",right)):
        target=next(obj for _,obj in objects if files[obj.attrib["file"]]["name"] == "meter_"+side+"_target_0.png")
        keys=anims["meter_"+side].find("timeline").findall("key")
        obj=keys[round(ratio*50)].find("object")
        pos=(origin[0]+float(target.attrib["x"]),origin[1]-float(target.attrib["y"]))
        draw_object(canvas,folder,files,obj,pos)

def preview(samples, destination):
    destination.mkdir(parents=True,exist_ok=True)
    frames=[]
    font=ImageFont.truetype("C:/Windows/Fonts/msyh.ttc",18)
    for i in range(16):
        canvas=Image.new("RGBA",(1640,470),(27,32,37,255))
        d=ImageDraw.Draw(canvas)
        d.text((24,12),"源动画离线预览 · 料位为测试值，非游戏实测",font=font,fill="#d6e3ec")
        d.line((0,430,1640,430),fill="#819392",width=3)
        for sample,x in zip(samples,(170,490,910,1390)): render(sample,i,i/15,1-i/15,(x,430),canvas)
        frames.append(canvas.convert("RGB"))
    frames[4].save(destination/"animation-preview.png")
    frames[0].save(destination/"animation-preview.gif",save_all=True,append_images=frames[1:],duration=100,loop=0)
    print("Preview:",destination/"animation-preview.gif")

if __name__ == "__main__":
    p=argparse.ArgumentParser();p.add_argument("--preview",type=Path);args=p.parse_args()
    samples=[verify(n) for n in NAMES]
    if args.preview:preview(samples,args.preview)
