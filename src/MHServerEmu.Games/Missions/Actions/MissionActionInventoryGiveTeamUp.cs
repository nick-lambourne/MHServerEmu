using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionInventoryGiveTeamUp : MissionAction
    {
        private MissionActionInventoryGiveTeamUpPrototype _proto;
        public MissionActionInventoryGiveTeamUp(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
            // SHIELDTeamUpController
            _proto = prototype as MissionActionInventoryGiveTeamUpPrototype;
        }

        public override void Run()
        {
            var teamUpRef = _proto.TeamUpPrototype;
            if (teamUpRef == PrototypeId.Invalid) return;
            foreach (Player player in Mission.GetParticipants())
                player.UnlockTeamUpAgent(teamUpRef);
        }
    }
}