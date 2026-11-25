const ASPNET_JWT_SECRET = "DayLaMotCaiKeyCucKyDaiVaBaoMatChoDoAnMonHoc1234#!@#";

function beforeAuthCustom(
    ctx: nkruntime.Context,
    logger: nkruntime.Logger,
    nk: nkruntime.Nakama,
    data: nkruntime.AuthenticateCustomRequest
): nkruntime.AuthenticateCustomRequest {
    const token = data.account?.id?.trim();
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
        logger.error(
            `[AuthHook] Sai chu ky JWT! SecretLen=${ASPNET_JWT_SECRET.length} | Expected=${expectedHex} | Actual=${actualHex}`
        );
        throw new Error("Token giả mạo hoặc sai Secret Key!");
    }

    const payloadJson = nk.binaryToString(nk.base64Decode(base64UrlToStandard(payloadB64)));
    const payload = JSON.parse(payloadJson);

    const nowInSeconds = Math.floor(Date.now() / 1000);
    if (payload.exp && payload.exp < nowInSeconds) {
        logger.error("[AuthHook] JWT Token da het han!");
        throw new Error("Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại!");
    }

    const externalUserId =
        payload.nameid ||
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

    const rawName =
        payload.unique_name ||
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

function computeHmacSha256Hex(nk: nkruntime.Nakama, message: string, secret: string): string {
    const raw: unknown = nk.hmacSha256Hash(message, secret);
    if (typeof raw === "string") {
        return raw.trim().toLowerCase();
    }
    if (raw instanceof ArrayBuffer) {
        return arrayBufferToHex(raw);
    }
    if (ArrayBuffer.isView(raw)) {
        const view = raw as ArrayBufferView;
        const bytes = new Uint8Array(view.buffer, view.byteOffset, view.byteLength);
        return bytesToHex(bytes);
    }
    return String(raw).trim().toLowerCase();
}

function base64UrlToStandard(input: string): string {
    let b64 = input.replace(/-/g, "+").replace(/_/g, "/");
    const pad = b64.length % 4;
    if (pad === 2) b64 += "==";
    else if (pad === 3) b64 += "=";
    return b64;
}

function arrayBufferToHex(buffer: ArrayBuffer): string {
    return bytesToHex(new Uint8Array(buffer));
}

function bytesToHex(bytes: Uint8Array): string {
    let hex = "";
    for (let i = 0; i < bytes.length; i++) {
        const h = bytes[i].toString(16);
        hex += h.length === 1 ? "0" + h : h;
    }
    return hex.toLowerCase();
}