export async function request(method, url, payload = null) {
    const deviceId = window.uauth?.deviceId;

    if (!deviceId) {
        throw new Error("UltimateAuth device is not initialized.");
    }

    const headers = {
        "X-UDID": deviceId,
        "X-Requested-With": "UAuth"
    };

    const options = {
        method,
        credentials: "same-origin",
        headers
    };

    if (payload !== null) {
        headers["Content-Type"] = "application/json";
        options.body = JSON.stringify(payload);
    }

    const response = await fetch(url, options);

    const body = response.status === 204
        ? null
        : await response.json().catch(() => null);

    return {
        ok: response.ok,
        status: response.status,
        body
    };
}
