# 現行の防具共通化
最新ユーザー指示により、個別3D生成の計画は保留。頭部を除く398防具は承認済Wardenの部位別形状/骨/UVを共用する。胴に袖を含み、手はカフ以降。腰1.04m、足境界.2882m。素体の装備下非表示も維持。

製品別の材質色はArt/SharedArmorLUTへ。元の高精細atlasを1枚共有し、32^3色変換を1024x32PNGに格納する。画像座標は上原点なのでshader側green軸を反転。RGB出力を作成するときは承認baseのUVや模様を変更しない。

原画の個別模様や構造を精密に再現する方式ではなく、原画から素材色を移し、承認済みの布目/傷/縫目を維持する方式。頭部は変更せず、初期装備の元材質も維持する。再生成はTools/SharedArmor/make_lut.py。全サイズatlas候補は本番Resourcesへ置かない。モジュール/武器は対象外。

最終検証: shared-armor-lut-build2.log、shared-armor-final-runtime2.log、shared-armor-final/result.txt、shared-armor-motion-runtime.log。実機iOSは未確認。
