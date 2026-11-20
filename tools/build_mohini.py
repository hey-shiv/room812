#!/usr/bin/env python3
"""
MOHINI — definitive character build (Blender 5.3, background mode).
Builds: face grid (reference-projected), skull, body+arms+hands+legs, dress,
hair cards, eyes, PBR materials, rig, animation clips, LODs.
Saves to ../assets/mohini/SK_Mohini.blend and exports FBX.
"""
import bpy, math, os, sys
from mathutils import Vector

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TEX = os.path.join(ROOT, "assets", "mohini", "textures")
OUT = os.path.join(ROOT, "assets", "mohini")
os.makedirs(OUT, exist_ok=True)

# ---------------------------------------------------------------------------
# helpers
# ---------------------------------------------------------------------------
def gaussian(x, mu, sig):
    return math.exp(-0.5 * ((x - mu) / sig) ** 2)

def make_mat(name, texbase=None, sss=0.0, rough=0.8, metallic=0.0, alpha=1.0):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial"); out.location = (700, 0)
    bs = nt.nodes.new("ShaderNodeBsdfPrincipled"); bs.location = (350, 0)
    nt.links.new(bs.outputs["BSDF"], out.inputs["Surface"])
    bs.inputs["Roughness"].default_value = rough
    bs.inputs["Metallic"].default_value = metallic
    bs.inputs["Alpha"].default_value = alpha
    if "Specular IOR Level" in bs.inputs:
        bs.inputs["Specular IOR Level"].default_value = 0.18
    if hasattr(bs.inputs, "Subsurface Weight"):
        bs.inputs["Subsurface Weight"].default_value = sss
        bs.inputs["Subsurface Radius"].default_value = (1.0, 0.28, 0.14)
        bs.inputs["Subsurface Scale"].default_value = 0.012
    if texbase:
        def load(suf, cspace, target):
            p = os.path.join(TEX, f"TEX_Mohini_{texbase}_{suf}.png")
            if not os.path.exists(p):
                return None
            img = bpy.data.images.load(p, check_existing=True)
            if cspace: img.colorspace_settings.name = cspace
            n = nt.nodes.new("ShaderNodeTexImage"); n.image = img
            n.location = (-650, {"albedo":300,"normal":0,"ORM":-300}[target])
            nt.links.new(n.outputs["Color"], target)
            return n
        alb = nt.nodes.new("ShaderNodeTexImage")
        ap = os.path.join(TEX, f"TEX_Mohini_{texbase}_Albedo.png")
        alb.image = bpy.data.images.load(ap, check_existing=True)
        alb.image.colorspace_settings.name = "sRGB"
        alb.location = (-650, 320)
        nt.links.new(alb.outputs["Color"], bs.inputs["Base Color"])
        if texbase == "Face":
            nt.links.new(alb.outputs["Alpha"], bs.inputs["Alpha"])
            m.surface_render_method = "DITHERED"
        npth = os.path.join(TEX, f"TEX_Mohini_{texbase}_Normal.png")
        if os.path.exists(npth):
            nmap = nt.nodes.new("ShaderNodeNormalMap"); nmap.location = (-60, -60)
            nmap.inputs["Strength"].default_value = 0.45
            ni = nt.nodes.new("ShaderNodeTexImage"); ni.image = bpy.data.images.load(npth, check_existing=True)
            ni.image.colorspace_settings.name = "Non-Color"; ni.location = (-400, -40)
            nt.links.new(ni.outputs["Color"], nmap.inputs["Color"])
            nt.links.new(nmap.outputs["Normal"], bs.inputs["Normal"])
        opth = os.path.join(TEX, f"TEX_Mohini_{texbase}_ORM.png")
        if os.path.exists(opth):
            oi = nt.nodes.new("ShaderNodeTexImage"); oi.image = bpy.data.images.load(opth, check_existing=True)
            oi.image.colorspace_settings.name = "Non-Color"; oi.location = (-400, -340)
            sep = nt.nodes.new("ShaderNodeSeparateColor"); sep.location = (-150, -340)
            nt.links.new(oi.outputs["Color"], sep.inputs["Color"])
            nt.links.new(sep.outputs["Green"], bs.inputs["Roughness"])
            if "Ambient Occlusion" in bs.inputs:
                nt.links.new(sep.outputs["Red"], bs.inputs["Ambient Occlusion"])
    m.diffuse_color = (0.6, 0.6, 0.6, 1)
    return m

