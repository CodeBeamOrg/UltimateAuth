window.uauth = window.uauth || {};

window.uauth.storage = {
    set: function (scope, key, value) {
        const storage = scope === "local"
            ? window.localStorage
            : window.sessionStorage;

        storage.setItem(key, value);
    },

    get: function (scope, key) {
        const storage = scope === "local"
            ? window.localStorage
            : window.sessionStorage;

        return storage.getItem(key);
    },

    remove: function (scope, key) {
        const storage = scope === "local"
            ? window.localStorage
            : window.sessionStorage;

        storage.removeItem(key);
    },

    exists: function (scope, key) {
        const storage = scope === "local"
            ? window.localStorage
            : window.sessionStorage;

        return storage.getItem(key) !== null;
    }
};

window.uauth.submitForm = function (form) {
    if (!form)
        return;

    if (!window.uauth.deviceId) {
        throw new Error("UAuth deviceId is not initialized.");
    }

    let udid = form.querySelector("input[name='__uauth_device']");
    if (!udid) {
        udid = document.createElement("input");
        udid.type = "hidden";
        udid.name = "__uauth_device";
        form.appendChild(udid);
    }
    udid.value = window.uauth.deviceId;

    form.submit();
};

window.uauth.tryAndCommit = async function (options) {
    const { tryUrl, commitUrl, data, clientProfile } = options;

    const tryResponse = await window.uauth.postJson({
        url: tryUrl,
        payload: data,
        clientProfile: clientProfile
    });

    let result = tryResponse?.body;

    if (!result) {
        result = {};
    }

    const normalized = {
        success: result.success ?? false,
        reason: result.reason ?? null,
        remainingAttempts: result.remainingAttempts ?? null,
        lockoutUntilUtc: result.lockoutUntilUtc ?? null,
        requiresMfa: result.requiresMfa ?? false,
        retryWithNewPkce: result.retryWithNewPkce ?? false
    };

    if (normalized.success) {
        const form = document.createElement("form");
        form.method = "POST";
        form.action = commitUrl;

        for (const key in data) {
            const input = document.createElement("input");
            input.type = "hidden";
            input.name = key;
            input.value = data[key] ?? "";
            form.appendChild(input);
        }

        const cp = document.createElement("input");
        cp.type = "hidden";
        cp.name = "__uauth_client_profile";
        cp.value = clientProfile ?? "";
        form.appendChild(cp);

        const udid = document.createElement("input");
        udid.type = "hidden";
        udid.name = "__uauth_device";
        udid.value = window.uauth.deviceId;
        form.appendChild(udid);

        document.body.appendChild(form);
        form.submit();
    }

    return normalized;
};

window.uauth.post = async function (options) {
    const {
        url,
        mode,
        data,
        clientProfile
    } = options;

    if (mode === "navigate") {
        const form = document.createElement("form");
        form.method = "POST";
        form.action = url;

        const cp = document.createElement("input");
        cp.type = "hidden";
        cp.name = "__uauth_client_profile";
        cp.value = clientProfile ?? "";
        form.appendChild(cp);

        const udid = document.createElement("input");
        udid.type = "hidden";
        udid.name = "__uauth_device";
        udid.value = window.uauth.deviceId;
        form.appendChild(udid);

        if (data) {
            for (const key in data) {
                const input = document.createElement("input");
                input.type = "hidden";
                input.name = key;
                input.value = data[key];
                form.appendChild(input);
            }
        }

        document.body.appendChild(form);
        form.submit();
        return null;
    }

    let body = null;
    if (!window.uauth.deviceId) {
        throw new Error("UAuth deviceId is not initialized.");
    }
    const headers = {
        "X-UDID": window.uauth.deviceId,
        "X-UAuth-ClientProfile": clientProfile,
        "X-Requested-With": "UAuth"
    };

    if (data) {
        body = new URLSearchParams();
        for (const key in data) {
            body.append(key, data[key]);
        }

        headers["Content-Type"] = "application/x-www-form-urlencoded";
    }

    const response = await fetch(url, {
        method: "POST",
        credentials: "include",
        headers: headers,
        body: body
    });

    let responseBody = null;
    try {
        responseBody = await response.json();
    } catch {
        responseBody = null;
    }

    return {
        ok: response.ok,
        status: response.status,
        refreshOutcome: response.headers.get("X-UAuth-Refresh"),
        body: responseBody
    };
};

window.uauth.postJson = async function (options) {
    const {
        url,
        payload,
        clientProfile
    } = options;

    if (!window.uauth.deviceId) {
        throw new Error("UAuth deviceId is not initialized.");
    }

    const headers = {
        "Content-Type": "application/json",
        "X-UDID": window.uauth.deviceId,
        "X-UAuth-ClientProfile": clientProfile ?? "",
        "X-Requested-With": "UAuth"
    };

    const response = await fetch(url, {
        method: "POST",
        credentials: "include",
        headers: headers,
        body: payload ? JSON.stringify(payload) : null
    });

    let responseBody = null;
    try {
        responseBody = await response.json();
    } catch {
        responseBody = null;
    }

    return {
        ok: response.ok,
        status: response.status,
        refreshOutcome: response.headers.get("X-UAuth-Refresh"),
        body: responseBody
    };
};

window.uauth.setDeviceId = function (value) {
    window.uauth.deviceId = value;
};

window.uauth.getDeviceInfo = function () {
    return {
        userAgent: navigator.userAgent,
        platform: navigator.platform,
        language: navigator.language
    };
};
