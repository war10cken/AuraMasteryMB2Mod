using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace AuraMastery.Core
{
    public static class AuraSkill
    {
        public const string StringId = "AuraMastery";

        public static SkillObject Skill
        {
            get
            {
                try
                {
                    return MBObjectManager.Instance
                        ?.GetObjectTypeList<SkillObject>()
                        ?.FirstOrDefault(s => s.StringId == StringId);
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}