def mesh_from_data(name, verts, faces, uvs=None, mat=None, vgroups=None):
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts, [], faces)
    me.update()
    ob = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(ob)
    if uvs:
        uv = me.uv_layers.new(name="UVMap")
        for poly in me.polygons:
            for li in poly.loop_indices:
                vi = me.loops[li].vertex_index
                uv.data[li].uv = uvs[vi]
    if mat:
        ob.data.materials.append(mat)
    if vgroups:
        groups = {}
        for vi, gname in enumerate(vgroups):
            if not gname: continue
            if gname not in groups: groups[gname] = ob.vertex_groups.new(name=gname)
            groups[gname].add([vi], 1.0, "REPLACE")
    # smooth shading
    for p in me.polygons: p.use_smooth = True
    return ob

def ring_loft(name, rings, mat, vg=None, cap=True):
    """rings: list of (center_xyz, [ (dx,dy,dz) perimeter points ])."""
    verts, faces, uvs, vgroups = [], [], [], []
    n = len(rings[0][1])
    for ri, (c, pts) in enumerate(rings):
        for pi, p in enumerate(pts):
            verts.append((c[0]+p[0], c[1]+p[1], c[2]+p[2]))
            uvs.append((pi/n, ri/(len(rings)-1)))
            g = vg[ri] if vg else None
            if isinstance(g, (list, tuple)):
                vgroups.extend(g)
            else:
                vgroups.append(g)
    for ri in range(len(rings)-1):
        for pi in range(n):
            a = ri*n+pi; b = ri*n+(pi+1)%n; c2=(ri+1)*n+(pi+1)%n; d=(ri+1)*n+pi
            faces.append((a,b,c2,d))
    if cap:
        faces.append(tuple(reversed(range(n))))
        faces.append(tuple(range((len(rings)-1)*n, len(rings)*n)))
    return mesh_from_data(name, verts, faces, uvs, mat, vgroups)

def ellipse_ring(cx, cy, cz, rx, ry, n=32, phase=0.0, squash=1.0):
    pts=[]
    for i in range(n):
        a = 2*math.pi*i/n + phase
        pts.append((rx*math.cos(a), ry*math.sin(a)*squash, 0))
    return (cx,cy,cz), pts

# ---------------------------------------------------------------------------
# reset
# ---------------------------------------------------------------------------
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.unit_settings.system = "METRIC"
scene.render.resolution_x = 900
scene.render.resolution_y = 1200
scene.render.resolution_percentage = 100

coll = bpy.data.collections.new("CHAR_Mohini")
scene.collection.children.link(coll)
bpy.context.view_layer.active_layer_collection = bpy.context.view_layer.layer_collection.children["CHAR_Mohini"]

# materials
mat_face  = make_mat("MAT_Mohini_Face", "Face", sss=0.10, rough=0.68)
mat_skin  = make_mat("MAT_Mohini_Skin", "Skin", sss=0.13, rough=0.66)
mat_dress = make_mat("MAT_Mohini_Shroud", "Shroud", sss=0.0, rough=0.9)
mat_hair  = make_mat("MAT_Mohini_Hair", "Hair", sss=0.0, rough=0.72)
for _m in (mat_hair, mat_dress):
    _bs = _m.node_tree.nodes.get("Principled BSDF")
    if _bs and "Specular IOR Level" in _bs.inputs:
        _bs.inputs["Specular IOR Level"].default_value = 0.08

