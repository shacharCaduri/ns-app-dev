"""Maintain named extraction rectangles; does not process image pixels."""
import json
from pathlib import Path
entries=[]
def add(source,folder,name,rect,canvas=None,mirror=False):
    entry=dict(source='Assets/Art/'+source,output='Assets/Art/'+folder+'/'+name+'.png',rect=rect)
    if canvas: entry['canvas']=canvas
    if mirror: entry['mirror']=True
    entries.append(entry)
def wizard(name,rect,direction='right',canvas=(256,256)):
    for face,flip in [(direction,False)]+([('left',True)] if direction=='right' else []):
        action,frame=name.rsplit('_',1)
        add('Characters/wizard-px.png','Characters/Wizard/'+action,'wizard_'+action+'_'+face+'_'+frame,rect,canvas,flip)
for i,(l,r) in enumerate([(40,184),(189,326),(326,467),(468,618)],1): wizard(f'idle_{i:02}',[l,55,r,255],'down')
for i,(l,r) in enumerate([(670,819),(818,956),(956,1088),(1088,1230)],1): wizard(f'walk_{i:02}',[l,55,r,255])
for i,r in enumerate([[48,298,234,490],[276,297,503,490],[505,295,730,490],[757,296,1004,490]],1):wizard(f'attack_{i:02}',r)
wizard('hurt_01',[47,526,248,701])
for i,r in enumerate([[385,532,539,699],[546,583,755,698],[763,588,965,698],[997,592,1210,698]],1):wizard(f'death_{i:02}',r)
for name,r in zip(['arcane','fire','ice','lightning','heal'],[[39,753,297,944],[294,750,528,945],[527,749,757,945],[746,748,998,950],[995,722,1234,951]]):wizard('cast_'+name+'_01',r,canvas=(288,256))
for face,r in zip(['down','up','left','right'],[[60,992,206,1183],[262,993,395,1183],[445,989,589,1183],[672,991,813,1183]]):wizard('direction_idle_01',r,face)
add('Characters/wizard-px.png','Effects/Projectiles','arcane_projectile_right_01',[1004,315,1227,470],(240,176))
add('Characters/wizard-px.png','Effects/Projectiles','arcane_projectile_left_01',[1004,315,1227,470],(240,176),True)
# Enemy rows: labels are excluded by the rectangles.
cols=[(15,208),(216,404),(414,601),(595,814),(818,1015),(1015,1240)]
for species,(top,bottom),actions in [
 ('blue_slime',(78,224),['idle','walk','walk','attack','hurt','death']),
 ('goblin',(304,451),['idle','walk','walk','attack','hurt','death']),
 ('skeleton_warrior',(529,696),['idle','walk','walk','attack','hurt','death']),
 ('cave_bat',(771,911),['idle','fly','fly','attack','hurt','death']),
 ('rocky_golem',(977,1166),['idle','walk','walk','attack','hurt','death'])]:
    for j,((left,right),action) in enumerate(zip(cols,actions)):
        rect=[left,top,right,bottom]
        if species=='skeleton_warrior' and action=='attack':rect=[608,508,814,696]
        frame=2 if j==2 else 1
        for face,flip in [('left',False),('right',True)]:
            add('Enemies/monsters-transparent.png',f'Enemies/{species}/{action}',f'{species}_{action}_{face}_{frame:02}',rect,(240,224),flip)
# Item rows.
def items(category,names,bounds,top,bottom):
    for name,(l,r) in zip(names,bounds):add('Items/items-transparent.png','Items/'+category,name,[l,top,r,bottom])
