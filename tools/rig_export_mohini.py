#!/usr/bin/env python3
"""MOHINI — combine meshes, build production rig, author 10 animation clips,
create LODs, export FBX."""
import bpy, math, os
from mathutils import Vector

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BLEND = os.path.join(ROOT, "assets", "mohini", "SK_Mohini.blend")
OUT   = os.path.join(ROOT, "assets", "mohini")
bpy.ops.wm.open_mainfile(filepath=BLEND)
scene = bpy.context.scene

# ---------------------------------------------------------------------------
# 1. COMBINE MESHES BY MATERIAL
# ---------------------------------------------------------------------------
def combine(name, objs):
    objs = [o for o in objs if o and o.type == "MESH"]
    if not objs: return None
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    j = bpy.context.object; j.name = name
    return j

skin  = [o for o in scene.objects if o.type=="MESH" and any(m.name=="MAT_Mohini_Skin"  for m in o.data.materials)]
hairm = [o for o in scene.objects if o.type=="MESH" and any(m.name=="MAT_Mohini_Hair"  for m in o.data.materials)]
eyem  = [o for o in scene.objects if o.type=="MESH" and "Eye_" in o.name]
face  = bpy.data.objects.get("SK_Mohini_Face")
dress = bpy.data.objects.get("SK_Mohini_Dress")
body  = combine("SK_Mohini_Body", skin)
hair  = combine("SK_Mohini_Hair", hairm)
eyes  = combine("SK_Mohini_Eyes", eyem)
for o in (body, hair, eyes, face, dress):
    if o: print("COMBINED", o.name, len(o.data.vertices), len(o.data.polygons))