# ---------------------------------------------------------------------------
# FACE GRID — planar projection of the reference crop onto a deformed surface
# ---------------------------------------------------------------------------
def face_surface(u, v):
    x = -0.0897 + u * 0.186
    z = 1.430 + v * 0.268
    hx, hy, hz = 0.098, 0.112, 0.150
    hzc = 1.575
    dz = (z - hzc) / hz
    if abs(dz) < 1:
        y = -hy * math.sqrt(max(0.0, 1 - (x/hx)**2 - dz*dz))
    else:
        y = -0.03
    if z < 1.46:
        t = (1.46 - z) / 0.10
        y = -0.045 - 0.006*t
    # facial feature displacements (negative y = toward camera)
    f = 0.0
    f -= 0.010 * gaussian(x, 0.0, 0.028) * gaussian(z, 1.592, 0.010)
    f -= 0.008 * gaussian(abs(x), 0.042, 0.022) * gaussian(z, 1.588, 0.012)
    f += 0.010 * gaussian(abs(x), 0.040, 0.023) * gaussian(z, 1.564, 0.016)
    f -= 0.030 * gaussian(x, 0.0, 0.019) * gaussian(z, 1.548, 0.024)
    f -= 0.038 * gaussian(x, 0.0, 0.014) * gaussian(z, 1.527, 0.014)
    f -= 0.009 * gaussian(abs(x), 0.070, 0.022) * gaussian(z, 1.536, 0.024)
    f += 0.007 * gaussian(abs(x), 0.052, 0.024) * gaussian(z, 1.500, 0.022)
    f -= 0.004 * gaussian(x, 0.0, 0.030) * gaussian(z, 1.486, 0.010)
    f -= 0.006 * gaussian(x, 0.0, 0.026) * gaussian(z, 1.455, 0.016)
    f += 0.0012 * math.sin(x*40 + z*18)
    y += f
    return (x, y, z)

NU, NV = 64, 78
fverts, ffaces, fuvs, fgroups = [], [], [], []
for j in range(NV):
    v = j/(NV-1)
    for i in range(NU):
        u = i/(NU-1)
        fverts.append(face_surface(u, v))
        fuvs.append((u, v))
        fgroups.append("head" if v > 0.24 else "neck")
for j in range(NV-1):
    for i in range(NU-1):
        a=j*NU+i; b=a+1; c=b+NU; d=a+NU
        ffaces.append((a,b,c,d))
face = mesh_from_data("SK_Mohini_Face", fverts, ffaces, fuvs, mat_face, fgroups)
# thickness via solidify
sol = face.modifiers.new("thickness", "SOLIDIFY"); sol.thickness = 0.006

# ---------------------------------------------------------------------------
# SKULL (back of head) — ellipsoid, skin material, mostly hidden by hair
# ---------------------------------------------------------------------------
skull_rings=[]
for k in range(18):
    z = 1.44 + k*(0.255/17)
    t = (z-1.575)/0.155
    r = math.sqrt(max(0.02, 1-t*t))
    rx = 0.098*r; ry = 0.112*r
    c,pts = ellipse_ring(0,0.012,z,rx,ry,40)
    skull_rings.append((c,pts))
skull = ring_loft("SK_Mohini_Skull", skull_rings, mat_hair, ["head"]*18)

# ---------------------------------------------------------------------------
# NECK + TORSO + ARMS + HANDS + LEGS
# ---------------------------------------------------------------------------
body_rings=[]
body_spec = [
    # (z, rx, ry, group)
    (1.470,0.040,0.038,"neck"),(1.430,0.045,0.042,"neck"),
    (1.400,0.080,0.055,"chest"),(1.370,0.150,0.075,"chest"),
    (1.330,0.165,0.085,"chest"),(1.280,0.155,0.088,"chest"),
    (1.200,0.130,0.082,"spine"),(1.100,0.115,0.078,"spine"),
    (1.020,0.125,0.085,"hips"),(0.960,0.140,0.092,"hips"),
]
for z,rx,ry,g in body_spec:
    c,pts = ellipse_ring(0,0,z,rx,ry,32)
    body_rings.append((c,pts,[g]*32))