items('Potions',['potion_health_small','potion_health_medium','potion_health_large','potion_mana','potion_energy','potion_antidote','potion_speed','potion_shield','potion_rage','potion_revive'],[(19,125),(132,241),(248,373),(379,490),(497,610),(624,736),(753,869),(875,987),(995,1110),(1118,1240)],130,291)
items('Materials',['gold_coins','silver_coins','gem_shard','magic_crystal','soul_shard','monster_fang','bone_fragment','slime_jelly','bat_wing','goblin_knife','rocky_core'],[(12,127),(134,247),(260,340),(346,473),(478,560),(569,661),(663,780),(780,909),(909,1021),(1021,1127),(1128,1241)],430,578)
items('Utility',['key','magic_key','scroll','spell_scroll','bomb','trap_disarm_kit','torch','food_ration','heart_pickup','mana_orb','experience_orb','shield_orb'],[(13,102),(102,203),(207,307),(309,415),(418,511),(515,636),(639,729),(728,834),(838,939),(940,1040),(1041,1138),(1138,1247)],703,861)
items('RareDrops',['treasure_bag','relic_fragment','rune_tablet','enchanted_ring','mystery_chest'],[(35,228),(286,439),(499,698),(749,920),(975,1208)],984,1147)
# Terrain, construction pieces, props and crystals.
s='Environments/arena-objects-px.png'
def env(group,name,r):add(s,'Environments/'+group,name,r)
for i,(l,r) in enumerate(zip([29,113,194,273,351,458,534,600],[113,194,273,351,458,534,592,713]),1):env('Tiles/Grass',f'grass_surface_{i:02}',[l,39,r,138])
for name,(l,r) in zip(['block_01','block_02','block_03','edge_right','slope_down','block_04','slope_up','edge_left'],[(28,112),(119,192),(198,275),(280,349),(354,446),(447,527),(530,622),(625,714)]):env('Tiles/Grass','grass_'+name,[l,138,r,230])
for i,(l,r) in enumerate(zip([756,838,918,998,1078,1129,1207,1281,1344],[838,918,998,1078,1129,1207,1281,1344,1428]),1):env('Tiles/Stone',f'stone_surface_{i:02}',[l,45,r,137])
for i,(l,r) in enumerate([(755,840),(844,927),(932,1008),(1012,1080),(1081,1169),(1173,1252),(1253,1342),(1346,1428)],1):env('Tiles/Stone',f'stone_block_{i:02}',[l,140,r,230])
for i,(l,r) in enumerate([(24,117),(117,250),(257,346),(347,427),(429,515)],1):env('Platforms',f'grass_platform_{i:02}',[l,282,r,380])
for i,(l,r) in enumerate([(26,119),(121,261),(263,413),(414,504)],1):env('Platforms',f'stone_platform_{i:02}',[l,381,r,473])
for name,r in [('ledge_left_tall',[529,282,635,481]),('ledge_left_short',[637,282,732,400]),('ledge_center',[732,284,796,395]),('ledge_right_tall',[795,283,875,472]),('corner_left',[642,398,726,501]),('corner_right',[726,398,797,494])]:env('Ledges',name,r)
for name,r in [('stone_steps_small',[903,285,991,379]),('stone_steps_medium',[993,283,1093,379]),('stone_steps_block',[1093,286,1172,379]),('stone_slope_down',[1174,286,1272,379]),('stone_steps_low',[908,398,1016,483]),('grass_slope_up_01',[1014,391,1113,488]),('grass_slope_up_02',[1112,391,1216,488]),('stone_staircase',[1220,284,1429,481])]:env('Stairs',name,r)
for row,(top,bottom) in enumerate([(530,625),(625,723)],1):
 for col,(l,r) in enumerate([(26,126),(130,236),(239,339),(343,404)],1):env('Walls',f'stone_wall_{row:02}_{col:02}',[l,top,r,bottom])
