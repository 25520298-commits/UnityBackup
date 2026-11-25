interface PlayerLoadout {
    primaryWeapon: string;
    secondaryWeapon: string;
    unlockedWeapons: string[];
}

function rpcGetLoadout(
    ctx: nkruntime.Context,
    _logger: nkruntime.Logger,
    nk: nkruntime.Nakama,
    _payload: string
): string {
    if (!ctx.userId) throw new Error("Chưa xác thực người dùng!");

    const records = nk.storageRead([
        { collection: "player_data", key: "loadout", userId: ctx.userId }
    ]);

    if (records.length > 0) {
        return JSON.stringify(records[0].value);
    }

    const defaultLoadout: PlayerLoadout = {
        primaryWeapon: "AK47",
        secondaryWeapon: "Glock17",
        unlockedWeapons: ["AK47", "M4A1", "Glock17"]
    };

    nk.storageWrite([
        {
            collection: "player_data",
            key: "loadout",
            userId: ctx.userId,
            value: defaultLoadout,
            permissionRead: 1,
            permissionWrite: 0
        }
    ]);

    return JSON.stringify(defaultLoadout);
}

function rpcFindOrCreateMatch(
    _ctx: nkruntime.Context,
    _logger: nkruntime.Logger,
    nk: nkruntime.Nakama,
    _payload: string
): string {
    const matches = nk.matchList(10, true, "", null, 15, "");
    if (matches.length > 0) {
        return JSON.stringify({ matchId: matches[0].matchId });
    }

    const newMatchId = nk.matchCreate("fps_deathmatch", {});
    return JSON.stringify({ matchId: newMatchId });
}