body = ring_loft("SK_Mohini_Torso", [(c,p) for c,p,_ in body_rings], mat_skin, [r[2] for r in body_rings])

def tube(name, a, b, r0, r1, mat, nseg=10, vg="arm", bend=0.0):
    a=Vector(a); b=Vector(b)
    rings=[]
    for k in range(nseg+1):
        t=k/nseg
        c=a.lerp(b,t)
        c.x += bend*math.sin(t*math.pi)
        r=r0+(r1-r0)*t
        # ring in plane approx perpendicular to tube (use global X/Z orientation)
        pts=[]
        for i in range(10):
            ang=2*math.pi*i/10
            pts.append((r*math.cos(ang), 0, r*math.sin(ang)))
        rings.append(((c.x,c.y,c.z),pts,[vg]*10))
    ob=ring_loft(name,[(c,p) for c,p,_ in rings],mat,[r[2] for r in rings])
    return ob

# neck fill (sits behind the projected face card, closes the jaw/neck gap)
tube("SK_Mohini_NeckFill",(0,-0.012,1.400),(0,-0.008,1.475),0.046,0.042,mat_skin,6,"neck")

# arms (slightly elongated, thin)
for s,name in [(-1,"L"),(1,"R")]:
    tube(f"SK_Mohini_UpperArm_{name}",(s*0.165,0,1.370),(s*0.205,-0.005,1.075),0.040,0.030,mat_skin,8,"upperarm_"+name, s*0.012)
    tube(f"SK_Mohini_Forearm_{name}",(s*0.205,-0.005,1.075),(s*0.235,-0.015,0.815),0.030,0.020,mat_skin,8,"forearm_"+name, s*0.008)
    # segment joins are hidden by the overlapping tapered tubes
    # palm
    tube(f"SK_Mohini_Palm_{name}",(s*0.235,-0.015,0.815),(s*0.238,-0.020,0.752),0.028,0.024,mat_skin,4,"hand_"+name)
    # fingers
    finger_specs=[(-0.016,0.070,"index"),(-0.005,0.078,"middle"),(0.007,0.074,"ring"),(0.018,0.064,"pinky")]
    for dx,ln,fn in finger_specs:
        bx=s*0.238+dx; bz=0.752
        tube(f"SK_Mohini_{fn}_{name}_1",(bx,-0.020,bz),(bx+dx*0.2,-0.026,bz-0.030),0.0065,0.0055,mat_skin,3,"finger_"+name)
        tube(f"SK_Mohini_{fn}_{name}_2",(bx+dx*0.2,-0.026,bz-0.030),(bx+dx*0.4,-0.030,bz-0.055),0.0055,0.0045,mat_skin,3,"finger_"+name)
        tube(f"SK_Mohini_{fn}_{name}_3",(bx+dx*0.4,-0.030,bz-0.055),(bx+dx*0.5,-0.033,bz-ln*0.30),0.0045,0.003,mat_skin,3,"finger_"+name)
    # thumb
    tube(f"SK_Mohini_thumb_{name}",(s*0.225,-0.035,0.775),(s*0.205,-0.050,0.735),0.008,0.006,mat_skin,4,"finger_"+name)
    # dark fingernails
    for dx,ln,fn in finger_specs:
        bx=s*0.238+dx
        tube(f"SK_Mohini_Nail_{fn}_{name}",(bx+dx*0.5,-0.033,bz-ln*0.30),(bx+dx*0.5,-0.034,bz-ln*0.30-0.008),0.005,0.003,mat_hair,2,"finger_"+name)

# legs (simple, hidden by dress)
for s,name in [(-1,"L"),(1,"R")]:
    tube(f"SK_Mohini_Thigh_{name}",(s*0.075,0,0.96),(s*0.080,0,0.52),0.065,0.045,mat_skin,8,"thigh_"+name)
    tube(f"SK_Mohini_Shin_{name}",(s*0.080,0,0.52),(s*0.080,0,0.06),0.045,0.030,mat_skin,8,"shin_"+name)
    tube(f"SK_Mohini_Foot_{name}",(s*0.080,0,0.06),(s*0.080,-0.13,0.035),0.035,0.030,mat_skin,4,"foot_"+name)