for i,(l,r) in enumerate([(410,492),(495,582),(586,661),(663,749),(751,813)],1):env('Columns',f'ruin_column_{i:02}',[l,528,r,726])
for name,r in [('archway_pillar_left',[821,541,890,726]),('stone_archway',[882,523,1061,726]),('gate_cap',[1059,530,1135,593]),('iron_gate',[1064,588,1173,727]),('archway_pillar_right',[1174,530,1244,726])]:env('Architecture',name,r)
for name,r in [('banner_blue',[1246,528,1340,714]),('banner_red',[1340,528,1439,714])]:env('Banners',name,r)
for name,r in [('rock_cluster',[29,763,145,838]),('rock_large',[145,767,265,849]),('rock_small_01',[28,835,106,896]),('skull',[106,835,157,890]),('rock_small_02',[158,844,221,896]),('rock_small_03',[222,849,278,896]),('bush_small',[267,800,340,852]),('bush_medium',[345,787,422,852]),('grass_tuft',[422,819,482,867]),('grass_small',[277,859,327,896]),('flower_blue_01',[328,848,373,895]),('flower_blue_02',[373,852,406,895]),('mushroom_red_01',[406,857,437,895]),('mushroom_red_02',[437,862,462,895]),('dead_tree',[476,746,640,895]),('wood_crate_large',[635,789,718,884]),('wood_crate_small',[674,845,726,896]),('wood_barrel',[723,775,818,896])]:env('Props',name,r)
for name,r in [('crystal_blue_cluster',[825,756,989,905]),('crystal_blue_tall',[993,754,1131,903]),('crystal_amber',[1134,750,1271,903]),('crystal_purple',[1266,750,1435,903])]:env('Crystals',name,r)
for name,r in [('grass_strip',[24,943,333,1062]),('stone_strip',[333,946,603,1062]),('grass_floating_strip',[603,935,876,1062])]:env('Platforms',name,r)
for name,r in [('grass',[884,947,987,1061]),('lava',[993,946,1094,1061]),('snow',[1104,943,1205,1061]),('corrupted',[1210,944,1318,1061]),('sandstone',[1328,944,1431,1061])]:env('Tiles/Variants',name+'_tile',r)
# Progression and interaction sheet.
s='Environments/portal&co.png'
for name,r in [('portal_inactive',[14,46,208,241]),('portal_forming',[208,43,394,240]),('portal_active',[396,27,583,239]),('portal_upgraded',[584,24,780,240])]:env('Portals',name,r)
for i,(l,r) in enumerate([(799,973),(970,1127),(1125,1285),(1284,1442)],1):env('Portals',f'portal_swirl_{i:02}',[l,62,r,244])
for name,r in [('rune_door_closed',[17,281,185,473]),('iron_gate_closed',[184,282,342,475]),('rune_door_open',[342,272,509,475]),('crystal_barrier_large',[508,271,639,475]),('crystal_barrier_small',[639,289,777,475]),('progression_lock',[772,284,909,464])]:env('Progression',name,r)
for name,r in [('pressure_plate_pressed',[913,317,993,370]),('pressure_plate_raised',[993,301,1082,370]),('wall_switch_inactive',[1091,287,1174,368]),('wall_switch_active',[1175,287,1249,368]),('lever_left',[1260,291,1343,373]),('lever_right',[1341,285,1410,373]),('rune_totem_inactive',[969,394,1045,502]),('rune_totem_active',[1045,394,1131,502]),('monster_seal_inactive',[1148,395,1268,506]),('monster_seal_active',[1268,395,1388,506])]:env('Triggers',name,r)
for name,r in [('push_block',[18,549,124,665]),('rune_block',[123,548,231,665]),('metal_block',[229,547,345,665]),('wooden_crate',[346,548,455,665]),('explosive_barrel',[455,546,554,665]),('puzzle_key',[555,560,637,660]),('key_pedestal',[633,542,716,665]),('key_pedestal_column',[715,548,778,665]),('orb_socket',[777,528,878,665]),('energy_crystal',[879,548,957,665]),('mirror_statue_front',[955,529,1060,665]),('mirror_statue_diagonal',[1060,528,1182,665]),('mirror_statue_side',[1184,528,1299,665]),('mirror_statue_back',[1323,528,1443,667])]:env('Puzzle',name,r)
for name,r in [('spike_trap_retracted',[12,758,87,850]),('spike_trap_extended',[87,751,169,850]),('flame_jet_inactive',[187,752,242,853]),('flame_jet_active',[240,744,300,853]),('arrow_trap',[299,751,450,850]),('saw_trap_01',[450,751,523,851]),('saw_trap_02',[522,751,610,851]),('cracked_floor',[610,752,740,850]),('collapsing_platform_01',[742,753,821,818]),('collapsing_platform_02',[821,754,902,818]),('collapsing_platform_03',[902,752,974,851]),('collapsing_platform_04',[974,751,1087,851]),('poison_puddle',[1088,761,1211,844]),('ice_patch',[1210,751,1333,850]),('lava_tile',[1334,757,1445,850])]:env('Hazards',name,r)
for name,r in [('checkpoint_inactive',[11,895,91,1035]),('checkpoint_active',[91,890,176,1035]),('health_orb',[175,936,242,1008]),('mana_orb',[240,936,312,1008]),('treasure_chest_closed',[314,934,394,1014]),('treasure_chest_open',[395,921,489,1015]),('magic_crystal',[490,922,566,1019]),('stage_clear_emblem',[568,905,707,1016]),('skull_warning_sign',[702,934,805,1043])]:env('Rewards',name,r)
for name,r in [('magic_bridge_inactive',[815,938,879,1009]),('magic_bridge_active',[879,926,958,1010]),('teleport_pad_inactive',[955,943,1022,1009]),('teleport_pad_active',[1020,913,1093,1011]),('summon_circle_inactive',[1094,937,1176,1015]),('summon_circle_active',[1164,913,1252,1016]),('enemy_spawn_seal',[1248,919,1348,1025]),('enemy_spawn_rocks',[1348,924,1447,1028])]:env('Utility',name,r)
# Corrections after reviewing all contact sheets.
# Keep the independently drawn left-facing idle instead of replacing it with a mirror.
entries=[e for e in entries if not (e['output'].endswith('wizard_direction_idle_left_01.png') and e.get('mirror'))]
fixes={
 'magic_key':[109,703,203,861],
 'stone_archway':[883,533,1061,726],
 'archway_pillar_left':[822,541,883,726],
 'gate_cap':[1059,530,1135,589],
 'rock_large':[147,775,265,844],
 'rock_small_02':[158,848,221,896],
 'bush_medium':[345,787,422,845],
 'grass_tuft':[422,817,479,858],
 'crystal_blue_cluster':[825,765,989,905],
 'crystal_blue_tall':[993,765,1131,903],
 'wall_switch_inactive':[1091,287,1169,368],
 'lever_left':[1260,291,1331,367],
 'lever_right':[1331,285,1406,367],
 'monster_seal_inactive':[1148,395,1268,497],
 'monster_seal_active':[1268,395,1388,498],
 'key_pedestal_column':[719,548,778,665],
 'mirror_statue_front':[955,529,1052,665],
 'flame_jet_active':[240,751,299,853],
 'ice_patch':[1210,761,1333,850],
 'skull_warning_sign':[709,935,805,1028],
 'magic_bridge_active':[879,926,950,1010],
 'teleport_pad_active':[1020,920,1093,1011],
 'summon_circle_inactive':[1094,937,1168,1015],
 'enemy_spawn_seal':[1248,923,1340,1025],
}
for e in entries:
 name=Path(e['output']).stem
 if name in fixes:e['rect']=fixes[name]
 if name.startswith('goblin_walk_') and name.endswith('_02'):e['rect'][2]=585
 if name.startswith('cave_bat_attack_'):e['rect'][0]=603
 if name=='wood_crate_large':
  e['output']=e['output'].replace('wood_crate_large','wood_crate_stack');e['rect']=[635,789,726,896]
 if name=='stone_staircase':e['polygon']=[[1222,417],[1345,282],[1429,282],[1429,361],[1284,484],[1222,484]]
 # Tiles have no extra transparent padding, for edge-to-edge placement.
 if '/Tiles/' in e['output'] or '/Walls/' in e['output']:e['padding']=0
