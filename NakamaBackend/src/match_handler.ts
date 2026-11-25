interface FpsPlayerState {
    userId: string;
    username: string;
    hp: number;
    kills: number;
    deaths: number;
}

interface FpsMatchState {
    players: { [userId: string]: FpsPlayerState };
    emptyTicks: number;
}

const enum FpsOpCode {
    PLAYER_MOVE = 1,
    PLAYER_SHOOT = 2,
    PLAYER_HIT = 3,
    STATE_SYNC = 4
}

function matchInit(
    _ctx: nkruntime.Context,
    _logger: nkruntime.Logger,
    _nk: nkruntime.Nakama,
    _params: { [key: string]: string }
): { state: FpsMatchState; tickRate: number; label: string } {
    return {
        state: { players: {}, emptyTicks: 0 },
        tickRate: 20,
        label: JSON.stringify({ mode: "deathmatch", maxPlayers: 16 })
    };
}

function matchJoinAttempt(
    _ctx: nkruntime.Context,
    _logger: nkruntime.Logger,
    _nk: nkruntime.Nakama,
    _dispatcher: nkruntime.MatchDispatcher,
    _tick: number,
    state: FpsMatchState,
    _presence: nkruntime.Presence,
    _metadata: { [key: string]: any }
): { state: FpsMatchState; accept: boolean; rejectMessage?: string } | null {
    if (Object.keys(state.players).length >= 16) {
        return { state, accept: false, rejectMessage: "Phòng đã đầy 16/16 người chơi!" };
    }
    return { state, accept: true };
}

function matchJoin(
    _ctx: nkruntime.Context,
    _logger: nkruntime.Logger,
    _nk: nkruntime.Nakama,
    dispatcher: nkruntime.MatchDispatcher,
    _tick: number,
    state: FpsMatchState,
    presences: nkruntime.Presence[]
): { state: FpsMatchState } | null {
    for (const p of presences) {
        state.players[p.userId] = {
            userId: p.userId,
            username: p.username,
            hp: 100,
            kills: 0,
            deaths: 0
        };
    }
    dispatcher.broadcastMessage(FpsOpCode.STATE_SYNC, JSON.stringify(state.players));
    return { state };
}

function matchLeave(
    _ctx: nkruntime.Context,
    _logger: nkruntime.Logger,
    _nk: nkruntime.Nakama,
    dispatcher: nkruntime.MatchDispatcher,
    _tick: number,
    state: FpsMatchState,
    presences: nkruntime.Presence[]
): { state: FpsMatchState } | null {
    for (const p of presences) {
        delete state.players[p.userId];
    }
    dispatcher.broadcastMessage(FpsOpCode.STATE_SYNC, JSON.stringify(state.players));
    return { state };
}

function matchLoop(
    _ctx: nkruntime.Context,
    _logger: nkruntime.Logger,
    nk: nkruntime.Nakama,
    dispatcher: nkruntime.MatchDispatcher,
    _tick: number,
    state: FpsMatchState,
    messages: nkruntime.MatchMessage[]
): { state: FpsMatchState } | null {
    if (Object.keys(state.players).length === 0) {
        state.emptyTicks++;
        if (state.emptyTicks > 600) return null;
        return { state };
    }
    state.emptyTicks = 0;

    for (const msg of messages) {
        if (msg.opCode === FpsOpCode.PLAYER_MOVE || msg.opCode === FpsOpCode.PLAYER_SHOOT) {
            dispatcher.broadcastMessage(msg.opCode, msg.data, null, msg.sender);
        } else if (msg.opCode === FpsOpCode.PLAYER_HIT) {
            const data = JSON.parse(nk.binaryToString(msg.data));
            const target = state.players[data.targetUserId];
            if (target && target.hp > 0) {
                target.hp -= data.isHeadshot ? 100 : 25;
                if (target.hp <= 0) {
                    target.hp = 100;
                    target.deaths += 1;
                    if (state.players[msg.sender.userId]) {
                        state.players[msg.sender.userId].kills += 1;
                    }
                }
                dispatcher.broadcastMessage(FpsOpCode.STATE_SYNC, JSON.stringify(state.players));
            }
        }
    }

    return { state };
}

function matchTerminate(
    _ctx: nkruntime.Context,
    _logger: nkruntime.Logger,
    _nk: nkruntime.Nakama,
    _dispatcher: nkruntime.MatchDispatcher,
    _tick: number,
    state: FpsMatchState,
    _graceSeconds: number
): { state: FpsMatchState } | null {
    return { state };
}

function matchSignal(
    _ctx: nkruntime.Context,
    _logger: nkruntime.Logger,
    _nk: nkruntime.Nakama,
    _dispatcher: nkruntime.MatchDispatcher,
    _tick: number,
    state: FpsMatchState,
    data: string
): { state: FpsMatchState; data?: string } | null {
    return { state, data };
}