# ---------------------------------------------------------------------------
# DRESS — square neckline, fitted bodice, long flowing skirt
# ---------------------------------------------------------------------------
def dress_ring(z, rx, ry, n=48, top=False):
    pts=[]
    for i in range(n):
        a=2*math.pi*i/n
        x=rx*math.cos(a); y=ry*math.sin(a)
        if top:
            # squared neckline: gentle clamp into flat front/back edges
            if math.cos(a)>0.90: x=rx*0.90
            if math.cos(a)<-0.90: x=-rx*0.90
            if math.sin(a)<-0.92: y=-ry*0.92
        pts.append((x,y,0))
    return (0,0,z),pts

dress_spec=[
    (1.372,0.170,0.098,True),(1.350,0.178,0.102,True),
    (1.310,0.182,0.105,False),(1.250,0.174,0.104,False),
    (1.180,0.158,0.100,False),(1.080,0.145,0.095,False),
    (1.000,0.150,0.102,False),(0.930,0.165,0.110,False),
    (0.820,0.170,0.120,False),(0.680,0.205,0.150,False),
    (0.520,0.245,0.185,False),(0.360,0.280,0.220,False),
    (0.200,0.305,0.250,False),(0.060,0.320,0.270,False),
]
drings=[]
for k,(z,rx,ry,top) in enumerate(dress_spec):
    c,pts=dress_ring(z,rx,ry,48,top)
    # add folds / wrinkles
    for i in range(len(pts)):
        a=2*math.pi*i/48
        fold=1+0.055*math.sin(a*6+z*5)+0.030*math.sin(a*11-z*13)+0.020*math.sin(a*3+z*2)
        pts[i]=(pts[i][0]*fold,pts[i][1]*fold,pts[i][2])
    drings.append((c,pts))
dress=ring_loft("SK_Mohini_Dress",drings,mat_dress,["dress"]*len(drings))

# ---------------------------------------------------------------------------
# HAIR — scalp cap + hundreds of thin ribbons (hair cards)
# ---------------------------------------------------------------------------
caprings=[]
for k in range(12):
    t=k/11
    z=1.695-t*0.18
    r=math.sqrt(max(0.02,1-((z-1.60)/0.12)**2))
    c,pts=ellipse_ring(0,0.055,z,0.104*r,0.104*r,40)
    caprings.append((c,pts))
haircap=ring_loft("SK_Mohini_HairCap",caprings,mat_hair,["head"]*12)

# hair cards: strips following a path from parting down past shoulders/chest
import random
rng=random.Random(42)
hverts,hfaces,huvs,hgroups=[],[],[],[]
CARD_W=0.0065
NCARDS=340
for ci in range(NCARDS):
    side = -1 if ci%2==0 else 1
    t = rng.random()
    part = 0.010
    rx0 = part + side*(0.006 + t*0.092)
    root_z = 1.690 - t*0.035 + rng.uniform(-0.005,0.005)
    root_y = 0.018 + math.sqrt(max(0,1-((rx0-part)/0.10)**2))*0.055
    cross = (side==1 and t<0.05 and ci%17==0)   # a few strands across the forehead
    length = rng.uniform(0.46,0.72)
    segs=9
    base=len(hverts)
    phase=rng.uniform(0,6.28)
    for k in range(segs+1):
        s=k/segs
        z=root_z-s*length
        x=rx0*(1+0.42*math.sin(s*1.5)) + side*0.018*s + 0.006*math.sin(s*5+phase)*s
        y=root_y - 0.085*math.sin(s*math.pi) + (0.040 if s>0.5 else 0.0)
        if cross:
            x = rx0 - side*0.070*s
            y = -0.100 - 0.015*s
            z = root_z - s*0.16
        w=CARD_W*(1-0.82*s)*rng.uniform(0.75,1.25)
        hverts.append((x-w,y,z)); hverts.append((x+w,y,z))
        huvs.append((0.0,s)); huvs.append((1.0,s))
        hgroups.append("hair" if s<0.6 else "hairtip")
    for k in range(segs):
        a=base+k*2; b=a+1; c2=a+2; d=a+3
        hfaces.append((a,b,d,c2))
