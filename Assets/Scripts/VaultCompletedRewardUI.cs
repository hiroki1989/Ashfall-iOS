using UnityEngine;
namespace NeonVault {
 public partial class VaultGame {
  Vector2 completedRewardScroll;
  void DrawCompletedGearReward(Rect area){
   object item=(object)completedRewardGun??completedRewardArmor;
   if(item==null)return;
   DrawBorderlandsWorldLootCard(area,item,T("CONTRACT REWARD","任務報酬"));
  }

 }
}