# The grass slope below the large staircase is a separate terrain piece.
add('Environments/arena-objects-px.png','Environments/Stairs','grass_slope_long',[1291,363,1429,484])
entries[-1]['polygon']=[[1291,477],[1419,363],[1429,363],[1429,484],[1291,484]]
# Remove neighboring fragments inside tightly packed prop crops.
for e in entries:
 n=Path(e['output']).stem
 if n=='rock_large':e['exclude']=[[145,828,169,849]]
 if n=='archway_pillar_left':e['exclude']=[[880,580,884,650]]
 if n=='enemy_spawn_seal':e['exclude']=[[1333,985,1341,1026]]
# Anchor wizard animations on the body, so spell effects do not shift the character.
for e in entries:
 if '/Characters/Wizard/' not in e['output']:continue
 n=Path(e['output']).stem
 rect=e['rect'];center=(rect[0]+rect[2])/2
 if '/attack/' in e['output']:center=[135,353,596,842][int(n[-2:])-1]
 elif '/cast_' in e['output']:
  center=next(v for k,v in [('arcane',127),('fire',379),('ice',607),('lightning',814),('heal',1087)] if 'cast_'+k in n)
 elif '/hurt/' in e['output']:center=139
 e['anchorX']=round(center)
 e['canvas']=[384,256]
final_crops={
 'gate_cap':[1059,530,1135,586],
 'iron_gate':[1064,595,1173,727],
 'banner_red':[1350,528,1439,714],
 'dead_tree':[476,746,634,895],
 'crystal_barrier_small':[639,289,770,475],
 'checkpoint_inactive':[11,895,91,1026],
 'summon_circle_active':[1164,919,1252,1016],
 'magic_bridge_active':[885,926,950,1010],
}
for e in entries:
 n=Path(e['output']).stem
 if n in final_crops:e['rect']=final_crops[n]
 if n=='stone_archway':e['exclude']=[[883,533,893,587]]
Path('Tools/Art/assets.json').write_text(json.dumps(entries,indent=2)+'\n')
print(len(entries), 'reviewed asset definitions')