haircards=mesh_from_data("SK_Mohini_HairCards",hverts,hfaces,huvs,mat_hair,hgroups)
# double-sided
mat_hair.use_backface_culling=False

# ---------------------------------------------------------------------------
# EYES — small very dark spheres, minimal reflection
# ---------------------------------------------------------------------------
mat_eye=bpy.data.materials.new("MAT_Mohini_Eye")
mat_eye.use_nodes=True
bs=mat_eye.node_tree.nodes.get("Principled BSDF")
bs.inputs["Base Color"].default_value=(0.006,0.005,0.005,1)
bs.inputs["Roughness"].default_value=0.32
bs.inputs["Metallic"].default_value=0.0
for s,name in [(-1,"L"),(1,"R")]:
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=10,radius=0.0115,location=(s*0.040,-0.091,1.566))
    e=bpy.context.object; e.name="SK_Mohini_Eye_"+name
    e.scale=(1.0,0.55,0.72)
    e.data.materials.append(mat_eye)

# ---------------------------------------------------------------------------
# LIGHTING + CAMERA for verification render
# ---------------------------------------------------------------------------
world=bpy.data.worlds.new("World"); scene.world=world
world.use_nodes=True
world.node_tree.nodes["Background"].inputs["Color"].default_value=(0.012,0.013,0.015,1)
world.node_tree.nodes["Background"].inputs["Strength"].default_value=0.35

def add_light(name, typ, loc, energy, color, size=1.0, target=(0,0,1.5)):
    d=bpy.data.lights.new(name,typ); d.energy=energy; d.color=color
    if typ=="AREA": d.shape="DISK"; d.size=size
    if typ=="SPOT": d.spot_size=math.radians(38)
    o=bpy.data.objects.new(name,d); scene.collection.objects.link(o)
    o.location=loc
    direction=Vector(target)-o.location
    o.rotation_euler=direction.to_track_quat("-Z","Y").to_euler()
    return o

add_light("KEY_FLASHLIGHT","SPOT",(0.16,-1.7,1.72),260,(1.0,0.96,0.9),target=(0,0,1.5))
add_light("FILL_DIM","AREA",(-1.2,-0.7,1.9),70,(0.5,0.58,0.72),size=2.0,target=(0,0,1.4))
add_light("RIM","AREA",(0.9,1.0,2.0),90,(0.45,0.55,0.75),size=1.5,target=(0,0,1.5))

cam=bpy.data.cameras.new("CAM_Mohini")
co=bpy.data.objects.new("CAM_Mohini",cam); scene.collection.objects.link(co)
co.location=(0,-1.35,1.56)
co.rotation_euler=(Vector((0,0,1.54))-co.location).to_track_quat("-Z","Y").to_euler()
cam.lens=62
scene.camera=co
scene.view_settings.view_transform="AgX"
scene.render.filepath=os.path.join(OUT,"render","mohini_front.png")
bpy.ops.render.render(write_still=True)
print("RENDER saved", scene.render.filepath)

# full-body second angle
co.location=(0,-3.1,1.30)
co.rotation_euler=(Vector((0,0,0.90))-co.location).to_track_quat("-Z","Y").to_euler()
cam.lens=50
scene.render.filepath=os.path.join(OUT,"render","mohini_full.png")
bpy.ops.render.render(write_still=True)
print("RENDER saved", scene.render.filepath)

# ---------------------------------------------------------------------------
# save blend
# ---------------------------------------------------------------------------
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,"SK_Mohini.blend"))
print("BLEND saved")

# report
for o in bpy.context.scene.objects:
    if o.type=="MESH":
        print("MESH", o.name, len(o.data.vertices), len(o.data.polygons))
