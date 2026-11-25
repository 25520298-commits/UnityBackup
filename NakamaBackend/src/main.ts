function InitModule(
    _ctx: nkruntime.Context,
    logger: nkruntime.Logger,
    _nk: nkruntime.Nakama,
    initializer: nkruntime.Initializer
): void {
    initializer.registerBeforeAuthenticateCustom(beforeAuthCustom);
    initializer.registerRpc("get_loadout", rpcGetLoadout);
    initializer.registerRpc("find_or_create_match", rpcFindOrCreateMatch);
    initializer.registerMatch("fps_deathmatch", {
        matchInit,
        matchJoinAttempt,
        matchJoin,
        matchLeave,
        matchLoop,
        matchTerminate,
        matchSignal
    });

    logger.info("=== FPS Custom Backend Runtime Loaded Successfully ===");
}