import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
S = ROOT / 'Assets' / 'Scripts'

def block(text, marker):
    pos = text.index(marker)
    start = text.index('{', pos)
    depth = 0
    for i in range(start, len(text)):
        if text[i] == '{': depth += 1
        elif text[i] == '}':
            depth -= 1
            if depth == 0: return text[start:i+1]
    raise ValueError(marker)

def strings(value):
    return re.findall(r'"((?:[^"\\]|\\.)*)"', value)

def rows(text, marker):
    return [strings(row) for row in re.findall(r'\{([^{}]*)\}', block(text, marker))]

def array(text, marker): return strings(block(text, marker))

cat = (S/'LegendaryCatalog.cs').read_text(encoding='utf-8')
gun = (S/'VaultLegendaryGunBehaviors.cs').read_text(encoding='utf-8')
armor = (S/'VaultLegendaryEffects.cs').read_text(encoding='utf-8')
boss = (S/'BossRelicFoundation.cs').read_text(encoding='utf-8')
skill = (S/'ActionSkillSystem.cs').read_text(encoding='utf-8')
design = (S/'DistinctEquipmentDesigns.cs').read_text(encoding='utf-8')

old_names=rows(cat,'static readonly string[][] GunsJa=')
new_names=rows(cat,'static readonly string[][] NewGunsJa=')
armor_names=rows(cat,'static readonly string[][] ArmorJa=')
gun_effects=array(gun,'string[] jp=')
triggers=['3射ごと（1秒）','銃弾命中時（2秒）','撃破時（2秒）','リロード時（3秒）','ダッシュ時（3秒）']
mechanisms=array(gun,'string[] mechanismJa=')
maker_effects=array(gun,'string[] makerJa=')
armor_effects=rows(armor,'string[,] jp=')
armor_triggers=['対象外','撃破時（4秒）','リロード時（4秒）','ダッシュ時（4秒）','戦闘中5秒ごと']
boss_names=rows(boss,'static readonly string[,] NamesJa=')
boss_effects=rows(boss,'string[,] effectsJa=')
title_section=skill[skill.index('public string ActionSkillNodeTitle'):skill.index('public string ActionSkillNodeDescription')]
skill_titles=rows(title_section,'string[,,] ja=')
assert len(old_names)==len(new_names)==len(armor_names)==6
assert len(gun_effects)==30 and len(mechanisms)==10 and len(maker_effects)==6
assert len(boss_names)==len(boss_effects)==3

lines=['# 現行レジェンダリ装備・アクションスキル一覧','',
       '出典: Dドライブの現行Unityコード。効果は日本語UI定義から転記。数値はレベルや装備による変動分を除く。',
       '防具の頭部枠は廃止されており、現行ドロップ対象外。','',
       '## 銃（90種）','', '|名称|銃種|固有効果|','|---|---|---|']
classes=['スナイパーライフル','ハンドガン','ショットガン','ヘビーマシンガン','ヘビーマシンガン']
for maker in range(6):
    for variant in range(15):
        name=old_names[maker][variant] if variant<5 else new_names[maker][variant-5]
        category=classes[variant%5]
        effect=(triggers[variant]+': '+gun_effects[maker*5+variant]) if variant<5 else mechanisms[variant-5]+maker_effects[maker]
        lines.append(f'|{name}|{category}|{effect}|')
lines += ['', '## 防具（実装30種、頭部6種は使用停止）','', '|名称|部位|固有効果|','|---|---|---|']
slots=['頭部（停止中）','胴体','腕・手','脚','足']
for maker in range(6):
    for variant in range(5):
        effect='頭装備は一時停止中' if variant==0 else armor_triggers[variant]+': '+armor_effects[maker][variant]
        lines.append(f'|{armor_names[maker][variant]}|{slots[variant]}|{effect}|')
