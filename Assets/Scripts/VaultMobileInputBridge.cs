using UnityEngine;
namespace NeonVault
{
    /// <summary>Single input boundary: mobile controls never fall through to emulated mouse input.</summary>
    public static class VaultMobileInputBridge
    {
        public static bool Enabled => MobileTouchControls.Enabled;
        public static Vector2 Movement => MobileTouchControls.Movement;
        public static Vector2 Aim => MobileTouchControls.Aim;
        public static bool Firing => Enabled ? MobileTouchControls.Firing : Input.GetMouseButton(0);
        public static bool MeleePressed => Enabled ? MobileTouchControls.Consume(MobileAction.Melee) : Input.GetMouseButtonDown(VaultGame.MeleeAttackMouseButton);
        public static bool Pressed(MobileAction action,KeyCode legacyKey)
        { return Enabled ? MobileTouchControls.Consume(action) : Input.GetKeyDown(legacyKey); }
        public static Vector3 ToWorldMovement(Vector2 screenInput)
        {
            var value=Vector2.ClampMagnitude(screenInput,1);
            return new Vector3(value.y,0,-value.x);
        }
    }
    public partial class VaultGame
    {
        public Vector3 ReadPlayerMovement()
        {
            return VaultMobileInputBridge.Enabled ? VaultMobileInputBridge.ToWorldMovement(VaultMobileInputBridge.Movement)
                : ScreenMovement(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical"));
        }
        public void ApplyMobileAim(Vector2 screenDirection)
        {
            // Re-anchor held aim every frame as the player moves; releasing the stick retains direction.
            var direction=VaultMobileInputBridge.ToWorldMovement(screenDirection);
            if(direction.sqrMagnitude>.0001f)aim=direction.normalized;
            aimUsesCursor=false;
            if(player!=null&&PlayerWeapon!=null)AimAt(PlayerWeapon.MuzzlePosition+aim*30);
        }
    }
}
