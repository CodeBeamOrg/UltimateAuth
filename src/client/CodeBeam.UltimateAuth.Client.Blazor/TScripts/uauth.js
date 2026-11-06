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

window.uauth.submitForm = async function (
    form,
    trustedOrigin = null) {

    if (!form)
        return;

    if (!window.uauth.deviceId) {
        throw new Error("UAuth deviceId is not initialized.");
    }

    const validatedUrl = window.uauth.validateAuthUrl(
        form.action,
        trustedOrigin
    );

    form.action = validatedUrl;

    let udid = form.querySelector(
        "input[name='__uauth_device']"
    );

    if (!udid) {
        udid = document.createElement("input");
        udid.type = "hidden";
        udid.name = "__uauth_device";
        form.appendChild(udid);
    }

    udid.value = window.uauth.deviceId;

    await window.uauth.antiforgery.appendToForm(
        form,
        trustedOrigin
    );

    form.submit();
};

window.uauth.tryAndCommit = async function (options) {
    const {
        tryUrl,
        commitUrl,
        data,
        clientProfile,
        trustedOrigin
    } = options;

    const deviceId = window.uauth.deviceId;

    if (!deviceId) {
        throw new Error("UAuth deviceId is not initialized.");
    }

    const tryResponse = await window.uauth.postJson({
        url: tryUrl,
        payload: data,
        clientProfile,
        trustedOrigin
    });

    if (!tryResponse || !Number.isInteger(tryResponse.status)) {
        throw new Error("Invalid UAuth transport response.");
    }

    const status = tryResponse.status;
    const isHttpSuccess = status >= 200 && status < 300;
    const isClientError = status >= 400 && status < 500;

    if (!isHttpSuccess && !isClientError) {
        throw new Error(
            `Unexpected UAuth try response: HTTP ${status}.`
        );
    }

    const result = tryResponse.body;

    if (!result ||
        typeof result !== "object" ||
        Array.isArray(result) ||
        typeof result.isSuccess !== "boolean") {
        throw new Error("Invalid UAuth try response body.");
    }

    if (isClientError && result.isSuccess) {
        throw new Error(
            "Successful authentication result returned with HTTP error."
        );
    }

    const normalized = {
        isSuccess: result.isSuccess,
        reason: result.reason ?? null,
        remainingAttempts: result.remainingAttempts ?? null,
        lockoutUntilUtc: result.lockoutUntilUtc ?? null,
        requiresMfa: result.requiresMfa ?? false,
        retryWithNewPkce: result.retryWithNewPkce ?? false,
        previewReceipt: result.previewReceipt ?? null
    };

    if (normalized.isSuccess) {
        const form = document.createElement("form");
        form.method = "POST";
        form.action = commitUrl;

        for (const key of Object.keys(data)) {
            const input = document.createElement("input");
            input.type = "hidden";
            input.name = key;
            input.value = data[key] ?? "";
            form.appendChild(input);
        }

        if (normalized.previewReceipt) {
            const receipt = document.createElement("input");
            receipt.type = "hidden";
            receipt.name = "PreviewReceipt";
            receipt.value = normalized.previewReceipt;
            form.appendChild(receipt);
        }

        const cp = document.createElement("input");
        cp.type = "hidden";
        cp.name = "__uauth_client_profile";
        cp.value = clientProfile ?? "";
        form.appendChild(cp);

        const udid = document.createElement("input");
        udid.type = "hidden";
        udid.name = "__uauth_device";
        udid.value = deviceId;
        form.appendChild(udid);

        document.body.appendChild(form);

        try {
            await window.uauth.submitForm(form, trustedOrigin);
        }
        catch (error) {
            form.remove();
            throw error;
        }
    }

    return normalized;
};

window.uauth.post = async function (options) {
    const {
        url,
        mode,
        data,
        clientProfile,
        trustedOrigin
    } = options;

    const validatedUrl = window.uauth.validateAuthUrl(
        url,
        trustedOrigin
    );

    if (mode === "navigate") {
        const form = document.createElement("form");
        form.method = "POST";
        form.action = validatedUrl;

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

        try {
            await window.uauth.submitForm(form, trustedOrigin);
        }
        catch (error) {
            form.remove();
            throw error;
        }

        return null;
    }

    const isLogout =
        mode === "logout" ||
        mode === "logout-observe";

    return window.uauth.request({
        url: validatedUrl,
        method: "POST",
        payload: data,
        clientProfile,
        contentType: "form",
        credentials: "include",
        allowCrossOriginCredentials: true,
        antiforgery: true,
        trustedOrigin,
        navigateOnSuccess: mode === "logout",
        headers: isLogout
            ? { "X-UAuth-Logout-Mode": "observe" }
            : {}
    });
};

window.uauth.postJson = function (options) {
    const url = window.uauth.validateAuthUrl(
        options.url,
        options.trustedOrigin
    );

    return window.uauth.request({
        url,
        method: "POST",
        payload: options.payload,
        clientProfile: options.clientProfile,
        contentType: "json",
        credentials: "include",
        allowCrossOriginCredentials: true,
        antiforgery: true,
        trustedOrigin: options.trustedOrigin
    });
};