lines += ['', '## ボス固有装備（3人×11種＝33種）','', '|ボス|名称|銃種・部位|固有効果|','|---|---|---|---|']
bosses=['レッドクロウ／レイヴン','コヨーテ／ヴェックス','ヴェイル']
boss_slots=['ハンドガン','ショットガン','アサルトライフル','スナイパーライフル','ヘビーマシンガン','胴体','腕・手','脚','足','モジュール','近接武器']
for b in range(3):
    for c in range(11):
        lines.append(f'|{bosses[b]}|{boss_names[b][c]}|{boss_slots[c]}|{boss_effects[b][c]} クールダウン4秒。|')
module_title_section=design[design.index('static void Name(EquipmentRecipe'):]
module_roots=rows(module_title_section,'string[][] ja=')
module_cores=array(module_title_section,'coreJa=')
module_types=['シールド','パルス','リロード','コンバージョン','ハイウェイ','ファーネス','アイソトープ']
signature=['シールド+3（上限30）','2.6m以内の見える敵最大3体へ威力連動ダメージ','回避クールダウン-0.45秒','3m以内の敵の次の攻撃を0.6秒以上遅延','0.2秒以上の無敵','3m以内の敵を1.2m押し返す','3m以内の敵を0.9m引き寄せる','前方4mの直線上の最大3体へ威力連動ダメージ','次の近接攻撃を即時使用可能','2.5m以内の最大2体へ威力連動ダメージと攻撃遅延']
assert len(module_roots)==6 and len(module_cores)==6 and len(module_types)==7
lines += ['', '## レジェンダリ・モジュール（現行レシピ）','',
          '効果はモジュール発動時。回復量・予備弾・回避短縮は基礎効果に上乗せ。数値が装備のpowerに依存する場合は「威力連動」と記載。','',
          '|名称|種別|レジェンダリ効果|','|---|---|---|']
