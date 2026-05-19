namespace Dalamud.Game.ClientState.Objects.Types
{
    public interface IBattleChara
    {
        ulong GameObjectId { get; }
    }
}

namespace RotationSolver.Basic.Actions
{
    using Dalamud.Game.ClientState.Objects.Types;

    public enum TargetType : byte
    {
        Big,
        Nearest,
    }

    public interface IAction
    {
    }

    public interface IBaseAction : IAction
    {
        ActionSetting Setting { get; set; }

        bool CanUse(
            out IAction act,
            bool skipStatusProvideCheck = false,
            bool skipStatusNeed = false,
            bool skipTargetStatusNeedCheck = false,
            bool skipComboCheck = false,
            bool skipCastingCheck = false,
            bool usedUp = false,
            bool skipAoeCheck = false,
            bool skipTTKCheck = false,
            byte gcdCountForAbility = 0,
            bool checkActionManager = false,
            TargetType targetOverride = default);
    }

    public class ActionSetting
    {
        public Func<IBattleChara, bool> CanTarget { get; set; } = _ => true;
    }
}