window.uauth.request = async function (options) {
    const {
        followRedirects = false,
        navigateOnSuccess = false,
        url,
        method = "GET",
        payload = null,
        clientProfile = "",
        contentType = "json",
        credentials = "same-origin",
        headers: customHeaders = {}
    } = options;

    if (!url) {
        throw new Error("Request URL is required.");
    }

    const requestUrl = new URL(url, document.baseURI);

    if (!["http:", "https:"].includes(requestUrl.protocol)) {
        throw new Error("Unsupported request URL protocol.");
    }

    const requestMethod = method.toUpperCase();

    if (!["GET", "POST", "PUT", "PATCH", "DELETE"].includes(requestMethod)) {
        throw new Error(
            `Unsupported HTTP method: ${requestMethod}`
        );
    }

    if (!["same-origin", "include", "omit"].includes(credentials)) {
        throw new Error("Invalid credentials policy.");
    }

    if (credentials === "include" &&
        requestUrl.origin !== window.location.origin &&
        !options.allowCrossOriginCredentials) {
        throw new Error(
            "Cross-origin credentials require explicit authorization."
        );
    }

    if (!window.uauth.deviceId) {
        throw new Error("UAuth deviceId is not initialized.");
    }

    const headers = {
        "X-UDID": window.uauth.deviceId,
        "X-UAuth-ClientProfile": clientProfile,
        "X-Requested-With": "UAuth",
        ...customHeaders
    };

    let body = null;

    if (payload !== null &&
        payload !== undefined &&
        requestMethod !== "GET") {

        if (contentType === "json") {
            headers["Content-Type"] = "application/json";
            body = JSON.stringify(payload);
        }
        else if (contentType === "form") {
            headers["Content-Type"] =
                "application/x-www-form-urlencoded";

            body = new URLSearchParams();

            for (const key of Object.keys(payload)) {
                body.append(key, payload[key] ?? "");
            }
        }
        else {
            throw new Error(
                `Unsupported content type: ${contentType}`
            );
        }
    }

    if (requestMethod !== "GET" && options.antiforgery === true) {
        const tokenUrl = window.uauth.validateAuthUrl(
            window.uauth.antiforgery.tokenUrl,
            options.trustedOrigin
        );

        if (new URL(tokenUrl).origin !== requestUrl.origin) {
            throw new Error(
                "UAuth antiforgery token origin differs from request origin."
            );
        }

        const token = await window.uauth.antiforgery.getToken(
            options.trustedOrigin
        );

        headers[token.headerName] = token.requestToken;
    }

    let response;

    try {
        response = await fetch(requestUrl.href, {
            method: requestMethod,
            credentials,
            headers,
            body,
            redirect: "follow"
        });
    }
    catch (error) {
        if (error instanceof TypeError) {
            return {
                ok: false,
                status: 0,
                refreshOutcome: null,
                body: null
            };
        }

        throw error;
    }

    let responseBody = null;

    try {
        responseBody = await response.json();
    }
    catch {
        responseBody = null;
    }

    return {
        ok: response.ok,
        status: response.status,
        refreshOutcome: response.headers.get("X-UAuth-Refresh"),
        body: responseBody
    };
};

window.uauth.antiforgery = {
    tokenUrl: null,

    configure: function (options) {
        if (!options?.tokenUrl) {
            throw new Error(
                "UAuth antiforgery token URL is required."
            );
        }

        this.tokenUrl = window.uauth.validateAuthUrl(
            options.tokenUrl,
            options.trustedOrigin
        );
    },

    getToken: async function (trustedOrigin) {
        if (!this.tokenUrl) {
            throw new Error(
                "UAuth antiforgery is not configured."
            );
        }

        const url = window.uauth.validateAuthUrl(
            this.tokenUrl,
            trustedOrigin
        );

        const response = await fetch(url, {
            method: "GET",
            credentials: "include",
            cache: "no-store",
            redirect: "error"
        });

        if (!response.ok) {
            throw new Error(
                `Unable to obtain UAuth antiforgery token: HTTP ${response.status}.`
            );
        }

        const token = await response.json();

        if (!token?.requestToken ||
            !token?.headerName ||
            !token?.formFieldName) {
            throw new Error(
                "Invalid UAuth antiforgery response."
            );
        }

        return token;
    },

    appendToForm: async function (form, trustedOrigin) {
        const token = await this.getToken(trustedOrigin);

        let input = Array.from(form.elements)
            .find(element => element.name === token.formFieldName);

        if (!input) {
            input = document.createElement("input");
            input.type = "hidden";
            input.name = token.formFieldName;
            form.appendChild(input);
        }

        input.value = token.requestToken;
    }
};

window.uauth.validateAuthUrl = function (
    url,
    trustedOrigin = null) {

    if (!url || typeof url !== "string") {
        throw new Error("Authentication URL is required.");
    }

    const origin = trustedOrigin
        ? new URL(trustedOrigin, window.location.origin)
        : new URL(window.location.origin);

    const target = new URL(url, document.baseURI);

    if (!["http:", "https:"].includes(origin.protocol)) {
        throw new Error("Unsupported trusted origin protocol.");
    }

    if (!["http:", "https:"].includes(target.protocol)) {
        throw new Error("Unsupported authentication URL protocol.");
    }

    if (origin.username || origin.password) {
        throw new Error(
            "Trusted origin cannot contain credentials."
        );
    }

    if (target.username || target.password) {
        throw new Error(
            "Authentication URL cannot contain credentials."
        );
    }

    if (target.origin !== origin.origin) {
        throw new Error(
            "Authentication request target differs from the trusted UAuthHub origin."
        );
    }

    return target.href;
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