for i in range(200):
    maker=i%6; local=i//6+5; family=local%7
    if family<4: continue
    body=local%6; core=(local//6)%6
    name=module_roots[maker][body]+'・'+module_types[family]+'・'+module_cores[core]
    charge=1+i%4
    parts=[]
    if maker in (1,3,5): parts.append(f'HP+{charge+body}')
    if maker in (0,4,5): parts.append(f'予備弾+{charge+core//2}')
    parts.append(f'回避クールダウン-{(i+1)/100:.2f}秒')
    mode=(core+body*2)%10
    lines.append(f'|{name}|{module_types[family]}|{", ".join(parts)}。追加: {signature[mode]}|')
manufacturer_section=design[design.index('public static string ManufacturerProductName'):design.index('static string Roman')]
melee_roots=rows(manufacturer_section,'string[][] jp=')
melee_suffix=rows(manufacturer_section,'string[][] suffixJa=')
melee_effects=[
 ['別の敵1体へ45%追撃','次撃まで-0.25秒','直線上の別の敵最大2体へ30%貫通','敵の攻撃を0.7秒遅らせ、次撃まで-0.15秒'],
 ['敵を1.4m押す','敵の攻撃を0.8秒遅らせる','別の敵最大2体へ40%追撃','敵を0.8m押し、攻撃を0.55秒遅らせる'],
 ['別の敵2体へ各32%連鎖','回避クールダウン-0.6秒','直線上の別の敵最大3体へ28%貫通','周囲の敵を0.9m押し、回避クールダウン-0.25秒'],
 ['HP+3','敵の攻撃を1.1秒遅らせる','周囲の敵を対象へ0.8m引く','別の敵最大2体へ22%追撃、HP+1'],
 ['別の敵最大3体へ38%範囲攻撃','周囲の敵を1m押す','直線上の別の敵最大3体へ42%貫通','対象を1m押し、別の敵最大2体へ25%追撃'],
 ['シールド+4（上限30）','0.25秒以上の無敵','シールド+2、周囲の敵を0.8m押す','シールド+2、0.15秒以上の無敵、敵の攻撃を0.75秒遅らせる']]
lines += ['', '## レジェンダリ・近接武器（5種×10意匠＝50種）','',
          'クールダウン終了後の最初の近接命中時に発動。クールダウン3秒。','',
          '|名称|武器種|レジェンダリ効果|','|---|---|---|']
melee_types=['フィールドナイフ','シヴ','ブッチャーナイフ','マチェット','ハチェット']
for family in range(5):
    for variant in range(10):
        i=family*10+variant; maker=(i+variant)%6; ordinal=i//6+21; root=melee_roots[maker][ordinal%12]; edition=ordinal//12; suffix=melee_suffix[maker][edition%8]
        name=root+('・'+suffix if suffix else '')
        if edition>=8: name+=' '+str(edition//8+1)
        mode=(variant+2*family)%4
        lines.append(f'|{name}|{melee_types[family]}|{melee_effects[maker][mode]}|')
lines += ['', '## アクションスキルツリー（3種類×3枝×5段＝45ノード）','',
          '各枝の1～4段は最大6ランク、5段は最大1ランク。前段への投資で次段が解放される。','',
          '|スキル|枝|1段|2段|3段|4段|5段|','|---|---|---|---|---|---|---|']
skill_names=['メカニック','ボマー','メディック']
branches=[['威力','機動','制圧'],['弾数','範囲','威力'],['回復','範囲','支援']]
for k in range(3):
    for b in range(3):
        titles=skill_titles[k*3+b]
        assert len(titles)==5
        lines.append('|'+skill_names[k]+'|'+branches[k][b]+'|'+ '|'.join(titles)+'|')
lines += ['', '### 各枝の基本効果（1～4段、ランクごと）','',
          '|スキル|枝|効果|','|---|---|---|',
          '|メカニック|威力|タレット威力が武器威力の約2.17%分上昇|',
          '|メカニック|機動|持続時間+0.25秒|',
          '|メカニック|制圧|射程+0.25m、射撃間隔-0.01秒|',
          '|ボマー|弾数|枝の合計6ランクごとにグレネード+1|',
          '|ボマー|範囲|爆発範囲+約0.06m|',
          '|ボマー|威力|爆発威力が武器威力の約16.7%分上昇|',
          '|メディック|回復|最大HPの毎秒0.1%分回復量上昇|',
          '|メディック|範囲|回復範囲+0.075m|',
          '|メディック|支援|持続時間+0.25秒|',
          '', '### 5段目の固有強化','',
          '|スキル|枝|効果|','|---|---|---|',
          '|メカニック|威力|タレット威力が武器威力の55%分さらに上昇|',
          '|メカニック|機動|タレットがプレイヤーを追尾、持続+5秒|',
          '|メカニック|制圧|2体目の敵にも射撃|',
          '|ボマー|弾数|さらに3個投擲|',
          '|ボマー|範囲|爆発範囲+1.25m|',
          '|ボマー|威力|爆発威力が武器威力の300%分上昇、追加打撃|',
          '|メディック|回復|最大HPの毎秒2.5%分回復量上昇|',
          '|メディック|範囲|回復範囲+1.6m|',
          '|メディック|支援|回復エリアがプレイヤーを追尾、持続+5秒|',
          '', '### 追加パッシブ','',
          '|スキル|枝・段|効果|','|---|---|---|',
          '|メカニック|威力・2段|アサルトライフルとヘビーマシンガンの威力+1.2%/ランク|',
          '|メカニック|制圧・2段|リロード速度+1.2%/ランク|',
          '|ボマー|弾数・2段|ショットガン威力+1.4%/ランク|',
          '|ボマー|威力・2段|会心率+0.3%/ランク|',
          '|メディック|回復・2段|最大HP+3/ランク|',
          '|メディック|範囲・2段|命中精度+0.6%/ランク|',
          '|メディック|支援・2段|被ダメージ軽減+1%/ランク|',
          '|メカニック|機動・4段|アサルトライフルとヘビーマシンガンの連射速度+1%/ランク|',
          '|ボマー|範囲・4段|ショットガン連射速度+1%/ランク|',
          '|メディック|範囲・4段|HP自動回復+0.12/秒/ランク|']
out=ROOT/'Development'/'LegendaryAndSkillsCurrent.md'
out.write_text('\n'.join(lines)+'\n',encoding='utf-8')
print(out, 'rows', len(lines))
