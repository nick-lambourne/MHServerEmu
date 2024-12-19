﻿using MHServerEmu.Commands.Attributes;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("boost", "Provides commands for boost.", AccountUserLevel.Admin)]
    public class BoostCommands : CommandGroup
    {
        [Command("damage", "Increase Damage of current avatar.\nUsage: boost damage [1-10000]")]
        public string Damage(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            Avatar avatar = playerConnection.Player.CurrentAvatar;

            if ((@params.Length > 0 && int.TryParse(@params[0], out int damage)) == false)
                damage = 1000;

            damage = Math.Clamp(damage, 1, 10000);
            avatar.Properties[PropertyEnum.DamagePctBonus] = (float)damage;

            return $"Damage x{damage}";
        }

        [Command("vsboss", "Increase Damage vs Bosses of current avatar.\nUsage: boost vsboss [1-10000]")]
        public string VsBoss(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            Avatar avatar = playerConnection.Player.CurrentAvatar;

            if ((@params.Length > 0 && int.TryParse(@params[0], out int vsboss)) == false)
                vsboss = 1000;

            vsboss = Math.Clamp(vsboss, 1, 10000);
            avatar.Properties[PropertyEnum.DamagePctBonusVsBosses] = (float)vsboss;

            return $"Damage vs Bosses x{vsboss}";
        }   

        [Command("xp", "Set Bonus XP %\nUsage: boost xp [0-100]")]
        public string xp(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            Avatar avatar = playerConnection.Player.CurrentAvatar;

            if ((@params.Length > 0 && int.TryParse(@params[0], out int bonusXp)) == false)
                bonusXp = 1;

            bonusXp = Math.Clamp(bonusXp, 0, 100);
            avatar.Properties[PropertyEnum.LootBonusXPPct] = (float)bonusXp;
            return $"Loot Bonus XP%: x{bonusXp}";
        }
        
        [Command("xpstack", "Set XPBonusStackCount \nUsage: boost xp [1-10000]")]
        public string xpstack(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            Avatar avatar = playerConnection.Player.CurrentAvatar;

            if ((@params.Length > 0 && int.TryParse(@params[0], out int bonusXp)) == false)
                bonusXp = 1;

            bonusXp = Math.Clamp(bonusXp, 1, 10000);
            avatar.Properties[PropertyEnum.XPBonusStackCount] = (float)bonusXp;
            return $"XPBonusStackCount: x{bonusXp}";
        }
    }
}
