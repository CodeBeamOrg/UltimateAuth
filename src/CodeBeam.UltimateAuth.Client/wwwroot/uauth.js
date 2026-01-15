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

    //if (!form.querySelector("input[name='__uauth_device']")) {
        const udid = document.createElement("input");
        udid.type = "hidden";
        udid.name = "__uauth_device";
        udid.value = window.uauth.deviceId;
        form.appendChild(udid);
    //}

    form.submit();
};

window.uauth.post = async function (options) {
    const {
        url,
        mode,
        data,
        expectJson,
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
        "X-UAuth-ClientProfile": clientProfile
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
    if (expectJson) {
        try {
            responseBody = await response.json();
        } catch {
            responseBody = null;
        }
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
