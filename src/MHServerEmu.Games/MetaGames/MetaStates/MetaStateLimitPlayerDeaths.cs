using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateLimitPlayerDeaths : MetaState
    {
	    private MetaStateLimitPlayerDeathsPrototype _proto;
		
        public MetaStateLimitPlayerDeaths(MetaGame metaGame, MetaStatePrototype prototype) : base(metaGame, prototype)
        {
            _proto = prototype as MetaStateLimitPlayerDeathsPrototype;
        }

        public override void OnRemovedPlayer(Player player)
        {
            // TODO _proto.FailOnAllPlayersDead
        }
    }
}