# ---------------------------------------------------------------------------
# 2. REMAP VERTEX GROUPS TO BONE NAMES
# ---------------------------------------------------------------------------
NAME_MAP = {
    "head":"head","neck":"neck","chest":"chest","spine":"spine","hips":"pelvis",
    "upperarm_L":"upperarm.L","upperarm_R":"upperarm.R",
    "forearm_L":"forearm.L","forearm_R":"forearm.R",
    "hand_L":"hand.L","hand_R":"hand.R",
    "thigh_L":"thigh.L","thigh_R":"thigh.R",
    "shin_L":"shin.L","shin_R":"shin.R",
    "foot_L":"foot.L","foot_R":"foot.R",
    "hair":"hair_front.L","hairtip":"hair_back",
}
def remap(obj):
    if not obj: return
    # rename 1:1 groups
    for vg in list(obj.vertex_groups):
        if vg.name in NAME_MAP:
            vg.name = NAME_MAP[vg.name]
    # split finger groups by vertex position
    for side, sgn in (("L",-1),("R",1)):
        old = obj.vertex_groups.get("finger_"+side)
        if old:
            idx = [v.index for v in obj.data.vertices if any(g.group==old.index for g in v.groups)]
            old_idx = old.index
            # remove old group
            obj.vertex_groups.remove(old)
            fingers = ["index","middle","ring","pinky","thumb"]
            fgroups = {f:obj.vertex_groups.new(name=f+"."+side) for f in fingers}
            xs = sorted(idx, key=lambda i: obj.data.vertices[i].co.x)
            for rank, vi in enumerate(xs):
                v = obj.data.vertices[vi]
                if v.co.y < -0.038: f = "thumb"
                else: f = fingers[min(3, rank*4//max(1,len(idx)))]
                fgroups[f].add([vi], 1.0, "REPLACE")
    # dress follows body height
    if obj == dress:
        for vg in list(obj.vertex_groups): obj.vertex_groups.remove(vg)
        groups = {"chest":obj.vertex_groups.new(name="chest"),
                  "spine":obj.vertex_groups.new(name="spine"),
                  "pelvis":obj.vertex_groups.new(name="pelvis")}
        for v in obj.data.vertices:
            z = v.co.z
            g = "chest" if z>1.18 else ("spine" if z>0.98 else "pelvis")
            groups[g].add([v.index],1.0,"REPLACE")
    # hair: split sides
    if obj == hair:
        for vg in list(obj.vertex_groups):
            vgname = vg.name
            if vgname in ("hair_front.L","hair_back"):
                vgidx = vg.index
                idx=[v.index for v in obj.data.vertices if any(g.group==vgidx for g in v.groups)]
                obj.vertex_groups.remove(vg)
                prefix = "hair_front." if vgname.startswith("hair_front") else "hair_back."
                for side,test in (("L",lambda x:x<0),("R",lambda x:x>=0)):
                    sub=[i for i in idx if test(obj.data.vertices[i].co.x)]
                    if sub:
                        g=obj.vertex_groups.new(name=prefix+side)
                        g.add(sub,1.0,"REPLACE")
for o in (body,hair,eyes,face,dress): remap(o)

# ---------------------------------------------------------------------------
# 3. ARMATURE
# ---------------------------------------------------------------------------
armd = bpy.data.armatures.new("ARM_Mohini")
arm = bpy.data.objects.new("ARM_Mohini", armd)
scene.collection.objects.link(arm)
bpy.context.view_layer.objects.active = arm
arm.select_set(True)
bpy.ops.object.mode_set(mode="EDIT")

def bone(name, h, t, parent=None, connect=False):
    b = armd.edit_bones.new(name)
    b.head = h; b.tail = t
    if parent:
        b.parent = armd.edit_bones[parent]; b.use_connect = connect
    return b

bone("root",       (0,0,0.90),(0,0,1.00))
bone("pelvis",     (0,0,0.95),(0,0,1.05),"root")
bone("spine",      (0,0,1.05),(0,0,1.20),"pelvis",True)
bone("chest",      (0,0,1.20),(0,0,1.36),"spine",True)
bone("neck",       (0,0,1.38),(0,0,1.48),"chest")
bone("head",       (0,0,1.48),(0,0,1.66),"neck",True)
bone("jaw",        (0,-0.045,1.485),(0,-0.085,1.468),"head")
for side,s in (("L",-1),("R",1)):
    bone("shoulder."+side,(s*0.05,0,1.38),(s*0.17,0,1.37),"chest")
    bone("upperarm."+side,(s*0.17,0,1.37),(s*0.205,0,1.075),"shoulder."+side)
    bone("forearm."+side,(s*0.205,0,1.075),(s*0.235,-0.01,0.815),"upperarm."+side,True)
    bone("hand."+side,(s*0.235,-0.01,0.815),(s*0.238,-0.02,0.752),"forearm."+side,True)
    for f,dx in (("index",-0.016),("middle",-0.005),("ring",0.007),("pinky",0.018),("thumb",-0.013)):
        bone(f+"."+side,(s*0.238+dx,-0.025,0.752),(s*0.238+dx*1.4,-0.033,0.690),"hand."+side)
    bone("thigh."+side,(s*0.075,0,0.95),(s*0.08,0,0.52),"pelvis")
    bone("shin."+side,(s*0.08,0,0.52),(s*0.08,0,0.06),"thigh."+side,True)
    bone("foot."+side,(s*0.08,0,0.06),(s*0.08,-0.13,0.035),"shin."+side,True)
    bone("eye."+side,(s*0.040,-0.088,1.566),(s*0.040,-0.110,1.566),"head")
    bone("eyelid."+side,(s*0.040,-0.094,1.580),(s*0.040,-0.100,1.568),"head")
    bone("hair_front."+side,(s*0.015,0.03,1.68),(s*0.10,-0.04,1.42),"head")
    bone("hair_back."+side,(s*0.02,0.07,1.68),(s*0.09,0.10,1.20),"head")
bpy.ops.object.mode_set(mode="OBJECT")

# bind meshes
for o in (body,hair,eyes,face,dress):
    if not o: continue
    m = o.modifiers.new("Armature","ARMATURE"); m.object = arm
    o.parent = arm
    o.matrix_parent_inverse = arm.matrix_world.inverted()
print("RIG bones", len(armd.bones))

# ---------------------------------------------------------------------------
# 4. ANIMATIONS
# ---------------------------------------------------------------------------
def reset_pose():
    for pb in arm.pose.bones:
        pb.rotation_mode = "XYZ"
        pb.rotation_euler = (0,0,0)
        pb.location = (0,0,0)

def setrot(name, rx=0, ry=0, rz=0):
    pb = arm.pose.bones.get(name)
    if pb: pb.rotation_euler = (math.radians(rx),math.radians(ry),math.radians(rz))

def setloc(name, x=0, y=0, z=0):
    pb = arm.pose.bones.get(name)
    if pb: pb.location = (x,y,z)

ANIMS = {}

def anim_idle(f):
    w=f/120.0
    setrot("neck", ry=1.3*math.sin(w*2*math.pi))
    setrot("head", rx=-0.8*math.sin(w*2*math.pi+0.4), ry=1.8*math.sin(w*2*math.pi+0.7))
    if 55<f<62 or 96<f<101: setrot("eye.L",ry=4); setrot("eye.R",ry=4)

def anim_observe(f):
    w=f/120.0
    setrot("head", rx=-2+1.5*math.sin(w*math.pi), ry=3*math.sin(w*2*math.pi))
    setrot("neck", ry=1.5*math.sin(w*2*math.pi))

def anim_headturn(f):
    t=min(1, f/30) if f<30 else (1 if f<45 else max(0,1-(f-45)/15))
    setrot("head", ry=-52*t, rx=-3*t)
    setrot("neck", ry=-14*t)
    setrot("eye.L", ry=6*t); setrot("eye.R", ry=6*t)

def anim_walk(f):
    w=f/60.0*2*math.pi
    sw=math.sin(w)
    for side,s in (("L",1),("R",-1)):
        setrot("thigh."+side, rx=22*sw*s)
        setrot("shin."+side, rx=max(0,-18*sw*s))
        setrot("upperarm."+side, rx=-14*sw*s, rz=2*s)
        setrot("forearm."+side, rx=-12-6*sw*s)
    setloc("root", z=0.012*math.sin(w*2))
    setrot("spine", ry=2.0*sw, rz=1.2*sw)

def anim_stalk(f):
    w=f/90.0*2*math.pi
    sw=math.sin(w)
    for side,s in (("L",1),("R",-1)):
        setrot("thigh."+side, rx=14*sw*s)
        setrot("shin."+side, rx=max(0,-11*sw*s))
        setrot("upperarm."+side, rx=-7*sw*s)
        setrot("forearm."+side, rx=-18)
    setrot("head", rx=9, ry=4*math.sin(w*0.5))
    setloc("root", y=-0.006*f/90.0, z=0.006*math.sin(w*2))

def anim_sudden(f):
    t = min(1,f/6) if f<6 else (1 if f<12 else max(0,1-(f-12)/12))
    setrot("head", ry=-40*t, rx=-16*t)
    setrot("neck", ry=-12*t)
    setrot("upperarm.L", rx=-55*t)
    setrot("forearm.L", rx=-50*t)

def anim_manifest(f):
    t=min(1,f/45)
    setloc("root", z=(1-t)*-0.06)
    for side in ("L","R"):
        setrot("upperarm."+side, rx=-14*t)
        setrot("hair_front."+side, ry=6*math.sin(f*0.2)*t)
    setrot("head", rx=-6*t)

def anim_disappear(f):
    t=f/90.0
    setloc("root", z=t*0.10, y=t*0.04)
    setrot("head", rx=10*t)
    for side in ("L","R"):
        setrot("upperarm."+side, rx=10*t)
        setrot("hair_front."+side, ry=5*math.sin(f*0.25)*t)

def anim_attack(f):
    t = min(1,f/14) if f<14 else (1 if f<32 else max(0,1-(f-32)/28))
    setloc("root", y=-0.22*t)
    setrot("spine", rx=14*t)
    setrot("head", rx=-18*t)
    for side in ("L","R"):
        setrot("upperarm."+side, rx=-78*t, rz=10*t)
        setrot("forearm."+side, rx=-28*t)
        setrot("hand."+side, rx=12*t)

def anim_search(f):
    w=f/120.0*2*math.pi
    setrot("head", ry=34*math.sin(w*0.5)-6, rx=-4+3*math.sin(w))
    setrot("neck", ry=10*math.sin(w*0.5))
    setrot("chest", ry=4*math.sin(w*0.25))

CLIPS = [
    ("ANIM_Mohini_Idle",120,anim_idle),("ANIM_Mohini_Observe",120,anim_observe),
    ("ANIM_Mohini_HeadTurn",60,anim_headturn),("ANIM_Mohini_Walk",60,anim_walk),
    ("ANIM_Mohini_Stalk",90,anim_stalk),("ANIM_Mohini_Sudden",30,anim_sudden),
    ("ANIM_Mohini_Manifest",90,anim_manifest),("ANIM_Mohini_Disappear",90,anim_disappear),
    ("ANIM_Mohini_Attack",60,anim_attack),("ANIM_Mohini_Search",120,anim_search),
]

arm.animation_data_create()
for name, nframes, fn in CLIPS:
    arm.animation_data.action = None
    reset_pose()
    scene.frame_set(1)
    for f in range(1,nframes+1):
        scene.frame_set(f)
        reset_pose()
        fn(f)
        for pb in arm.pose.bones:
            pb.keyframe_insert(data_path="rotation_euler", frame=f, group=pb.name)
        arm.pose.bones["root"].keyframe_insert(data_path="location", frame=f, group="root")
    act = arm.animation_data.action
    act.name = name
    tr = arm.animation_data.nla_tracks.new(); tr.name = name
    st = tr.strips.new(name, 1, act)
    st.action_frame_start=1; st.action_frame_end=nframes
    print("CLIP", name, nframes)

arm.animation_data.action = None
scene.frame_set(1)

# ---------------------------------------------------------------------------
# 5. LODs
# ---------------------------------------------------------------------------
def make_lod(src, ratio, suffix):
    cp = src.copy(); cp.data = src.data.copy(); cp.name = src.name+suffix
    scene.collection.objects.link(cp)
    mod = cp.modifiers.new("LOD","DECIMATE"); mod.ratio = ratio
    bpy.context.view_layer.objects.active = cp
    bpy.ops.object.modifier_apply(modifier=mod.name)
    return cp
for src in (body,dress,hair,face):
    if src: make_lod(src,0.45,"_LOD1")
for src in (body,dress,hair,face):
    if src: make_lod(src,0.22,"_LOD2")

# hide LODs from render (Unity will use its own LODGroup)
for o in scene.objects:
    if "_LOD" in o.name: o.hide_render=True

# ---------------------------------------------------------------------------
# 6. SAVE + EXPORT
# ---------------------------------------------------------------------------
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,"SK_Mohini_Rigged.blend"))

sel = [arm,body,hair,eyes,face,dress] + [o for o in scene.objects if "_LOD" in o.name]
bpy.ops.object.select_all(action="DESELECT")
for o in sel:
    if o: o.select_set(True)
bpy.context.view_layer.objects.active = arm
bpy.ops.export_scene.fbx(
    filepath=os.path.join(OUT,"SK_Mohini.fbx"),
    use_selection=True,
    object_types={"ARMATURE","MESH"},
    apply_unit_scale=True,
    apply_scale_options="FBX_SCALE_UNITS",
    axis_forward="-Z", axis_up="Y",
    bake_space_transform=False,
    add_leaf_bones=False,
    use_armature_deform_only=True,
    bake_anim=True,
    bake_anim_use_all_bones=True,
    bake_anim_use_nla_strips=True,
    bake_anim_use_all_actions=False,
    mesh_smooth_type="FACE",
    use_mesh_modifiers=True,
    path_mode="RELATIVE",
    embed_textures=False,
)
print("EXPORTED FBX")
