"use strict";
const ASPNET_JWT_SECRET = "DayLaMotCaiKeyCucKyDaiVaBaoMatChoDoAnMonHoc1234#!@#";
function beforeAuthCustom(ctx, logger, nk, data) {
    var _a, _b;
    const token = (_b = (_a = data.account) === null || _a === void 0 ? void 0 : _a.id) === null || _b === void 0 ? void 0 : _b.trim();
    if (!data.account || !token) {
        logger.error("[AuthHook] Thieu JWT Token xac thuc!");
        throw new Error("Thiếu JWT Token xác thực!");
    }
    const parts = token.split(".");
    if (parts.length !== 3) {
        logger.error("[AuthHook] Dinh dang JWT Token khong hop le!");
        throw new Error("Định dạng JWT Token không hợp lệ!");
    }
    const [headerB64, payloadB64, signatureB64Url] = parts;
    const signingInput = `${headerB64}.${payloadB64}`;
    const expectedHex = computeHmacSha256Hex(nk, signingInput, ASPNET_JWT_SECRET);
    const sigStdBase64 = base64UrlToStandard(signatureB64Url);
    const actualBuffer = nk.base64Decode(sigStdBase64);
    const actualHex = arrayBufferToHex(actualBuffer);
    if (expectedHex !== actualHex) {
        logger.error(`[AuthHook] Sai chu ky JWT! SecretLen=${ASPNET_JWT_SECRET.length} | Expected=${expectedHex} | Actual=${actualHex}`);
        throw new Error("Token giả mạo hoặc sai Secret Key!");
    }
    const payloadJson = nk.binaryToString(nk.base64Decode(base64UrlToStandard(payloadB64)));
    const payload = JSON.parse(payloadJson);
    const nowInSeconds = Math.floor(Date.now() / 1000);
    if (payload.exp && payload.exp < nowInSeconds) {
        logger.error("[AuthHook] JWT Token da het han!");
        throw new Error("Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại!");
    }
    const externalUserId = payload.nameid ||
        payload.sub ||
        payload.id ||
        payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"];
    if (!externalUserId) {
        logger.error("[AuthHook] Khong tim thay UserId trong JWT Payload: " + payloadJson);
        throw new Error("JWT không chứa thông tin định danh người dùng!");
    }
    let customId = String(externalUserId).trim();
    if (customId.length < 6) {
        customId = "aspnet_" + customId;
    }
    const rawName = payload.unique_name ||
        payload.email ||
        payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"] ||
        payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"];
    data.account.id = customId;
    if (rawName) {
        data.username = String(rawName).split("@")[0];
    }
    logger.info(`[AuthHook] Xac thuc JWT thanh cong -> CustomId: ${data.account.id}`);
    return data;
}
function computeHmacSha256Hex(nk, message, secret) {
    const raw = nk.hmacSha256Hash(message, secret);
    if (typeof raw === "string") {
        return raw.trim().toLowerCase();
    }
    if (raw instanceof ArrayBuffer) {
        return arrayBufferToHex(raw);
    }
    if (ArrayBuffer.isView(raw)) {
        const view = raw;
        const bytes = new Uint8Array(view.buffer, view.byteOffset, view.byteLength);
        return bytesToHex(bytes);
    }
    return String(raw).trim().toLowerCase();
}
function base64UrlToStandard(input) {
    let b64 = input.replace(/-/g, "+").replace(/_/g, "/");
    const pad = b64.length % 4;
    if (pad === 2)
        b64 += "==";
    else if (pad === 3)
        b64 += "=";
    return b64;
}
function arrayBufferToHex(buffer) {
    return bytesToHex(new Uint8Array(buffer));
}
function bytesToHex(bytes) {
    let hex = "";
    for (let i = 0; i < bytes.length; i++) {
        const h = bytes[i].toString(16);
        hex += h.length === 1 ? "0" + h : h;
    }
    return hex.toLowerCase();
}
function rpcGetLoadout(ctx, _logger, nk, _payload) {
    if (!ctx.userId)
        throw new Error("Chưa xác thực người dùng!");
    const records = nk.storageRead([
        { collection: "player_data", key: "loadout", userId: ctx.userId }
    ]);
    if (records.length > 0) {
        return JSON.stringify(records[0].value);
    }
    const defaultLoadout = {
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
function rpcFindOrCreateMatch(_ctx, _logger, nk, _payload) {
    const matches = nk.matchList(10, true, "", null, 15, "");
    if (matches.length > 0) {
        return JSON.stringify({ matchId: matches[0].matchId });
    }
    const newMatchId = nk.matchCreate("fps_deathmatch", {});
    return JSON.stringify({ matchId: newMatchId });
}
function InitModule(_ctx, logger, _nk, initializer) {
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
function matchInit(_ctx, _logger, _nk, _params) {
    return {
        state: { players: {}, emptyTicks: 0 },
        tickRate: 20,
        label: JSON.stringify({ mode: "deathmatch", maxPlayers: 16 })
    };
}
function matchJoinAttempt(_ctx, _logger, _nk, _dispatcher, _tick, state, _presence, _metadata) {
    if (Object.keys(state.players).length >= 16) {
        return { state, accept: false, rejectMessage: "Phòng đã đầy 16/16 người chơi!" };
    }
    return { state, accept: true };
}
function matchJoin(_ctx, _logger, _nk, dispatcher, _tick, state, presences) {
    for (const p of presences) {
        state.players[p.userId] = {
            userId: p.userId,
            username: p.username,
            hp: 100,
            kills: 0,
            deaths: 0
        };
    }
    dispatcher.broadcastMessage(4 /* FpsOpCode.STATE_SYNC */, JSON.stringify(state.players));
    return { state };
}
function matchLeave(_ctx, _logger, _nk, dispatcher, _tick, state, presences) {
    for (const p of presences) {
        delete state.players[p.userId];
    }
    dispatcher.broadcastMessage(4 /* FpsOpCode.STATE_SYNC */, JSON.stringify(state.players));
    return { state };
}
function matchLoop(_ctx, _logger, nk, dispatcher, _tick, state, messages) {
    if (Object.keys(state.players).length === 0) {
        state.emptyTicks++;
        if (state.emptyTicks > 600)
            return null;
        return { state };
    }
    state.emptyTicks = 0;
    for (const msg of messages) {
        if (msg.opCode === 1 /* FpsOpCode.PLAYER_MOVE */ || msg.opCode === 2 /* FpsOpCode.PLAYER_SHOOT */) {
            dispatcher.broadcastMessage(msg.opCode, msg.data, null, msg.sender);
        }
        else if (msg.opCode === 3 /* FpsOpCode.PLAYER_HIT */) {
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
                dispatcher.broadcastMessage(4 /* FpsOpCode.STATE_SYNC */, JSON.stringify(state.players));
            }
        }
    }
    return { state };
}
function matchTerminate(_ctx, _logger, _nk, _dispatcher, _tick, state, _graceSeconds) {
    return { state };
}
function matchSignal(_ctx, _logger, _nk, _dispatcher, _tick, state, data) {
    return { state, data };
}
