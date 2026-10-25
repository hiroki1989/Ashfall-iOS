using UnityEngine;

namespace NeonVault
{
    // Resources asset is intentionally shared by desktop and iOS builds.
    [CreateAssetMenu(fileName = "VaultBalanceProfile", menuName = "ASHFALL/戦闘・ドロップ設定")]
    public sealed class VaultBalanceProfile : ScriptableObject
    {
        static VaultBalanceProfile current;
        public static VaultBalanceProfile Current => current != null ? current : current = Resources.Load<VaultBalanceProfile>("VaultBalanceProfile");

        [Header("敵・プレイヤー")]
        [InspectorName("敵HP倍率")] [Min(.1f)] public float enemyHpMultiplier = 1f;
        [InspectorName("プレイヤー初期HP倍率")] [Min(.1f)] public float startingHpMultiplier = 1f;
        [InspectorName("フロアの敵出現数倍率")] [Min(.1f)] public float enemyCountMultiplier = 1f;
        [InspectorName("敵の攻撃力倍率")] [Min(.1f)] public float enemyDamageMultiplier = 1f;

        [Header("装備・回復パックのドロップ率（0～1）")]
        [InspectorName("銃のドロップ率・通常敵")] [Range(0,1)] public float weaponDropChanceNormal = .08f;
        [InspectorName("銃のドロップ率・ボス")] [Range(0,1)] public float weaponDropChanceBoss = .333333f;
        [InspectorName("防具のドロップ率・通常敵")] [Range(0,1)] public float armorDropChance = .087f;
        [InspectorName("防具のドロップ率・ボス")] [Range(0,1)] public float armorDropChanceBoss = .333333f;
        [InspectorName("モジュールのドロップ率・通常敵")] [Range(0,1)] public float moduleDropChance = .04f;
        [InspectorName("モジュールのドロップ率・ボス")] [Range(0,1)] public float moduleDropChanceBoss = .183f;
        [InspectorName("近接武器のドロップ率・通常敵")] [Range(0,1)] public float meleeDropChance = .06f;
        [InspectorName("近接武器のドロップ率・ボス")] [Range(0,1)] public float meleeDropChanceBoss = .25f;
        [InspectorName("レジェンダリー出現率倍率")] [Min(0)] public float legendaryDropMultiplier = 1f;

        [Header("弾薬・スクラップ")]
        [InspectorName("弾薬のドロップ率・通常敵")] [Range(0,1)] public float ammoDropChance = .64f;
        [InspectorName("弾薬のドロップ率・ボス")] [Range(0,1)] public float ammoDropChanceBoss = 1f;
        [InspectorName("ドロップ弾薬数倍率")] [Min(.1f)] public float ammoQuantityMultiplier = 1f;
        [InspectorName("各フロアの弾薬箱数")] [Range(0,30)] public int ammoCratesPerFloor = 10;
        [InspectorName("獲得スクラップ倍率")] [Min(0)] public float scrapGainMultiplier = 1f;
        [InspectorName("スクラップのドロップ率・通常敵")] [Range(0,1)] public float scrapDropChance = .45f;
        [InspectorName("スクラップのドロップ率・ボス")] [Range(0,1)] public float scrapDropChanceBoss = .8f;

        [Header("装備屋のレア度・相対出現比率")]
        [InspectorName("コモンの比率")] [Min(0)] public float shopCommonWeight = 0f;
        [InspectorName("アンコモンの比率")] [Min(0)] public float shopUncommonWeight = 3f;
        [InspectorName("レアの比率")] [Min(0)] public float shopRareWeight = 1f;
        [InspectorName("エピックの比率")] [Min(0)] public float shopEpicWeight = 0f;
        [InspectorName("レジェンダリーの比率")] [Min(0)] public float shopLegendaryWeight = 0f;

        [Header("オートエイム")]
        [InspectorName("最低命中精度時の角度（度）")] [Range(0,30)] public float autoAimMinAngle = 3f;
        [InspectorName("最高命中精度時の角度（度）")] [Range(0,45)] public float autoAimMaxAngle = 30f;
        [InspectorName("最低命中精度時の射程（m）")] [Min(1)] public float autoAimMinDistance = 5f;
        [Tooltip("最大命中精度では画面内の敵を射程無制限で対象にします。")]
        [InspectorName("最大未満での上限射程（m）")] [Min(1)] public float autoAimMaxDistance = 160f;

        public Rarity RollShopRarity(System.Random random)
        {
            float[] weights = { shopCommonWeight, shopUncommonWeight, shopRareWeight, shopEpicWeight, shopLegendaryWeight };
            float total = 0; foreach (float weight in weights) total += Mathf.Max(0, weight);
            if (total <= 0) return Rarity.Uncommon;
            double roll = random.NextDouble() * total;
            for (int i = 0; i < weights.Length; i++) { roll -= Mathf.Max(0, weights[i]); if (roll < 0) return (Rarity)i; }
            return Rarity.Legendary;
        }
    }
}
